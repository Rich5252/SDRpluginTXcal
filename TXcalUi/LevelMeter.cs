using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace TXcalUi
{
    /// <summary>
    /// Horizontal, segmented-LED style level meter. Feed it a level in dB through the
    /// Level property (default range -99 to +3 dB). Features:
    ///   * Smooth attack/decay: the lit bar rises quickly and falls gradually, so it
    ///     isn't jittery (AttackDbPerSecond / DecayDbPerSecond; set either to 0 or less
    ///     to make that direction instant).
    ///   * Peak hold marker: the highest recent level stays lit for PeakHoldMilliseconds,
    ///     then falls at PeakFallDbPerSecond until it meets the bar again.
    ///   * dB tick labels along the bottom (ScaleStepDb spacing, ShowScale to hide).
    ///   * Green / yellow / red segment colouring by dB threshold.
    ///
    /// Level can be set from any thread (it marshals itself onto the UI thread).
    /// The animation timer only runs while something is actually moving, so an idle
    /// meter costs no CPU.
    /// </summary>
    [DefaultProperty("Level")]
    public class LevelMeter : Control
    {
        private double _minDb = -99.0;
        private double _maxDb = 3.0;

        private double _targetDb = -99.0;   // latest value fed in
        private double _displayDb = -99.0;  // smoothed value actually drawn
        private double _peakDb = -99.0;     // peak hold marker position
        private DateTime _peakSetUtc = DateTime.UtcNow;
        private DateTime _lastTickUtc = DateTime.UtcNow;

        private int _segmentCount = 51;     // 51 over 102 dB = 2 dB per segment by default
        private int _segmentGap = 1;
        private double _yellowDb = -18.0;
        private double _redDb = -6.0;
        private bool _showScale = true;
        private double _scaleStepDb = 10.0;
        private bool _showPeak = true;
        private int _peakHoldMs = 1500;
        private double _peakFallDbPerSec = 20.0;
        private double _attackDbPerSec = 400.0;
        private double _decayDbPerSec = 60.0;

        private Color _lowColor = Color.FromArgb(0, 200, 70);
        private Color _midColor = Color.FromArgb(240, 200, 0);
        private Color _highColor = Color.FromArgb(230, 40, 40);

        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer { Interval = 30 };

        public LevelMeter()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.FromArgb(24, 24, 24);
            ForeColor = Color.Silver;

            _timer.Tick += OnTick;
        }

        protected override Size DefaultSize => new Size(320, 40);

        /// <summary>Short caption (about 3 characters, e.g. "dB") drawn on the tick label
        /// row, right-justified to the control's right edge.</summary>
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Visible),
         Category("Level Meter"), DefaultValue(""),
         Description("Caption drawn at the right end of the tick label row.")]
        public override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        // ---------------------------------------------------------------------
        // Value
        // ---------------------------------------------------------------------

        /// <summary>Current input level in dB. Values outside Minimum..Maximum are
        /// clamped; NaN and -Infinity read as Minimum.</summary>
        [Category("Level Meter"), DefaultValue(-99.0),
         Description("Level to display, in dB.")]
        public double Level
        {
            get => _targetDb;
            set
            {
                if (IsHandleCreated && InvokeRequired)
                {
                    double v = value;
                    BeginInvoke((Action)(() => SetLevel(v)));
                    return;
                }
                SetLevel(value);
            }
        }

        [Category("Level Meter"), DefaultValue(-99.0),
         Description("dB value at the left (empty) end of the bar.")]
        public double Minimum
        {
            get => _minDb;
            set
            {
                if (value >= _maxDb) throw new ArgumentOutOfRangeException(nameof(value), "Minimum must be less than Maximum.");
                _minDb = value;
                ReclampAll();
            }
        }

        [Category("Level Meter"), DefaultValue(3.0),
         Description("dB value at the right (full) end of the bar.")]
        public double Maximum
        {
            get => _maxDb;
            set
            {
                if (value <= _minDb) throw new ArgumentOutOfRangeException(nameof(value), "Maximum must be greater than Minimum.");
                _maxDb = value;
                ReclampAll();
            }
        }

        /// <summary>Clears the peak hold marker back down to the current bar level.</summary>
        public void ResetPeak()
        {
            if (IsHandleCreated && InvokeRequired)
            {
                BeginInvoke((Action)ResetPeak);
                return;
            }
            _peakDb = _displayDb;
            _peakSetUtc = DateTime.UtcNow;
            Invalidate();
        }

        // ---------------------------------------------------------------------
        // Appearance / behaviour
        // ---------------------------------------------------------------------

        [Category("Level Meter"), DefaultValue(51),
         Description("Number of LED segments across the bar.")]
        public int SegmentCount
        {
            get => _segmentCount;
            set { _segmentCount = Math.Max(1, Math.Min(400, value)); Invalidate(); }
        }

        [Category("Level Meter"), DefaultValue(1),
         Description("Gap in pixels between segments (reduced automatically if the control is too narrow).")]
        public int SegmentGap
        {
            get => _segmentGap;
            set { _segmentGap = Math.Max(0, Math.Min(10, value)); Invalidate(); }
        }

        [Category("Level Meter"), DefaultValue(-18.0),
         Description("Segments at or above this dB level are drawn in MidColor.")]
        public double YellowThresholdDb
        {
            get => _yellowDb;
            set { _yellowDb = value; Invalidate(); }
        }

        [Category("Level Meter"), DefaultValue(-6.0),
         Description("Segments at or above this dB level are drawn in HighColor.")]
        public double RedThresholdDb
        {
            get => _redDb;
            set { _redDb = value; Invalidate(); }
        }

        [Category("Level Meter"), DefaultValue(true),
         Description("Show the dB tick marks and labels under the bar.")]
        public bool ShowScale
        {
            get => _showScale;
            set { _showScale = value; Invalidate(); }
        }

        [Category("Level Meter"), DefaultValue(10.0),
         Description("Spacing in dB between scale tick labels (ticks fall on multiples of this).")]
        public double ScaleStepDb
        {
            get => _scaleStepDb;
            set { _scaleStepDb = value > 0 ? value : 10.0; Invalidate(); }
        }

        [Category("Level Meter"), DefaultValue(true),
         Description("Show the peak hold marker.")]
        public bool ShowPeakHold
        {
            get => _showPeak;
            set { _showPeak = value; Invalidate(); }
        }

        [Category("Level Meter"), DefaultValue(1500),
         Description("How long the peak marker is held before it starts to fall, in milliseconds.")]
        public int PeakHoldMilliseconds
        {
            get => _peakHoldMs;
            set { _peakHoldMs = Math.Max(0, value); }
        }

        [Category("Level Meter"), DefaultValue(20.0),
         Description("How fast the peak marker falls after the hold time, in dB per second.")]
        public double PeakFallDbPerSecond
        {
            get => _peakFallDbPerSec;
            set { _peakFallDbPerSec = Math.Max(0.1, value); }
        }

        [Category("Level Meter"), DefaultValue(400.0),
         Description("How fast the bar rises toward a higher level, in dB per second. 0 or less = instant.")]
        public double AttackDbPerSecond
        {
            get => _attackDbPerSec;
            set { _attackDbPerSec = value; }
        }

        [Category("Level Meter"), DefaultValue(60.0),
         Description("How fast the bar falls toward a lower level, in dB per second. 0 or less = instant.")]
        public double DecayDbPerSecond
        {
            get => _decayDbPerSec;
            set { _decayDbPerSec = value; }
        }

        [Category("Level Meter"), Description("Colour of segments below the yellow threshold.")]
        public Color LowColor
        {
            get => _lowColor;
            set { _lowColor = value; Invalidate(); }
        }
        private bool ShouldSerializeLowColor() => _lowColor != Color.FromArgb(0, 200, 70);
        private void ResetLowColor() => LowColor = Color.FromArgb(0, 200, 70);

        [Category("Level Meter"), Description("Colour of segments between the yellow and red thresholds.")]
        public Color MidColor
        {
            get => _midColor;
            set { _midColor = value; Invalidate(); }
        }
        private bool ShouldSerializeMidColor() => _midColor != Color.FromArgb(240, 200, 0);
        private void ResetMidColor() => MidColor = Color.FromArgb(240, 200, 0);

        [Category("Level Meter"), Description("Colour of segments at or above the red threshold.")]
        public Color HighColor
        {
            get => _highColor;
            set { _highColor = value; Invalidate(); }
        }
        private bool ShouldSerializeHighColor() => _highColor != Color.FromArgb(230, 40, 40);
        private void ResetHighColor() => HighColor = Color.FromArgb(230, 40, 40);

        // ---------------------------------------------------------------------
        // Animation
        // ---------------------------------------------------------------------

        private double Clamp(double db)
        {
            if (double.IsNaN(db) || db < _minDb) return _minDb;
            if (db > _maxDb) return _maxDb;
            return db;
        }

        private void SetLevel(double db)
        {
            double v = Clamp(db);
            _targetDb = v;

            // Instant response in a direction whose rate is switched off.
            if (v > _displayDb && _attackDbPerSec <= 0) _displayDb = v;
            if (v < _displayDb && _decayDbPerSec <= 0) _displayDb = v;

            if (v >= _peakDb)
            {
                _peakDb = v;
                _peakSetUtc = DateTime.UtcNow; // re-arm the hold time
            }

            Invalidate();
            StartTimerIfNeeded();
        }

        private void ReclampAll()
        {
            _targetDb = Clamp(_targetDb);
            _displayDb = Clamp(_displayDb);
            _peakDb = Clamp(_peakDb);
            Invalidate();
        }

        private bool IsSettled()
        {
            const double eps = 1e-9;
            return Math.Abs(_displayDb - _targetDb) < eps && _peakDb <= _displayDb + eps;
        }

        private void StartTimerIfNeeded()
        {
            if (_timer.Enabled || IsSettled()) return;
            _lastTickUtc = DateTime.UtcNow;
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            double dt = (now - _lastTickUtc).TotalSeconds;
            _lastTickUtc = now;
            if (dt <= 0) dt = 0.001;
            if (dt > 0.25) dt = 0.25; // don't leap after a UI stall

            if (_targetDb > _displayDb)
                _displayDb = _attackDbPerSec > 0 ? Math.Min(_targetDb, _displayDb + _attackDbPerSec * dt) : _targetDb;
            else if (_targetDb < _displayDb)
                _displayDb = _decayDbPerSec > 0 ? Math.Max(_targetDb, _displayDb - _decayDbPerSec * dt) : _targetDb;

            if (_peakDb > _displayDb && (now - _peakSetUtc).TotalMilliseconds > _peakHoldMs)
                _peakDb = Math.Max(_displayDb, _peakDb - _peakFallDbPerSec * dt);

            Invalidate();
            if (IsSettled()) _timer.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }

        // ---------------------------------------------------------------------
        // Painting
        // ---------------------------------------------------------------------

        private Color ColorForDb(double db)
        {
            if (db >= _redDb) return _highColor;
            if (db >= _yellowDb) return _midColor;
            return _lowColor;
        }

        private static Color Blend(Color a, Color b, double t) // t = 0 gives a, 1 gives b
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            const TextFormatFlags measureFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            bool hasText = !string.IsNullOrEmpty(Text);
            bool labelLine = _showScale || hasText; // the row under the bar
            int textH = 0;
            int inset = 1;
            if (labelLine)
            {
                Size sample = TextRenderer.MeasureText("-99", Font, new Size(int.MaxValue, int.MaxValue), measureFlags);
                textH = sample.Height;
                if (_showScale)
                    inset = sample.Width / 2 + 2; // leave room for the end labels to centre on their ticks
            }

            int scaleH = labelLine ? textH + 5 : 0;
            Rectangle bar = new Rectangle(inset, 1, Width - 2 * inset, Height - scaleH - 2);
            if (bar.Width < 4 || bar.Height < 2) return;

            double span = _maxDb - _minDb;
            int n = _segmentCount;
            int gap = _segmentGap;
            double segW = (bar.Width - gap * (n - 1)) / (double)n;
            if (segW < 1.0)
            {
                gap = 0;
                segW = bar.Width / (double)n;
            }

            int peakIdx = -1;
            if (_showPeak && _peakDb > _minDb + span / n * 0.5)
                peakIdx = Math.Max(0, Math.Min(n - 1, (int)Math.Floor((_peakDb - _minDb) / span * n)));

            using (var litLow = new SolidBrush(_lowColor))
            using (var litMid = new SolidBrush(_midColor))
            using (var litHigh = new SolidBrush(_highColor))
            using (var dimLow = new SolidBrush(Blend(_lowColor, BackColor, 0.80)))
            using (var dimMid = new SolidBrush(Blend(_midColor, BackColor, 0.80)))
            using (var dimHigh = new SolidBrush(Blend(_highColor, BackColor, 0.80)))
            {
                for (int i = 0; i < n; i++)
                {
                    double lo = _minDb + span * i / n;
                    double hi = _minDb + span * (i + 1) / n;
                    double centre = (lo + hi) / 2.0;

                    bool lit = _displayDb >= centre || i == peakIdx;
                    Brush brush;
                    if (centre >= _redDb) brush = lit ? litHigh : dimHigh;
                    else if (centre >= _yellowDb) brush = lit ? litMid : dimMid;
                    else brush = lit ? litLow : dimLow;

                    int x0 = bar.Left + (int)Math.Round(i * (segW + gap));
                    int x1 = bar.Left + (int)Math.Round(i * (segW + gap) + segW);
                    g.FillRectangle(brush, x0, bar.Top, Math.Max(1, x1 - x0), bar.Height);
                }
            }

            // Text is drawn on the label row, right-justified to the control's right edge.
            // Any tick labels that would run into it are dropped (the tick marks stay).
            int textLeft = int.MaxValue;
            if (hasText)
            {
                Size tsz = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), measureFlags);
                textLeft = Math.Max(0, Width - 1 - tsz.Width);
                TextRenderer.DrawText(g, Text, Font,
                    new Rectangle(textLeft, bar.Bottom + 4, Width - textLeft, textH),
                    ForeColor, measureFlags | TextFormatFlags.Right);
            }

            if (_showScale)
                DrawScale(g, bar, span, textH, measureFlags, textLeft);
        }

        private void DrawScale(Graphics g, Rectangle bar, double span, int textH, TextFormatFlags measureFlags, int textLeft)
        {
            using (var tickPen = new Pen(Blend(ForeColor, BackColor, 0.35)))
            {
                int lastRight = int.MinValue;
                double first = Math.Ceiling(_minDb / _scaleStepDb) * _scaleStepDb;
                for (double db = first; db <= _maxDb + 1e-9; db += _scaleStepDb)
                {
                    int x = bar.Left + (int)Math.Round((db - _minDb) / span * bar.Width);
                    g.DrawLine(tickPen, x, bar.Bottom + 1, x, bar.Bottom + 3);

                    string text = Math.Round(db).ToString("0");
                    Size sz = TextRenderer.MeasureText(text, Font, new Size(int.MaxValue, int.MaxValue), measureFlags);
                    int left = x - sz.Width / 2;
                    if (left <= lastRight + 2) continue; // would overlap the previous label -- skip it
                    if (left + sz.Width + 2 >= textLeft) continue; // would run into the Text -- skip it

                    TextRenderer.DrawText(g, text, Font,
                        new Rectangle(left, bar.Bottom + 4, sz.Width, textH),
                        ForeColor, measureFlags | TextFormatFlags.HorizontalCenter);
                    lastRight = left + sz.Width;
                }
            }
        }
    }
}
