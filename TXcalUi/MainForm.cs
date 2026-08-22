using System;
using System.Windows.Forms;

namespace TXcalUi
{
    public partial class MainForm : Form
    {
        private readonly ITXcalController _controller;
        private const int Channel = 0; // adjust if the plugin needs to address multiple VRX channels

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
        }

        // Nothing native to release here on close -- SDRunoPlugin_TXcalUi's destructor
        // (native side) owns the lifetime of TXcalUiHost/TXcalControllerBridge and
        // tears them down when the plugin itself is destroyed.
    }
}
