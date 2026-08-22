using System;
using System.Diagnostics;
using System.IO.Ports;

namespace SerialDemo
{
    /// <summary>
    /// Thin wrapper around SerialPort exposing a synchronous Send(msg) -> reply call.
    /// Construct it once (e.g. as a field on your form), Open() it, then call Send()
    /// whenever you need a request/reply round trip. Send() blocks the calling thread
    /// until a full "\r\n"-terminated reply arrives or the timeout elapses - so call it
    /// from a background thread (or an async handler via Task.Run) rather than directly
    /// on the UI thread, or your form will freeze while waiting for the device.
    /// </summary>
    public class SerialClient : IDisposable
    {
        private readonly object _lock = new object();
        private SerialPort _port;

        public bool IsOpen => _port?.IsOpen == true;

        public void Open(string portName, int baudRate,
            Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One,
            int readTimeoutMs = 3000, int writeTimeoutMs = 2000)
        {
            Close();

            _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                Handshake = Handshake.None,
                NewLine = "\r\n",          // WriteLine appends this; ReadLine stops at (and strips) this
                ReadTimeout = readTimeoutMs,
                WriteTimeout = writeTimeoutMs
            };
            _port.Open();
        }

        /// <summary>
        /// For boards like the ESP32-S3 that reboot when the port is opened (DTR/RTS toggle)
        /// and then spam a boot log / continuous telemetry until told to stop: open the port,
        /// swallow the reboot chatter, send "v" to silence it, and swallow whatever trails
        /// that too - so the client is left in a clean state ready for normal Send() calls.
        /// Throws TimeoutException if the device never goes quiet after repeated attempts.
        /// </summary>
        public void OpenAndSilenceEsp32(string portName, int baudRate, string silenceCommand = "v",
            int maxAttempts = 3)
        {
            Open(portName, baudRate);

            // Let the reboot finish and the boot log / early telemetry drain out.
            // If it's genuinely continuous with no gaps this will just run for the full
            // maxWaitMs and move on - that's fine, it acts as a bounded settle delay either way.
            DrainUntilQuiet(quietPeriodMs: 300, maxWaitMs: 4000);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _port.DiscardInBuffer();
                _port.WriteLine(silenceCommand);

                if (DrainUntilQuiet(quietPeriodMs: 300, maxWaitMs: 1500))
                {
                    _port.DiscardInBuffer(); // belt-and-braces: clear any last straggling bytes
                    return; // stream has gone quiet - device is in command mode
                }

                // Still noisy - "v" was probably lost in the flood. Loop and try again.
            }

            throw new TimeoutException(
                $"ESP32 did not go quiet after sending \"{silenceCommand}\" {maxAttempts} time(s).");
        }

        /// <summary>
        /// Reads and discards bytes until no new byte arrives for quietPeriodMs, or gives up
        /// after maxWaitMs total. Returns true if it detected a quiet gap, false if it hit the
        /// overall cap while data was still arriving.
        /// </summary>
        private bool DrainUntilQuiet(int quietPeriodMs, int maxWaitMs)
        {
            var stopwatch = Stopwatch.StartNew();
            int savedTimeout = _port.ReadTimeout;
            _port.ReadTimeout = quietPeriodMs;
            try
            {
                while (stopwatch.ElapsedMilliseconds < maxWaitMs)
                {
                    try
                    {
                        _port.ReadByte(); // discard one byte; blocks up to quietPeriodMs
                    }
                    catch (TimeoutException)
                    {
                        return true; // no byte arrived within quietPeriodMs -> stream is quiet
                    }
                }
                return false; // still receiving data when maxWaitMs ran out
            }
            finally
            {
                _port.ReadTimeout = savedTimeout;
            }
        }

        /// <summary>
        /// Discards anything currently sitting in the input buffer right now, with no wait.
        /// Use OpenAndSilenceEsp32 / DrainUntilQuiet instead when you need to wait out a
        /// device that's actively still sending.
        /// </summary>
        public void FlushInput()
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");
            _port.DiscardInBuffer();
        }

        /// <summary>
        /// Sends one message and blocks until the single "\r\n"-terminated reply line
        /// comes back. Throws TimeoutException if nothing arrives in time.
        /// </summary>
        public string Send(string message)
        {
            lock (_lock) // guards against two callers using the port at once
            {
                if (_port == null || !_port.IsOpen)
                    throw new InvalidOperationException("Serial port is not open.");

                _port.DiscardInBuffer();   // drop anything stale left over from before
                _port.Write(message);  // appends "\r\n" automatically
                return _port.ReadLine();   // blocks until "\r\n"; terminator is stripped from the result
            }
        }

        public void Close()
        {
            if (_port != null)
            {
                if (_port.IsOpen) _port.Close();
                _port.Dispose();
                _port = null;
            }
        }

        public void Dispose() => Close();
    }
}
