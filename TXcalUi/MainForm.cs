using SerialDemo;
using System;
using System.Linq;
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

        private SerialClient _serial = new SerialClient(); // optional SerialClient for talking to an external device over a COM port

        public MainForm(ITXcalController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent();
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

        
        private void MainForm_Shown(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(1000);            //wait for esp to wake up
            _serial.OpenAndSilenceEsp32("COM14", 921600); // adjust COM port and baud rate as needed
            tbData.AppendText("Serial port opened and ESP32 silenced.\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _serial.Close();
        }

        private void but3rdIMD_Click(object sender, EventArgs e)
        {
            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorCW); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 250);

            double ft1 = 14200700 + double.Parse(tbDeltaF.Text); // 14.200700 MHz
            double ft2 = 14201900 + double.Parse(tbDeltaF.Text); // 14.201900 MHz
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
            if (adjustFreq) NewFreq = freqHz + double.Parse(tbDeltaF.Text);

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


        private void tbCmd_KeyPress(object sender, KeyPressEventArgs e)
        {
            string strCmd = e.KeyChar.ToString();
            string strRet = _serial.Send(strCmd);
            tbData.AppendText($"Command sent: {strCmd}\r\n");
            tbData.AppendText($"Response: {strRet}\r\n");
            if (strCmd == "P")
            {
                strRet = _serial.ReadLine();
                tbResults.AppendText($"Response: {strRet}\r\n");
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

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        private void butMeas_Click(object sender, EventArgs e)
        {
            StopFlg = false; // reset the stop flag before starting the measurement loop
            tbResults.Text = string.Empty;
            double power = 0;
            double avgPower = 0;
            string level = string.Empty;

            level = _serial.Send("s");
            tbResults.AppendText($"{level}\r\n");

            for (int i = 0; i < 10; i++) level = _serial.Send("j");
            for (int i = 0; i < 5; i++) level = _serial.Send("i");
            tbResults.AppendText($"{level}\r\n\r\n");

            for (int i = 0; i < 50; i++) level = _serial.Send("-");
            tbResults.AppendText($"{level}\r\n\r\n");

            while (true)
            {
                //set next level
                level = _serial.Send(".");
                tbResults.AppendText($"{level}, ");
                int nMeas = 5;

                power = 0;
                avgPower = 0;
                for (int i = 0; i < nMeas; i++)
                {
                    power = _controller.GetPower(Channel);
                    //tbResults.AppendText($"GetPower returned {power:F6} dBm\r\n");
                    avgPower += power;

                    System.Threading.Thread.Sleep(250); // wait a bit before the next measurement
                }

                avgPower = avgPower / nMeas; // average the 5 measurements
                tbResults.AppendText($"{avgPower:F6} dBm\r\n");

                if (StopFlg)
                {
                    tbResults.AppendText("Measurement loop stopped by user.\r\n");
                    StopFlg = false; // reset the flag for next time

                    tbCmd.Focus(); // put the cursor back in the command box for convenience
                    return;
                }
            }
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            StopFlg = true;
            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }


        private void butMeasDuty_Click(object sender, EventArgs e)
        {
            StopFlg = false; // reset the stop flag before starting the measurement loop
            tbResults.Text = string.Empty;
            double power = 0;
            double avgPower = 0;
            string strRet = string.Empty;

            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorCW); // set demodulator to USB for image measurement
            _controller.SetFilterBandwidth(Channel, 250); // set filter to 3 kHz for image measurement

            strRet = _serial.Send("s");
            tbResults.AppendText($"{strRet}\r\n");

            strRet = _serial.Send("d");
            if (!strRet.Contains("override ON")) strRet = _serial.Send("d");
            tbResults.AppendText($"{strRet}\r\n");

            int nMeas = 10;
            avgPower = MeasPower(14201000, nMeas); // time to settle agc

            int i = 0;
            for (i = 0; i < 1026; i++)  
            {
                //set next level
                if (i == 0) strRet = _serial.Send("<");     // first time through, set to lowest level
                else strRet = _serial.Send(">");

                for (int j = 0; j < 5; j++)     //retry
                {
                    tbResults.AppendText($"{strRet}, ");

                    if (strRet.Contains("ERROR"))
                    {
                        strRet = _serial.Send("<");
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

                    tbCmd.Focus(); // put the cursor back in the command box for convenience
                    return;
                }
            }

            tbResults.AppendText("Measurement loop completed.\r\n");
            StopFlg = false; // reset the flag for next time

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        private void butMidBand_Click(object sender, EventArgs e)
        {
            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorUSB); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 3000); // set filter to 3 kHz for IMD measurement

            double fbase = 14200000 + double.Parse(tbDeltaF.Text);
            double main = MeasPower(fbase, 5, NoFreqAdjust);
            main = MeasPower(fbase, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Mid Band Test - Main level 3k bndw:, {main:F3}, dBm\r\n");

            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorCW); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 750); // set filter to 3 kHz for IMD measurement

            double ft1 = 14200700 + double.Parse(tbDeltaF.Text); // 14.200700 MHz
            double ft2 = 14201900 + double.Parse(tbDeltaF.Text); // 14.201900 MHz
            double midFreq = (ft1 + ft2) / 2; // 14.201300 MHz

            double pwr = MeasPower(midFreq, 5, NoFreqAdjust);
            pwr = MeasPower(midFreq, 5, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Pwr mid 750Hz band:, {pwr - main:F3}, dB\r\n\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience

        }

        private void butWide_Click(object sender, EventArgs e)
        {
            double fbase = 14200000 + double.Parse(tbDeltaF.Text); // 14.200700 MHz
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
            double fbase = 14200000 + double.Parse(tbDeltaF.Text); // 14.200700 MHz
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

        private void butFloor_Click(object sender, EventArgs e)
        {
            double fbase = 14200000 + double.Parse(tbDeltaF.Text); // 14.200700 MHz

            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorUSB); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 3000); // set filter to 3 kHz for IMD measurement

            string strRet = _serial.Send("o");
            tbResults.AppendText($"{strRet}\r\n");

            double main = MeasPower(fbase, 10, NoFreqAdjust);
            main = MeasPower(fbase, 10, NoFreqAdjust);       //meas twice to give time for agc to settle
            tbResults.AppendText($"Noise Floor 3k bndw:, {main:F3}, dBm\r\n");

            strRet = _serial.Send("o");
        }

        private void butWideIMD_Click(object sender, EventArgs e)
        {
            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorCW); // set demodulator to CW for IMD measurement
            _controller.SetFilterBandwidth(Channel, 250);

            double ft1 = 14200700 + double.Parse(tbDeltaF.Text); // 14.200700 MHz
            double ft2 = 14201900 + double.Parse(tbDeltaF.Text); // 14.201900 MHz
            double flsb3 = 2 * ft1 - ft2; // 14.199500 MHz
            double fusb3 = 2 * ft2 - ft1; // 14.203100 MHz

            double at900 = MeasPower(ft1, 5, NoFreqAdjust);
            tbResults.AppendText($"Wide IMD (250Hz bndw) - 900Hz ref level:, {at900:F3}, dBm\r\n");

            for (double deltaF = 0;  deltaF < 10000; deltaF += 1200)
            {
                double IMD = MeasPower(flsb3 - deltaF, 5, NoFreqAdjust);
                IMD = MeasPower(flsb3 - deltaF, 5, NoFreqAdjust);
                tbResults.AppendText($"Wide IMD (250Hz bndw) - freqHz/ampl:, {- 500 - deltaF}, {IMD - at900:F3}, dB\r\n");
            }

            double at1900 = MeasPower(ft2, 5, NoFreqAdjust);
            tbResults.AppendText($"Wide IMD (250Hz bndw) - 1900Hz ref level:, {at1900:F3}, dBm\r\n");

            for (double deltaF = 0; deltaF < 10000; deltaF += 1200)
            {
                double IMD = MeasPower(fusb3 + deltaF, 5, NoFreqAdjust);
                IMD = MeasPower(fusb3 + deltaF, 5, NoFreqAdjust);
                tbResults.AppendText($"Wide IMD (250Hz bndw) - freqHz/ampl:, {3100 + deltaF}, {IMD - at1900:F3}, dB\r\n");
            }
            tbResults.AppendText("\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        // Nothing native to release here on close -- SDRunoPlugin_TXcalUi's destructor
        // (native side) owns the lifetime of TXcalUiHost/TXcalControllerBridge and
        // tears them down when the plugin itself is destroyed.
    }
}
