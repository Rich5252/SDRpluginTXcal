using SerialDemo;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TXcalUi
{
    public partial class MainForm : Form
    {
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

        private void butMeas_Click(object sender, EventArgs e)
        {


            StopFlg = false; // reset the stop flag before starting the measurement loop
            tbData.Text = string.Empty;
            double power = 0;
            double avgPower = 0;
            string level = string.Empty;

            level = _serial.Send("s");
            tbData.AppendText($"{level}\r\n");

            for (int i = 0; i < 10; i++) level = _serial.Send("j");
            for (int i = 0; i < 5; i++) level = _serial.Send("i");
            tbData.AppendText($"{level}\r\n\r\n");

            for (int i = 0; i < 50; i++) level = _serial.Send("-");
            tbData.AppendText($"{level}\r\n\r\n");

            while (true)
            {
                //set next level
                level = _serial.Send(".");
                tbData.AppendText($"{level}, ");
                int nMeas = 5;

                power = 0;
                avgPower = 0;
                for (int i = 0; i < nMeas; i++)
                {
                    power = _controller.GetPower(Channel);
                    //tbData.AppendText($"GetPower returned {power:F6} dBm\r\n");
                    avgPower += power;

                    System.Threading.Thread.Sleep(250); // wait a bit before the next measurement
                }

                avgPower = avgPower / nMeas; // average the 5 measurements
                tbData.AppendText($"{avgPower:F6} dBm\r\n");

                if (StopFlg)
                {
                    tbData.AppendText("Measurement loop stopped by user.\r\n");
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

        private void MainForm_Shown(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(1000);            //wait for esp to wake up
            _serial.OpenAndSilenceEsp32("COM15", 921600); // adjust COM port and baud rate as needed
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
            _controller.SetFilterBandwidth(Channel, 500); // set filter to 3 kHz for IMD measurement

            double at900 = MeasPower(14200700, 5);
            double atNeg500 = MeasPower(14199500, 5);
            double diff = at900 - atNeg500;
            tbData.AppendText($"Low 3rd IMD: {diff:F3} dBm\r\n");

            at900 = MeasPower(14201900, 5);
            atNeg500 = MeasPower(14203000, 5);
            diff = at900 - atNeg500;
            tbData.AppendText($"High 3rd IMD: {diff:F3} dBm\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        private double MeasPower(double freqHz, int nMeas)
        {
            _controller.SetVfoFrequency(0, freqHz + double.Parse(tbDeltaF.Text));
            System.Threading.Thread.Sleep(2000);

            double power = 0;
            double avgPower = 0;
            for (int i = 0; i < nMeas; i++)
            {
                power = _controller.GetPower(0);
                //tbData.AppendText($"GetPower returned {power:F6} dBm\r\n");
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
            tbCmd.Text = string.Empty;

            tbCmd.Focus(); // put the cursor back in the command box for convenience
        }

        private void butImage_Click(object sender, EventArgs e)
        {
            _controller.SetDemodulatorType(Channel, DemodulatorType.DemodulatorUSB); // set demodulator to USB for image measurement
            _controller.SetFilterBandwidth(Channel, 3000); // set filter to 3 kHz for image measurement

            double at900 = MeasPower(1420000, 5);
            double atNeg500 = MeasPower(14216000, 5);
            double diff = at900 - atNeg500;
            tbData.AppendText($"High Image: {diff:F3} dBm\r\n");

            atNeg500 = MeasPower(14284000, 5);
            diff = at900 - atNeg500;
            tbData.AppendText($"Low Image: {diff:F3} dBm\r\n");

            tbCmd.Focus(); // put the cursor back in the command box for convenience

        }

        // Nothing native to release here on close -- SDRunoPlugin_TXcalUi's destructor
        // (native side) owns the lifetime of TXcalUiHost/TXcalControllerBridge and
        // tears them down when the plugin itself is destroyed.
    }
}
