using SerialDemo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

        
namespace TXcalUi
{

    public partial class MainForm : Form
    {
        private const bool FreqAdjust = true;
        private const bool NoFreqAdjust = true;

        private readonly ITXcalController _controller;
        private const int Channel = 0; // adjust if the plugin needs to address multiple VRX channels

        private bool StopFlg = false; // set by the Stop button to signal the measurement loop to exit

        private readonly AsyncSerialClient _serial = new AsyncSerialClient(); // async client for talking to the ESP32 over a COM port
        private readonly AudioFrequencyMeter _audioMeter = new AudioFrequencyMeter(); // on-demand mic-input tone measurement (NAudio)

        // Ticks on the UI thread and flushes _pendingDataText into tbData -- see
        // AppendToDataBox for why writes go through a buffer instead of straight in.
        private readonly System.Windows.Forms.Timer _dataFlushTimer = new System.Windows.Forms.Timer { Interval = 100 };

        public MainForm(ITXcalController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent();

            // "P" always replies with a fixed "-> settings line ..." marker line followed
            // by exactly one data line -- register it so SendTaggedAsync can pull that pair
            // out of the stream correctly even amid other traffic. Add further tagged
            // commands here as you confirm their marker text and fixed line count.
            _serial.RegisterTaggedCommand("P", "settings line (paste into settingsPresets[] in settings.h, then rename \"Live\")", 2);

            _serial.DataReceived += Serial_DataReceived;

            //tbData.MaxLength = 51000;   //int.MaxValue;
            //tbResults.MaxLength = 51000;   //int.MaxValue;

            _dataFlushTimer.Tick += DataFlushTimer_Tick;
            _dataFlushTimer.Start();
        }

        // Fires on a background thread (the same one SerialPort raises its own
        // DataReceived on) -- marshal onto the UI thread before touching tbData.
        private void Serial_DataReceived(string line)
        {
            AppendToDataBox(line + "\r\n");

            // The ESP32 sends a line containing " held_freq:" once it's settled on and
            // is holding a steady tone -- that's our cue to grab the actual audio
            // frequency via the mic input and log it alongside. Fire-and-forget from
            // this background thread; RunAudioMeasurement marshals its own result back
            // onto the UI thread via AppendToDataBox.
            if (line.Contains(" held_freq:"))
            {
                RunAudioMeasurement("Audio tone at held_freq");
            }
            if (line.Contains("held_trace") && line.Contains("-1]"))
            {
                RunAudioMeasurement("Audio tone at held_trace[-1]");
            }
            if (line.Contains("trend") && line.Contains("-1]"))
            {
                RunAudioMeasurement("Audio tone at trend[-1]");
            }

        }

        // -----------------------------------------------------------------------------
        // tbData storage model
        // -----------------------------------------------------------------------------
        // tbData itself is now just a DISPLAY WINDOW, not the data store. The plain
        // Win32 multiline Edit control behind a WinForms TextBox gets noticeably slow
        // once it's holding a lot of text -- not just appends, but ANY bulk operation on
        // it, including Text = "" from the Clear button, which is exactly why that was
        // locking up too. A 200k-char cap on the visible box avoids that, but that alone
        // isn't enough if you need more than 200k characters of history retained: it
        // just means old lines get discarded once trimmed. So the actual record now
        // lives separately, in _fullLog -- a plain in-memory buffer (not a UI control,
        // so no rendering cost) capped much higher, at MaxFullLogChars. Save Log reads
        // from _fullLog, not from what's currently visible in tbData.
        //
        // Every line goes through AppendToDataBox, which fans it out to both. This used
        // to marshal straight onto the UI thread with tbData.BeginInvoke once per line;
        // during a burst of fast serial telemetry that meant dozens of queued UI messages
        // a second competing with the same queue keyboard/mouse input rides on -- part of
        // what was showing up as "locking up". Now, ALL callers (background threads and
        // UI-thread event handlers alike) just append into a buffer, and a single
        // UI-thread Timer drains it every 100 ms into _fullLog and tbData together.
        private void AppendToDataBox(string text)
        {
            lock (_pendingDataLock)
            {
                _pendingDataText.Append(text);
            }
        }

        private readonly StringBuilder _pendingDataText = new StringBuilder();
        private readonly object _pendingDataLock = new object();

        // The record of everything logged, independent of what's currently on screen.
        // Only ever touched from the UI thread (via DataFlushTimer_Tick and the Save
        // Log / Clear Log handlers), so it needs no lock of its own.
        private readonly StringBuilder _fullLog = new StringBuilder();
        private const int MaxFullLogChars = 5_000_000; // ~5 MB -- comfortably past the 1 MB you need kept

        // Cap on the VISIBLE window only -- kept much smaller than MaxFullLogChars so
        // the Edit control stays responsive regardless of how much history _fullLog is
        // holding.
        private const int MaxDataBoxChars = 32_000;

        private void DataFlushTimer_Tick(object sender, EventArgs e)
        {
            string text;
            lock (_pendingDataLock)
            {
                if (_pendingDataText.Length == 0) return;
                text = _pendingDataText.ToString();
                _pendingDataText.Clear();
            }

            _fullLog.Append(text);
            TrimFullLogIfNeeded();

            tbData.AppendText(text);
            TrimDataBoxIfNeeded();
        }

        private void TrimDataBoxIfNeeded()
        {
            int excess = tbData.TextLength - MaxDataBoxChars;
            if (excess <= 0) return;

            // Drop whole lines from the start rather than cutting mid-line -- find the
            // first line break at or after the excess point and keep everything after it.
            string current = tbData.Text;
            int cutAt = current.IndexOf('\n', excess);
            cutAt = cutAt < 0 ? excess : cutAt + 1;

            tbData.Text = current.Substring(cutAt);
            tbData.SelectionStart = tbData.TextLength;
            tbData.ScrollToCaret();
        }

        private void TrimFullLogIfNeeded()
        {
            int excess = _fullLog.Length - MaxFullLogChars;
            if (excess <= 0) return;

            // Same idea as TrimDataBoxIfNeeded, but avoid stringifying the whole
            // multi-MB buffer just to find where to cut -- pull out only the surplus
            // (plus a little headroom) and search that for a line break instead.
            int searchLen = Math.Min(excess + 1024, _fullLog.Length);
            string prefix = _fullLog.ToString(0, searchLen);
            int cutAt = prefix.IndexOf('\n', excess);
            cutAt = cutAt < 0 ? excess : cutAt + 1;

            _fullLog.Remove(0, cutAt);
        }

        // WaveInEvent can't have two captures open on the same device at once, so
        // measurements still have to run one at a time -- but unlike the old
        // "skip if one is already in progress" guard, a request that arrives while
        // another is running is now queued rather than dropped, so a fast burst of
        // held_freq/held_trace[-1]/trend[-1] lines each still gets its own reading
        // instead of losing whichever one lands mid-measurement. RunAudioMeasurement
        // just enqueues a label and makes sure exactly one worker loop is draining the
        // queue; DrainAudioMeasurementQueueAsync is that worker.
        private readonly Queue<string> _audioMeasurementQueue = new Queue<string>();
        private bool _audioMeasurementWorkerRunning;
        private readonly object _audioQueueLock = new object();

        private void RunAudioMeasurement(string label)
        {
            //return;

            lock (_audioQueueLock)
            {
                _audioMeasurementQueue.Enqueue(label);
                if (_audioMeasurementWorkerRunning) return; // a worker is already draining the queue
                _audioMeasurementWorkerRunning = true;
            }

            // Dispatch via Task.Run rather than calling DrainAudioMeasurementQueueAsync()
            // directly. RunAudioMeasurement can be invoked straight from Serial_DataReceived,
            // which fires on SerialPort's own background thread -- and everything up to an
            // async method's first `await` runs SYNCHRONOUSLY on whatever thread called it.
            // That includes MeasureFrequencyAsync's own prefix, waveIn.StartRecording(),
            // which opens a native audio device and isn't instant. Without this, a burst of
            // serial traffic could briefly stall SerialPort's own line dispatch while an
            // audio device is being opened. Task.Run moves that prefix onto a thread-pool
            // thread instead, so Serial_DataReceived always returns immediately.
            Task.Run(() => DrainAudioMeasurementQueueAsync());
        }

        private async void DrainAudioMeasurementQueueAsync()
        {
            while (true)
            {
                string label;
                lock (_audioQueueLock)
                {
                    if (_audioMeasurementQueue.Count == 0)
                    {
                        _audioMeasurementWorkerRunning = false;
                        return;
                    }
                    label = _audioMeasurementQueue.Dequeue();
                }

                try
                {
                    double freqHz = await _audioMeter.MeasureFrequencyAsync();
                    AppendToDataBox($"{label}: {freqHz - 1000.0:F1} Hz\r\n");
                }
                catch (Exception ex)
                {
                    AppendToDataBox($"Audio frequency measurement failed ({label}): {ex.Message}\r\n");
                }
            }
        }

        // -----------------------------------------------------------------------------
        // Audio input tone measurement (NAudio) -- wire these up to a MenuStrip:
        //   * Put audioDeviceMenu_DropDownOpening on a ToolStripMenuItem's DropDownOpening
        //     event (e.g. an "Audio Device" top-level menu) -- it (re)builds that menu's
        //     items from whatever input devices are currently available each time it's
        //     opened, with the active one checked.
        //   * Put butMeasureAudioFreq_Click on a button or another ToolStripMenuItem's
        //     Click event to measure on demand.
        // -----------------------------------------------------------------------------

        private void audioDeviceMenu_DropDownOpening(object sender, EventArgs e)
        {
            var parent = (ToolStripMenuItem)sender;
            parent.DropDownItems.Clear();

            var devices = AudioFrequencyMeter.GetInputDevices();
            if (devices.Count == 0)
            {
                parent.DropDownItems.Add(new ToolStripMenuItem("(no input devices found)") { Enabled = false });
                return;
            }

            foreach (var device in devices)
            {
                var item = new ToolStripMenuItem(device.Name) { Checked = device.Index == _audioMeter.DeviceNumber };
                item.Click += (s, args) =>
                {
                    _audioMeter.DeviceNumber = device.Index;
                    foreach (ToolStripMenuItem sibling in parent.DropDownItems) sibling.Checked = false;
                    item.Checked = true;
                    AppendToDataBox($"Audio input device set to: {device.Name}\r\n");
                };
                parent.DropDownItems.Add(item);
            }
        }

        private void butMeasureAudioFreq_Click(object sender, EventArgs e)
        {
            RunAudioMeasurement("Measured audio tone");
            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        // Invoked by TXcalUiHost whenever SDRunoPlugin_TXcal::HandleEvent fires.
        // TXcalUiHost has already marshaled this call onto the form's own UI thread,
        // so it is always safe to touch controls directly here.
        public void OnUnoEvent(int eventType, int channel)
        {
            switch ((UnoEventType)eventType)
            {
                case UnoEventType.FrequencyChanged:
                case UnoEventType.CenterFrequencyChanged:
                    RefreshFrequencyDisplay();
                    break;

                case UnoEventType.StreamingStarted:
                    lblStatus.Text = "Streaming";
                    break;

                case UnoEventType.StreamingStopped:
                    lblStatus.Text = "Stopped";
                    break;

                case UnoEventType.ClosingDown:
                    // SDRUno is shutting the plugin down -- close the window rather than
                    // leaving an orphaned form around.
                    Close();
                    break;
            }
        }

        private void RefreshFrequencyDisplay()
        {
            double freqHz = _controller.GetVfoFrequency(Channel);
            lblFrequency.Text = $"{freqHz / 1e6:F6} MHz";
            AppendToDataBox($"SDR frequency updated to {freqHz} MHz\r\n");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            RefreshFrequencyDisplay();
            lblStatus.Text = _controller.IsStreamingEnabled(Channel) ? "Streaming" : "Stopped";
        }

        private void btnSetFrequency_Click(object sender, EventArgs e)
        {
            if (double.TryParse(txtFrequency.Text, out double mhz))
            {
                _controller.SetVfoFrequency(Channel, mhz * 1e6);
                RefreshFrequencyDisplay(); // SetVfoFrequency has now fully returned, so
                                           // t_insideSetCall is back to false and this reads
                                           // the real, settled value instead of the cached one
                                           // the reentrant event handler used mid-call.
            }
            else
            {
                MessageBox.Show(this, "Enter a frequency in MHz, e.g. 14.074", "My Plugin");
            }

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        
        private async void MainForm_Shown(object sender, EventArgs e)
        {
            await Task.Delay(1000); // wait for esp to wake up, without blocking the UI thread

            try
            {
                await _serial.OpenAndSilenceEsp32Async("COM14", 921600); // adjust COM port and baud rate as needed
                AppendToDataBox("Serial port opened and ESP32 silenced.\r\n");
            }
            catch (Exception ex)
            {
                AppendToDataBox($"Failed to open serial port: {ex.Message}\r\n");
            }

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _serial.DataReceived -= Serial_DataReceived;
            _serial.Close();

            _dataFlushTimer.Stop();
            DataFlushTimer_Tick(this, EventArgs.Empty); // flush anything still buffered before we go
            _dataFlushTimer.Dispose();
        }

        private void but3rdIMD_Click(object sender, EventArgs e)
        {
            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorCW); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 250);

            double ft1 = 14200000 + double.Parse(tbTone1.Text)  + double.Parse(tbCalF.Text); // 14.200700 MHz
            double ft2 = 14200000 + double.Parse(tbTone2.Text)  + double.Parse(tbCalF.Text); // 14.201900 MHz
            double flsb3 = 2 * ft1 - ft2; // 14.199500 MHz
            double fusb3 = 2 * ft2 - ft1; // 14.203100 MHz

            double at900 = MeasPower(ft1, 5, NoFreqAdjust);
            tbResults.AppendText($"3rd IMD (250Hz bndw) - 900Hz ref level:, {at900:F3}, dBm\r\n");

            double atNeg500 = MeasPower(flsb3, 5, NoFreqAdjust);
            atNeg500 = MeasPower(flsb3, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            double diff = atNeg500 - at900;
            tbResults.AppendText($"Low 3rd IMD:, {diff:F3}, dB\r\n");

            at900 = MeasPower(ft2, 5, NoFreqAdjust);
            tbResults.AppendText($"3rd IMD - 1900Hz ref level:, {at900:F3}, dBm\r\n");

            atNeg500 = MeasPower(fusb3, 5, NoFreqAdjust);
            atNeg500 = MeasPower(fusb3, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            diff = atNeg500 - at900;
            tbResults.AppendText($"High 3rd IMD:, {diff:F3}, dB\r\n\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }


        private static double LastFreq = 0;
        private double MeasPower(double freqHz, int nMeas, bool adjustFreq = true)
        {
            double NewFreq = freqHz;
            if (adjustFreq) NewFreq = freqHz + double.Parse(tbCalF.Text);

            if (LastFreq != NewFreq)
            {
                _controller.SetVfoFrequency(0, NewFreq);
                System.Threading.Thread.Sleep(2000); // wait for the frequency to settle
                LastFreq = NewFreq;
            }

            double power = 0;
            double avgPower = 0;
            for (int i = 0; i < nMeas; i++)
            {
                power = _controller.GetPower(0);
                //tbResults.AppendText($"GetPower returned {power:F6} dBm\r\n");
                avgPower += power;
                System.Threading.Thread.Sleep(250); // wait a bit before the next measurement
            }
            avgPower = avgPower / nMeas; // average the 5 measurements
            return avgPower;
        }


        private async void tbCmd_KeyPress(object sender, KeyPressEventArgs e)
        {
            string strCmd = e.KeyChar.ToString();
            AppendToDataBox($"Command sent: {strCmd}\r\n");

            if (strCmd == "P")
            {
                // "P" is a registered tagged command -- SendTaggedAsync waits for and
                // returns both its fixed lines (the "-> ..." marker plus the one data
                // line) together, correctly even if other traffic is interleaved. Only
                // the data line (pLines[1]) is useful to show -- pLines[0] is just the
                // fixed "-> settings line ..." marker text, so it's read and discarded
                // here rather than logged to tbData.
                string[] pLines = await _serial.SendTaggedAsync("P");
                if (pLines.Length > 1)
                {
                    tbResults.AppendText($"Response: {pLines[1]}\r\n");
                    UpdatePresets(pLines[1]);
                }
            }
            else
            {
                string strRet = await _serial.SendExclusiveAsync(strCmd);
                AppendToDataBox($"Response: {strRet}\r\n");

                GetAndUpdateSettings(); // refresh the settings display after any command that might have changed them
                
                bool isNumeric = int.TryParse(strCmd, out int n);
                if (isNumeric && n >= 0 && n <= 9)
                {
                    lblPreset.Text = $"Preset {n}";
                    lblPresetName.Text = strRet.Substring(10, strRet.Length - 10);
                }
                else
                {
                    lblPreset.Text = $"Current";
                }
            }
            
            tbCmd.Text = string.Empty;
            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        private void butImage_Click(object sender, EventArgs e)
        {
            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorUSB); // set demodulator to USB for image measurement
            _controller.SetFilterBandwidth(Channel, 3000); // set filter to 3 kHz for image measurement

            double at900 = MeasPower(14200000, 5);
            tbResults.AppendText($"Image - 3k bndw ref level:, {at900:F3}, dBm\r\n");

            double atNeg500 = MeasPower(14216000, 5);
            atNeg500 = MeasPower(14216000, 5);
            double diff = atNeg500 - at900;
            tbResults.AppendText($"High 16k Image:, {diff:F3}, dB\r\n");

            atNeg500 = MeasPower(14184000, 5);
            atNeg500 = MeasPower(14184000, 5);
            diff = atNeg500 - at900;
            tbResults.AppendText($"Low 16k Image:, {diff:F3}, dB\r\n\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience

        }

        private void butMicr_Click(object sender, EventArgs e)
        {
            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorCW); // set demodulator to USB for image measurement
            _controller.SetFilterBandwidth(Channel, 250); // set filter to 3 kHz for image measurement

            double at900 = MeasPower(14200000 + 300 + 125, 5);
            tbResults.AppendText($"Micr Test (250Hz bndw)- ref level:, {at900:F3}, dBm\r\n");

            double atNeg500 = MeasPower(14200000 - 300 - 125, 5);
            atNeg500 = MeasPower(14200000 - 300 - 125, 5);
            double diff = atNeg500 - at900;
            tbResults.AppendText($"Micr Test - Low 250:, {diff:F3}, dB\r\n");

            atNeg500 = MeasPower(14200000 + 3000 - 125, 5);
            atNeg500 = MeasPower(14200000 + 3000 + 3000 - 125, 5);
            diff = atNeg500 - at900;
            tbResults.AppendText($"Hi 250:, {diff:F3}, dB\r\n\r\n");

            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorUSB); // set demodulator to USB for image measurement
            _controller.SetFilterBandwidth(Channel, 3000); // set filter to 3 kHz for image measurement

            at900 = MeasPower(14200000, 5);
            tbResults.AppendText($"Micr Test (3kHz bndw)- ref level:, {at900:F3}, dBm\r\n");

            atNeg500 = MeasPower(14200000 - 3000, 5);
            atNeg500 = MeasPower(14200000 - 3000, 5);
            diff = atNeg500 - at900;
            tbResults.AppendText($"Micr Test - Low 3kHz:, {diff:F3}, dB\r\n");

            atNeg500 = MeasPower(14200000 + 3000, 5);
            atNeg500 = MeasPower(14200000 + 3000, 5);
            diff = atNeg500 - at900;
            tbResults.AppendText($"Hi 3kHz:, {diff:F3}, dB\r\n\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        private async void butMeas_Click(object sender, EventArgs e)
        {
            StopFlg = false; // reset the stop flag before starting the measurement loop
            tbResults.Text = string.Empty;
            double power = 0;
            double avgPower = 0;
            string level = string.Empty;

            try
            {
                level = await _serial.SendExclusiveAsync("s");
                tbResults.AppendText($"{level}\r\n");

                for (int i = 0; i < 10; i++) level = await _serial.SendExclusiveAsync("j");
                for (int i = 0; i < 5; i++) level = await _serial.SendExclusiveAsync("i");
                tbResults.AppendText($"{level}\r\n\r\n");

                for (int i = 0; i < 50; i++) level = await _serial.SendExclusiveAsync("-");
                tbResults.AppendText($"{level}\r\n\r\n");

                while (true)
                {
                    //set next level
                    level = await _serial.SendExclusiveAsync(".");
                    tbResults.AppendText($"{level}, ");
                    int nMeas = 5;

                    power = 0;
                    avgPower = 0;
                    for (int i = 0; i < nMeas; i++)
                    {
                        power = _controller.GetPower(Channel);
                        //tbResults.AppendText($"GetPower returned {power:F6} dBm\r\n");
                        avgPower += power;

                        await Task.Delay(250); // wait a bit before the next measurement, without blocking the UI thread
                    }

                    avgPower = avgPower / nMeas; // average the 5 measurements
                    tbResults.AppendText($"{avgPower:F6} dBm\r\n");

                    if (StopFlg)
                    {
                        tbResults.AppendText("Measurement loop stopped by user.\r\n");
                        StopFlg = false; // reset the flag for next time
                        return;
                    }
                }
            }
            finally
            {
                tbCmd.Focus(); // put the cursor back in the command box for convenience
            }
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            StopFlg = true;
            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }


        private async void butMeasDuty_Click(object sender, EventArgs e)
        {
            StopFlg = false; // reset the stop flag before starting the measurement loop
            tbResults.Text = string.Empty;
            double power = 0;
            double avgPower = 0;
            string strRet = string.Empty;

            try
            {
                _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorCW); // set demodulator to USB for image measurement
                _controller.SetFilterBandwidth(Channel, 250); // set filter to 3 kHz for image measurement

                strRet = await _serial.SendExclusiveAsync("s");
                tbResults.AppendText($"{strRet}\r\n");

                strRet = await _serial.SendExclusiveAsync("d");
                if (!strRet.Contains("override ON")) strRet = await _serial.SendExclusiveAsync("d");
                tbResults.AppendText($"{strRet}\r\n");

                int i = 0;
                int nMeas = 20;
                double lastavgPower = 0;
                for (i = 0; i < 10; i++)
                {
                    avgPower = MeasPower(14201000, nMeas); // time to settle agc
                    if (avgPower > lastavgPower + 0.02 || avgPower < lastavgPower - 0.02)
                    {
                        lastavgPower = avgPower;
                        tbResults.AppendText($"AGC not settled, waiting 2 seconds\r\n");
                        await Task.Delay(2000); // wait a bit before the next measurement, without blocking the UI thread
                    }
                    else
                    {
                        break;
                    }
                }


                for (i = 0; i < 1026; i++)
                {
                    //set next level
                    if (i == 0) strRet = await _serial.SendExclusiveAsync("<");     // first time through, set to lowest level
                    else strRet = await _serial.SendExclusiveAsync(">");

                    for (int j = 0; j < 5; j++)     //retry
                    {
                        tbResults.AppendText($"{strRet}, ");

                        if (strRet.Contains("ERROR"))
                        {
                            strRet = await _serial.SendExclusiveAsync("<");
                            i--;
                            if (i < 0) i = 0;
                        }
                        else
                        {
                            break;
                        }
                    }

                    avgPower = MeasPower(14201000, nMeas); // measure at 14.2 MHz
                    tbResults.AppendText($"{avgPower:F6} dBm\r\n");

                    if (StopFlg)
                    {
                        tbResults.AppendText("Measurement loop stopped by user.\r\n");
                        StopFlg = false; // reset the flag for next time
                        return;
                    }
                }

                tbResults.AppendText("Measurement loop completed.\r\n");
                StopFlg = false; // reset the flag for next time
            }
            finally
            {
                tbCmd.Focus(); // put the cursor back in the command box for convenience
            }
        }

        private void butMidBand_Click(object sender, EventArgs e)
        {
            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorUSB); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 3000); // set filter to 3 kHz for IMD measurement

            double fbase = 14200000 + double.Parse(tbCalF.Text);
            double main = MeasPower(fbase, 5, NoFreqAdjust);
            main = MeasPower(fbase, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Mid Band Test - Main level 3k bndw:, {main:F3}, dBm\r\n");

            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorCW); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 750); // set filter to 3 kHz for IMD measurement

            double ft1 = 14200000 + double.Parse(tbTone1.Text)  + double.Parse(tbCalF.Text); // 14.200700 MHz
            double ft2 = 14200000 + double.Parse(tbTone2.Text)  + double.Parse(tbCalF.Text); // 14.201900 MHz
            double midFreq = (ft1 + ft2) / 2; // 14.201300 MHz

            double pwr = MeasPower(midFreq, 5, NoFreqAdjust);
            pwr = MeasPower(midFreq, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Pwr mid 750Hz band:, {pwr - main:F3}, dB\r\n\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience

        }

        private void butWide_Click(object sender, EventArgs e)
        {
            double fbase = 14200000 + double.Parse(tbCalF.Text); // 14.200700 MHz
            double f16kLo = fbase - 16000; // 14.198700 MHz
            double f16kHi = fbase + 16000; // 14.202700 MHz
            double fwide12Lo = fbase - 12000; // 14.199800 MHz
            double fwide12Hi = fbase + 3000; // 14.202800 MHz
            double f78kLo = fbase - 78000 - 6000; // 14.199220 MHz
            double f78kHi = fbase + 78000 - 6000; // 14.199220 MHz

            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorUSB); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 3000); // set filter to 3 kHz for IMD measurement

            double main = MeasPower(fbase, 5, NoFreqAdjust);
            main = MeasPower(fbase, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Wide Test Main Level (3k bndw):, {main:F3}, dBm\r\n");

            double pwr = MeasPower(f16kLo, 5, NoFreqAdjust);
            pwr = MeasPower(f16kLo, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Low 16k Image (3k bndw):, {pwr-main:F3}, dB\r\n");

            pwr = MeasPower(f16kHi, 5, NoFreqAdjust);
            pwr = MeasPower(f16kHi, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"High 16k Image (3k bndw):, {pwr - main:F3}, dB\r\n");


            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorDigital); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 12000); // set filter to 3 kHz for IMD measurement

            pwr = MeasPower(fwide12Lo, 5, NoFreqAdjust);
            pwr = MeasPower(fwide12Lo, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Low 12kHz wide:, {pwr - main:F3}, dB\r\n");

            pwr = MeasPower(fwide12Hi, 5, NoFreqAdjust);
            pwr = MeasPower(fwide12Hi, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"High 12k wide:, {pwr - main:F3}, dB\r\n");

            pwr = MeasPower(f78kLo, 5, NoFreqAdjust);
            pwr = MeasPower(f78kLo, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Low 78k Image:, {pwr - main:F3}, dB\r\n");

            pwr = MeasPower(f78kHi, 5, NoFreqAdjust);
            pwr = MeasPower(f78kHi, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"High 78k Image:, {pwr - main:F3}, dB\r\n\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience

        }

        private void butSpread_Click(object sender, EventArgs e)
        {
            double fbase = 14200000 + double.Parse(tbCalF.Text); // 14.200700 MHz
            double f5kLo = fbase - 5000; 
            double f5kHi = fbase + 5000; 
            double f10kLo = fbase - 10000; 
            double f10kHi = fbase + 10000; 
            double f20kLo = fbase - 20000; 
            double f20kHi = fbase + 20000; // 14.200700 MHz

            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorUSB); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 3000); // set filter to 3 kHz for IMD measurement

            double main = MeasPower(fbase, 5, NoFreqAdjust);
            main = MeasPower(fbase, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Spread in 3k bndw - Main Level:, {main:F3}, dBm\r\n");

            double pwr = MeasPower(f5kLo, 5, NoFreqAdjust);
            pwr = MeasPower(f5kLo, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Low 5k leakage:, {pwr - main:F3}, dB\r\n");

            pwr = MeasPower(f5kHi, 5, NoFreqAdjust);
            pwr = MeasPower(f5kHi, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"High 5k leakage:, {pwr - main:F3}, dB\r\n");

            pwr = MeasPower(f10kLo, 5, NoFreqAdjust);
            pwr = MeasPower(f10kLo, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Low 10k leakage:, {pwr - main:F3}, dB\r\n");

            pwr = MeasPower(f10kHi, 5, NoFreqAdjust);
            pwr = MeasPower(f10kHi, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"High 10k leakage:, {pwr - main:F3}, dB\r\n");

            pwr = MeasPower(f20kLo, 5, NoFreqAdjust);
            pwr = MeasPower(f20kLo, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Low 20k leakage:, {pwr - main:F3}, dB\r\n");

            pwr = MeasPower(f20kHi, 5, NoFreqAdjust);
            pwr = MeasPower(f20kHi, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"High 20k leakage:, {pwr - main:F3}, dB\r\n\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience

        }

        private async void butFloor_Click(object sender, EventArgs e)
        {
            double fbase = 14200000 + double.Parse(tbCalF.Text); // 14.200700 MHz

            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorUSB); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 3000); // set filter to 3 kHz for IMD measurement

            string strRet = await _serial.SendExclusiveAsync("o");
            tbResults.AppendText($"{strRet}\r\n");

            double main = MeasPower(fbase, 10, NoFreqAdjust);
            main = MeasPower(fbase, 10, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Noise Floor 3k bndw:, {main:F3}, dBm\r\n");

            strRet = await _serial.SendExclusiveAsync("o");

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        private void butWideIMD_Click(object sender, EventArgs e)
        {
            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorCW); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 250);

            double ft1 = 14200000 + double.Parse(tbTone1.Text)  + double.Parse(tbCalF.Text); // 14.200700 MHz
            double ft2 = 14200000 + double.Parse(tbTone2.Text)  + double.Parse(tbCalF.Text); // 14.201900 MHz
            double flsb3 = 2 * ft1 - ft2; // 14.199500 MHz
            double fusb3 = 2 * ft2 - ft1; // 14.203100 MHz

            double at900 = MeasPower(ft1, 5, NoFreqAdjust);
            tbResults.AppendText($"Wide IMD (250Hz bndw) - {tbTone1.Text}Hz ref level:, {at900:F3}, dBm\r\n");

            double ToneDiff = ft2 - ft1;
            for (double deltaF = 0;  deltaF < 10000; deltaF += ToneDiff)
            {
                double IMD = MeasPower(flsb3 - deltaF, 5, NoFreqAdjust);
                IMD = MeasPower(flsb3 - deltaF, 5, NoFreqAdjust);
                tbResults.AppendText($"Wide IMD (250Hz bndw) - freqHz/ampl:, {double.Parse(tbTone1.Text) - ToneDiff - deltaF}, {IMD - at900:F3}, dB\r\n");
            }

            double at1900 = MeasPower(ft2, 5, NoFreqAdjust);
            tbResults.AppendText($"Wide IMD (250Hz bndw) - {tbTone2.Text}Hz ref level:, {at1900:F3}, dBm\r\n");

            for (double deltaF = 0; deltaF < 10000; deltaF += ToneDiff)
            {
                double IMD = MeasPower(fusb3 + deltaF, 5, NoFreqAdjust);
                IMD = MeasPower(fusb3 + deltaF, 5, NoFreqAdjust);
                tbResults.AppendText($"Wide IMD (250Hz bndw) - freqHz/ampl:, {double.Parse(tbTone2.Text) + ToneDiff + deltaF}, {IMD - at1900:F3}, dB\r\n");
            }
            tbResults.AppendText("\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }


         public enum Presets
            {
                preamble,
                name,
                audio_source,
                relative_delay_samples,
                env_pwm_offset,
                env_pwm_scale,
                env_gdeq_enable,
                adc_lpf_mode,
                eq_enable,
                compressor_enable,
                master_gain_db,
                ad9851_output_enable,
                env_predistort_enable,
                env_floor,
                freq_dev_slew_limit_hz,
                envelope_interp_enable,
                envelope_interp_curve,
                Shelf_a_enable,
                Shelf_A_enable,
                gd_eq_variant,
                ALC_enable,
                SoftLimiter_enable,
                MicrdB,
                CompdB,
                Squelch_enable,
                Squelch_threshold
        }
            //  Response:     { "Live", AUDIO_SRC_TWOTONE, 2.00f, 0.20f, 0.90f, true, ADC_LPF_MODE_OFF, false, false, -1.4f, true, true, 0.00f, SSB_DSP_FREQ_DEV_SLEW_UNLIMITED_HZ, false, ENVELOPE_INTERP_CURVE_CATMULL_ROM, true, true, ENV_GDEQ_VARIANT_CANDIDATE_B },

        private string UpdatePresets(string Poutput)
        {
            if (Poutput.Contains("ERROR") || !Poutput.Contains("\"Live\","))
            {
                tbData.AppendText($"Error in P response: {Poutput}\r\n");
                return "ERROR";
            }

            string[] parts = Poutput.Split(new char[] { '{', '}', ',' }, StringSplitOptions.RemoveEmptyEntries);

            string[] subparts = parts[(int)Presets.audio_source].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            tbPreset.Text = subparts[2];

            tbDelay.Text = parts[(int)Presets.relative_delay_samples];
            tbOffset.Text = parts[(int)Presets.env_pwm_offset];
            tbScale.Text = parts[(int)Presets.env_pwm_scale];
            tbGdeq.Text = parts[(int)Presets.env_gdeq_enable];

            subparts = parts[(int)Presets.adc_lpf_mode].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            tbLPF.Text = subparts[3];

            tbEQU.Text = parts[(int)Presets.eq_enable];
            tbComp.Text = parts[(int)Presets.compressor_enable];
            tbGain.Text = parts[(int)Presets.master_gain_db];
            tbRFon.Text = parts[(int)Presets.ad9851_output_enable];
            tbDlin.Text = parts[(int)Presets.env_predistort_enable];
            tbFloor.Text = parts[(int)Presets.env_floor];

            subparts = parts[(int)Presets.freq_dev_slew_limit_hz].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            tbSlew.Text = subparts[5];

            tbInterp.Text = parts[(int)Presets.envelope_interp_enable];

            subparts = parts[(int)Presets.envelope_interp_curve].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            tbCurve.Text = subparts[3];

            tbaShelf.Text = parts[(int)Presets.Shelf_a_enable];
            tbShelfA.Text = parts[(int)Presets.Shelf_A_enable];

            //a+A candidate, candidate B, A_CANDIDATE
            subparts = parts[(int)Presets.gd_eq_variant].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            tbGdOpt.Text = subparts[3] == "CANDIDATE" ? "B" : subparts[3];

            tbALC.Text = parts[(int)Presets.ALC_enable];
            tbSoftLimiter.Text = parts[(int)Presets.SoftLimiter_enable];
            tbCompdB.Text = parts[(int)Presets.CompdB];
            tbMicrdB.Text = parts[(int)Presets.MicrdB];
            tbSquelch_enable.Text = parts[(int)Presets.Squelch_enable];
            tbSquelch_.Text = parts[(int)Presets.Squelch_threshold];

            return parts[(int)Presets.name];
        }

        private void butSaveLog_Click(object sender, EventArgs e)
        {
            // Save from _fullLog, the full backing record -- not tbData.Text, which is
            // only the trimmed display window and can be missing older lines that got
            // dropped from the screen but are still held in _fullLog.
            string strData = _fullLog.ToString();
            string path = SaveTextToDownloads(strData);
            AppendToDataBox($"Saved log to {path}\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        /// <summary>
        /// Writes text to a new file in the user's Downloads folder, named
        /// "log_yyyyMMdd_HHmmss.txt". Returns the full path written to.
        /// </summary>
        private static string SaveTextToDownloads(string text, string baseName = "log")
        {
            try
            {

                string downloadsFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                Directory.CreateDirectory(downloadsFolder); // no-op if it already exists

                string fileName = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string fullPath = Path.Combine(downloadsFolder, fileName);

                File.WriteAllText(fullPath, text);
                return fullPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        private void butClearLog_Click(object sender, EventArgs e)
        {
            // Also drop anything background threads have appended but the flush timer
            // hasn't drawn in yet -- otherwise the very next tick would repaint a few
            // lines of "old" text right back in, right after you just cleared it.
            lock (_pendingDataLock)
            {
                _pendingDataText.Clear();
            }

            // Clears BOTH the visible window and the full backing record, i.e. this is
            // a real "start over", not just "clear the screen" -- Save Log right before
            // clicking this if you want to keep what's there. (If you'd rather Clear only
            // reset the screen and keep accumulating the full record regardless, just
            // drop the _fullLog.Clear() line below.)
            _fullLog.Clear();
            tbData.Text = string.Empty;
        }

        private void butUpdate_Click(object sender, EventArgs e)
        {
            GetAndUpdateSettings();
        }

        private async Task<string> GetAndUpdateSettings()
        {
            string[] pLines = await _serial.SendTaggedAsync("P");
            if (pLines.Length > 1)
            {
                UpdatePresets(pLines[1]);
            }
            return pLines[0];
        }


        // Nothing native to release here on close -- SDRunoPlugin_TXcalUi's destructor
        // (native side) owns the lifetime of TXcalUiHost/TXcalControllerBridge and
        // tears them down when the plugin itself is destroyed.
    }
}
