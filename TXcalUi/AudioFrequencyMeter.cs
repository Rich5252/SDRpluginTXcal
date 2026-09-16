using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NAudio.Dsp;
using NAudio.Wave;

namespace TXcalUi
{
    /// <summary>
    /// Captures a short, on-demand block of audio from a chosen input device and
    /// estimates the frequency of the dominant sine tone in it, via FFT with parabolic
    /// peak interpolation for sub-bin accuracy. Requires the NAudio NuGet package
    /// (adds NAudio.Wave for device I/O and NAudio.Dsp for the FFT).
    /// </summary>
    public class AudioFrequencyMeter
    {
        /// <summary>Index into WaveIn.GetCapabilities() / WaveInEvent.DeviceNumber --
        /// NOT stable across reboots or device (re)plugging, only for the current
        /// session. Defaults to 0 (usually the system default input device). Set this
        /// from a device-selection menu (see MainForm's audioDeviceMenu_DropDownOpening
        /// for an example) before calling MeasureFrequencyAsync.</summary>
        public int DeviceNumber { get; set; } = 0;

        public int SampleRate { get; set; } = 48000;

        /// <summary>Number of samples analysed per measurement -- must be a power of two
        /// (required by NAudio's FFT). Larger values give finer frequency resolution
        /// (SampleRate / FftSize Hz per bin, before the parabolic interpolation below
        /// sharpens it further) at the cost of a longer capture. 8192 at 48 kHz is
        /// ~171 ms of audio and ~5.9 Hz/bin before interpolation -- comfortably enough
        /// for typical SSB audio tones (a few hundred Hz to a few kHz).</summary>
        public int FftSize { get; set; } = 8192;

        /// <summary>How long to wait for FftSize samples to arrive before giving up.</summary>
        public int TimeoutMs { get; set; } = 3000;

        /// <summary>Every input device currently available, in the same order/index used
        /// by DeviceNumber above. Call this fresh each time you populate a device menu --
        /// devices can be plugged/unplugged between calls.</summary>
        public static IReadOnlyList<(int Index, string Name)> GetInputDevices()
        {
            var devices = new List<(int, string)>();
            for (int i = 0; i < WaveIn.DeviceCount; i++)
                devices.Add((i, WaveIn.GetCapabilities(i).ProductName));
            return devices;
        }

        /// <summary>
        /// Records FftSize samples from the selected device and returns the estimated
        /// frequency, in Hz, of its dominant sine component. Throws TimeoutException if
        /// not enough audio arrives within TimeoutMs (no signal, wrong/disconnected
        /// device, etc.) -- call it from an async handler and catch that, rather than
        /// letting it escape into a UI event handler unhandled.
        /// </summary>
        public async Task<double> MeasureFrequencyAsync()
        {
            int fftSize = FftSize;
            var samples = new List<float>(fftSize + 4096);
            var sync = new object();
            var tcs = new TaskCompletionSource<bool>();

            using (var waveIn = new WaveInEvent
            {
                DeviceNumber = DeviceNumber,
                WaveFormat = new WaveFormat(SampleRate, 16, 1), // mono, 16-bit PCM
                BufferMilliseconds = 50
            })
            {
                waveIn.DataAvailable += (s, e) =>
                {
                    lock (sync)
                    {
                        int sampleCount = e.BytesRecorded / 2; // 16-bit PCM = 2 bytes/sample
                        for (int i = 0; i < sampleCount; i++)
                        {
                            short raw = BitConverter.ToInt16(e.Buffer, i * 2);
                            samples.Add(raw / 32768f);
                        }
                        if (samples.Count >= fftSize) tcs.TrySetResult(true);
                    }
                };
                waveIn.RecordingStopped += (s, e) => tcs.TrySetResult(false);

                waveIn.StartRecording();
                var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeoutMs));
                waveIn.StopRecording();

                float[] captured;
                lock (sync)
                {
                    if (completed != tcs.Task || samples.Count < fftSize)
                        throw new TimeoutException(
                            $"Only captured {samples.Count} of {fftSize} samples needed within {TimeoutMs} ms.");
                    captured = samples.GetRange(0, fftSize).ToArray();
                }

                return EstimateSineFrequency(captured, SampleRate);
            }
        }

        private static double EstimateSineFrequency(float[] samples, int sampleRate)
        {
            int n = samples.Length;
            var spectrum = new Complex[n];

            // Hann window reduces spectral leakage so the peak bin comes out cleaner.
            for (int i = 0; i < n; i++)
            {
                double window = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (n - 1)));
                spectrum[i].X = (float)(samples[i] * window);
                spectrum[i].Y = 0;
            }

            int m = (int)Math.Log(n, 2.0); // NAudio's FFT wants log2(n), not n itself
            FastFourierTransform.FFT(true, m, spectrum);

            double Magnitude(int bin) =>
                Math.Sqrt(spectrum[bin].X * spectrum[bin].X + spectrum[bin].Y * spectrum[bin].Y);

            int nyquistBin = n / 2; // spectrum[nyquistBin..] mirrors the lower half for real input
            int peakBin = 1; // start search past DC (bin 0)
            double peakMag = 0;
            for (int i = 1; i < nyquistBin; i++)
            {
                double mag = Magnitude(i);
                if (mag > peakMag) { peakMag = mag; peakBin = i; }
            }

            // Parabolic interpolation across the peak bin and its neighbours, for
            // sub-bin frequency accuracy instead of being quantised to bin width.
            double alpha = Magnitude(Math.Max(peakBin - 1, 0));
            double beta = Magnitude(peakBin);
            double gamma = Magnitude(Math.Min(peakBin + 1, nyquistBin - 1));
            double denom = alpha - 2 * beta + gamma;
            double p = denom == 0 ? 0 : 0.5 * (alpha - gamma) / denom;

            double binFrequency = (double)sampleRate / n;
            return (peakBin + p) * binFrequency;
        }
    }
}
