using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;

namespace SerialDemo
{
    /// <summary>
    /// Describes one command whose response can be picked out of the incoming byte
    /// stream even while continuous telemetry is flowing, because its first line always
    /// starts with a fixed "-&gt; &lt;TagPrefix&gt;" marker (e.g. sending "P" always comes back
    /// as a line starting "-&gt; settings line ..."). Everything after that fixed prefix on
    /// the same line, plus any further continuation lines, is free-form and can vary
    /// every time -- only the prefix and the total line count need to be constant.
    ///
    /// Register one of these per command via <see cref="AsyncSerialClient.RegisterTaggedCommand"/>
    /// and then just call <see cref="AsyncSerialClient.SendTaggedAsync"/> with the command --
    /// the client already knows what marker and how many lines to wait for.
    /// </summary>
    public sealed class TaggedResponse
    {
        /// <summary>The exact text that appears right after "-&gt; " on the first line of
        /// this command's response, e.g. "relative delay" or the full
        /// "settings line (paste into settingsPresets[] in settings.h..." phrase. Matched
        /// as a fixed prefix (line.StartsWith("-&gt; " + TagPrefix)) -- case-sensitive, and
        /// deliberately NOT a full-line match, since the rest of the line varies.</summary>
        public string TagPrefix { get; }

        /// <summary>Total number of lines in the full response, INCLUDING the "-&gt; ..." line
        /// itself. 1 for a single-line response, 2 for a response like "P" that always
        /// sends its data line immediately after the tag line.</summary>
        public int LineCount { get; }

        public TaggedResponse(string tagPrefix, int lineCount = 1)
        {
            if (string.IsNullOrEmpty(tagPrefix)) throw new ArgumentNullException(nameof(tagPrefix));
            if (lineCount < 1) throw new ArgumentOutOfRangeException(nameof(lineCount), "A tagged response must have at least 1 line.");
            TagPrefix = tagPrefix;
            LineCount = lineCount;
        }
    }

    /// <summary>
    /// Async, event-driven replacement for the old blocking SerialClient. Built around
    /// SerialPort.DataReceived rather than ad hoc blocking ReadLine()/ReadByte() calls, so
    /// ONE background listener owns all reading from the port at all times, and multiple
    /// kinds of traffic can be pulled out of the same continuous byte stream at once:
    ///
    ///   1. Tagged responses (see TaggedResponse above) -- a specific command's reply,
    ///      identified by its fixed "-&gt; TagPrefix" marker line, correlated back to the
    ///      SendTaggedAsync call that asked for it even if unrelated telemetry lines are
    ///      arriving in between. Works whether continuous streaming is running or stopped.
    ///
    ///   2. Exclusive request/response commands -- the original single-line "send a
    ///      command, the very next line is its reply" pattern (what "s"/"j"/"i"/"-"/"."/
    ///      "d"/"o"/"&lt;"/"&gt;" etc. all use via the old blocking Send()). Only meaningful
    ///      while continuous streaming is stopped, since otherwise an unrelated telemetry
    ///      line could easily be mistaken for the reply -- SendExclusiveAsync enforces this.
    ///
    ///   3. Everything else -- ongoing continuous telemetry, and the output of "large
    ///      output" commands that don't need to be tied back to a specific request --
    ///      all surfaces uniformly through the DataReceived event as plain lines.
    ///
    /// Error handling follows the same convention the previous synchronous SerialClient
    /// settled on: I/O problems (port not open, no reply within the timeout) come back as
    /// an ordinary "ERROR: ..." string/line rather than a thrown exception, since several
    /// callers already do `if (strRet.Contains("ERROR")) ...` and this is now an async void
    /// UI event handler, where a genuinely thrown exception has nowhere good to go. Calling
    /// a member in a way only a programmer could get wrong (an unregistered tagged command,
    /// SendExclusiveAsync while streaming is running) still throws, same as before.
    ///
    /// Thread-safety: all the Send*/RegisterTaggedCommand members are safe to call from
    /// the UI thread (they don't block it -- await the returned Task instead). The
    /// DataReceived event fires on a background thread (the same one .NET's SerialPort
    /// uses to raise its own DataReceived event) -- marshal back onto the UI thread
    /// (Control.Invoke/BeginInvoke) before touching any UI control from that handler,
    /// same as TXcalUiHost already does for native-originated events.
    /// </summary>
    public class AsyncSerialClient : IDisposable
    {
        private SerialPort _port;
        private readonly StringBuilder _rxBuffer = new StringBuilder();

        // Registered tag metadata, keyed by the command string used to send it.
        private readonly Dictionary<string, TaggedResponse> _taggedCommands = new Dictionary<string, TaggedResponse>();

        // FIFO queue of callers waiting on a match for a given tag prefix. A List rather
        // than a single slot per tag, because nothing stops the same tagged command being
        // sent twice before its first reply arrives -- replies are then matched back to
        // requests strictly in the order they were sent.
        private readonly Dictionary<string, Queue<PendingTagged>> _pendingByTag = new Dictionary<string, Queue<PendingTagged>>();

        // FIFO queue for the plain "send it, next line is the reply" exclusive commands.
        private readonly Queue<TaskCompletionSource<string>> _pendingExclusive = new Queue<TaskCompletionSource<string>>();

        // State for a multi-line tagged response currently being collected: once a line
        // matches a tag, any further lines are unconditionally appended to this buffer
        // (never re-classified) until LineCount is reached.
        private PendingTagged _collecting;
        private readonly List<string> _collectingLines = new List<string>();

        private readonly object _stateLock = new object();

        /// <summary>Raised for every complete line that wasn't consumed as a tagged or
        /// exclusive reply -- continuous telemetry while streaming is running, "large
        /// output" command results, boot chatter, or anything unrecognized. Fires on a
        /// background thread; marshal to the UI thread before touching controls.</summary>
        public event Action<string> DataReceived;

        /// <summary>Host-side bookkeeping only -- NOT used to classify incoming lines
        /// (tagged matching works the same regardless of this flag). Purely so callers
        /// know whether it's currently valid to call SendExclusiveAsync, and so the UI can
        /// reflect streaming state. Defaults to true (device assumed stopped/quiet) since
        /// that's the state OpenAndSilenceEsp32Async leaves it in.</summary>
        public bool IsStreamingStopped { get; private set; } = true;

        /// <summary>The command that both starts and stops continuous streaming (a
        /// toggle) -- defaults to "v", matching the existing silence command.</summary>
        public string StreamingToggleCommand { get; set; } = "v";

        public bool IsOpen => _port?.IsOpen == true;

        public void Open(string portName, int baudRate,
            Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One,
            int writeTimeoutMs = 2000)
        {
            Close();

            _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                Handshake = Handshake.None,
                WriteTimeout = writeTimeoutMs
            };
            _port.DataReceived += OnPortDataReceived;
            _port.Open();
        }

        /// <summary>
        /// For boards like the ESP32-S3 that reboot when the port is opened (DTR/RTS toggle)
        /// and then spam a boot log / continuous telemetry until told to stop: open the port,
        /// wait for the boot chatter to go quiet, then send the streaming-toggle command to
        /// make sure it ends up stopped -- so the client is left ready for SendExclusiveAsync/
        /// SendTaggedAsync calls.
        ///
        /// Deliberately does NOT throw if the device never settles (matching the previous
        /// SerialClient, which had its TimeoutException commented out) -- it just gives up
        /// after maxAttempts and leaves IsStreamingStopped reflecting whatever was last
        /// observed, so a plugin load with no ESP32 attached doesn't take the whole form
        /// down with it.
        /// </summary>
        public async Task OpenAndSilenceEsp32Async(string portName, int baudRate,
            int maxAttempts = 3, int quietPeriodMs = 300, int maxWaitMs = 4000)
        {
            Open(portName, baudRate);
            IsStreamingStopped = true; // assumed until proven otherwise below

            // Let the reboot finish and the boot log / early telemetry drain out. If it's
            // genuinely continuous with no gaps this just waits out maxWaitMs and moves on.
            await WaitForQuietAsync(quietPeriodMs, maxWaitMs);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _port.Write(StreamingToggleCommand);

                if (await WaitForQuietAsync(quietPeriodMs, 1500))
                {
                    IsStreamingStopped = true;
                    return; // stream has gone quiet -- device is in command mode
                }

                // Still noisy -- the toggle command was probably lost in the flood. Retry.
            }

            // Gave up after maxAttempts -- leave IsStreamingStopped as last observed rather
            // than throwing; see remarks above.
        }

        /// <summary>
        /// Waits for a gap of quietPeriodMs with no DataReceived activity, up to maxWaitMs
        /// total. Returns true if a quiet gap was seen, false if data was still arriving
        /// when maxWaitMs ran out. Every line seen while waiting still fires DataReceived
        /// as normal (it's just boot chatter / telemetry -- nothing is discarded silently).
        /// </summary>
        private Task<bool> WaitForQuietAsync(int quietPeriodMs, int maxWaitMs)
        {
            var tcs = new TaskCompletionSource<bool>();
            int settled = 0; // guards against the two timers racing each other right at the boundary
            Timer overallTimer = null;
            Timer quietTimer = null;

            void ResetQuietTimer()
            {
                // A DataReceived line can race a timer callback that already fired and is
                // mid-disposal; Change() on a disposed Timer throws ObjectDisposedException,
                // which would otherwise escape into SerialPort's raise-event thread.
                try { quietTimer.Change(quietPeriodMs, Timeout.Infinite); }
                catch (ObjectDisposedException) { }
            }

            void Finish(bool quiet)
            {
                if (Interlocked.Exchange(ref settled, 1) != 0) return; // already finished
                DataReceived -= OnData;
                overallTimer.Dispose();
                quietTimer.Dispose();
                tcs.TrySetResult(quiet);
            }

            void OnData(string _) => ResetQuietTimer();

            overallTimer = new Timer(_ => Finish(false), null, maxWaitMs, Timeout.Infinite);
            quietTimer = new Timer(_ => Finish(true), null, quietPeriodMs, Timeout.Infinite);
            DataReceived += OnData;

            return tcs.Task;
        }

        /// <summary>
        /// Registers a command whose response is identified by a fixed "-&gt; TagPrefix"
        /// marker line, so SendTaggedAsync(command) knows what to wait for. Call this once
        /// up front (e.g. in your form's constructor) for every command that has this style
        /// of response -- fill in your full command table here.
        /// </summary>
        public void RegisterTaggedCommand(string command, string tagPrefix, int lineCount = 1)
        {
            _taggedCommands[command] = new TaggedResponse(tagPrefix, lineCount);
        }

        /// <summary>
        /// Sends a registered tagged command and returns all of its response lines
        /// (including the leading "-&gt; ..." line) once fully received. Safe to call whether
        /// continuous streaming is running or stopped -- unrelated lines arriving in
        /// between are simply left alone for DataReceived to report.
        ///
        /// If the port isn't open, or nothing matches within timeoutMs, returns an array of
        /// LineCount elements each set to an "ERROR: ..." string rather than throwing --
        /// same convention as SendExclusiveAsync.
        /// </summary>
        public Task<string[]> SendTaggedAsync(string command, int timeoutMs = 3000)
        {
            if (!_taggedCommands.TryGetValue(command, out var info))
                throw new InvalidOperationException(
                    $"Command \"{command}\" has not been registered via RegisterTaggedCommand.");

            if (_port == null || !_port.IsOpen)
                return Task.FromResult(ErrorLines("SendTaggedAsync() ERROR: Serial port is not open", info.LineCount));

            var pending = new PendingTagged(info);

            lock (_stateLock)
            {
                if (!_pendingByTag.TryGetValue(info.TagPrefix, out var queue))
                {
                    queue = new Queue<PendingTagged>();
                    _pendingByTag[info.TagPrefix] = queue;
                }
                queue.Enqueue(pending);
            }

            WriteRaw(command);
            return WaitTaggedWithTimeout(pending.Completion.Task, timeoutMs, command, info.LineCount);
        }

        /// <summary>
        /// Sends a plain command and returns the single next line as its reply -- the
        /// original blocking Send() behaviour, now async and non-blocking on the caller's
        /// thread. Only valid while continuous streaming is stopped (IsStreamingStopped);
        /// throws InvalidOperationException if streaming is currently running (a
        /// programmer-usage error, not a runtime/hardware one, so it isn't turned into an
        /// "ERROR: ..." string like the cases below).
        ///
        /// If the port isn't open, or no reply arrives within timeoutMs, returns an
        /// "ERROR: ..." string instead of throwing.
        /// </summary>
        public Task<string> SendExclusiveAsync(string command, int timeoutMs = 3000)
        {
            if (!IsStreamingStopped)
                throw new InvalidOperationException(
                    "SendExclusiveAsync requires continuous streaming to be stopped -- " +
                    "call StopStreamingAsync() first, or use SendTaggedAsync for a registered command.");

            if (_port == null || !_port.IsOpen)
                return Task.FromResult("SendExclusiveAsync() ERROR: Serial port is not open");

            var tcs = new TaskCompletionSource<string>();
            lock (_stateLock)
            {
                _pendingExclusive.Enqueue(tcs);
            }

            WriteRaw(command);
            return WaitExclusiveWithTimeout(tcs.Task, timeoutMs, command);
        }

        /// <summary>
        /// Sends a command whose output you don't need tied back to this specific request
        /// (e.g. a "dump everything" command) -- writes it and returns immediately.
        /// Whatever comes back just flows through DataReceived like continuous telemetry.
        /// </summary>
        public void SendFireAndForget(string command) => WriteRaw(command);

        /// <summary>Starts continuous streaming by sending the toggle command, then waits
        /// briefly to confirm data actually starts flowing before updating IsStreamingStopped.</summary>
        public async Task StartStreamingAsync(int confirmWithinMs = 1000)
        {
            var tcs = new TaskCompletionSource<bool>();
            void OnData(string _) => tcs.TrySetResult(true);
            DataReceived += OnData;
            try
            {
                WriteRaw(StreamingToggleCommand);
                var completed = await Task.WhenAny(tcs.Task, Task.Delay(confirmWithinMs));
                IsStreamingStopped = completed != tcs.Task; // only mark "running" if we actually saw data
            }
            finally
            {
                DataReceived -= OnData;
            }
        }

        /// <summary>Stops continuous streaming by sending the toggle command and waiting
        /// for the stream to actually go quiet, same detection OpenAndSilenceEsp32Async uses.</summary>
        public async Task StopStreamingAsync(int quietPeriodMs = 300, int maxWaitMs = 2000)
        {
            WriteRaw(StreamingToggleCommand);
            IsStreamingStopped = await WaitForQuietAsync(quietPeriodMs, maxWaitMs);
        }

        public void FlushInput()
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");
            _port.DiscardInBuffer();
            lock (_stateLock) _rxBuffer.Clear();
        }

        public void Close()
        {
            if (_port != null)
            {
                _port.DataReceived -= OnPortDataReceived;
                if (_port.IsOpen) _port.Close();
                _port.Dispose();
                _port = null;
            }
        }

        public void Dispose() => Close();

        // -----------------------------------------------------------------------------
        // Internals
        // -----------------------------------------------------------------------------

        private static string[] ErrorLines(string message, int count)
        {
            var lines = new string[count];
            for (int i = 0; i < count; i++) lines[i] = message;
            return lines;
        }

        private static async Task<string> WaitExclusiveWithTimeout(Task<string> task, int timeoutMs, string command)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            if (completed != task)
                return $"ERROR: Timeout waiting for reply to \"{command}\"";
            return await task; // never faults -- only ever completed via TrySetResult
        }

        private static async Task<string[]> WaitTaggedWithTimeout(Task<string[]> task, int timeoutMs, string command, int lineCount)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            if (completed != task)
                return ErrorLines($"ERROR: Timeout waiting for reply to \"{command}\"", lineCount);
            return await task; // never faults -- only ever completed via TrySetResult
        }

        private void WriteRaw(string message)
        {
            if (_port == null || !_port.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");
            // NOTE: intentionally NOT appending "\r\n" here. The firmware's commands are
            // single characters read raw, with no terminator expected on the way in --
            // only replies come back "\r\n"-terminated. If a future multi-character
            // command needs one, send it explicitly: WriteRaw(cmd + "\r\n").
            _port.Write(message);
        }

        private void OnPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string chunk;
            try
            {
                chunk = _port.ReadExisting();
            }
            catch (Exception)
            {
                return; // port closing/closed mid-read -- nothing useful to do
            }

            List<string> completeLines = null;
            lock (_stateLock)
            {
                _rxBuffer.Append(chunk);

                int newlineIndex;
                while ((newlineIndex = IndexOfLineEnd(_rxBuffer, out int lineLength)) >= 0)
                {
                    string line = _rxBuffer.ToString(0, lineLength);
                    _rxBuffer.Remove(0, newlineIndex + 1);

                    (completeLines ?? (completeLines = new List<string>())).Add(line);
                }
            }

            if (completeLines == null) return;

            foreach (string line in completeLines)
            {
                DispatchLine(line);
            }
        }

        // Finds "\r\n", "\n", or a bare "\r" (some firmware/terminal combos are
        // inconsistent about which they send) -- returns the index of the character
        // right after the terminator, and outputs the line's length excluding it.
        private static int IndexOfLineEnd(StringBuilder buffer, out int lineLength)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == '\n')
                {
                    lineLength = (i > 0 && buffer[i - 1] == '\r') ? i - 1 : i;
                    return i;
                }
                if (buffer[i] == '\r' && (i + 1 >= buffer.Length || buffer[i + 1] != '\n'))
                {
                    lineLength = i;
                    return i;
                }
            }
            lineLength = -1;
            return -1;
        }

        // What DispatchLine decided to do with one incoming line, resolved while holding
        // _stateLock, then acted on afterward (outside the lock, so a caller's
        // continuation can never deadlock against another incoming line's dispatch).
        private enum LineAction { Unmatched, CompleteTagged, CompleteExclusive }

        private void DispatchLine(string line)
        {
            LineAction action = LineAction.Unmatched;
            TaskCompletionSource<string[]> tagCompletion = null;
            string[] tagLines = null;
            TaskCompletionSource<string> exclusiveCompletion = null;

            lock (_stateLock)
            {
                if (_collecting != null)
                {
                    // A multi-line tagged response is already in progress: this line is an
                    // unconditional continuation line, never re-classified against anything else.
                    _collectingLines.Add(line);
                    if (_collectingLines.Count >= _collecting.Info.LineCount)
                    {
                        tagCompletion = _collecting.Completion;
                        tagLines = _collectingLines.ToArray();
                        action = LineAction.CompleteTagged;
                        _collecting = null;
                        _collectingLines.Clear();
                    }
                }
                else
                {
                    PendingTagged newlyMatched = TryMatchTag(line);
                    if (newlyMatched != null)
                    {
                        if (newlyMatched.Info.LineCount == 1)
                        {
                            tagCompletion = newlyMatched.Completion;
                            tagLines = new[] { line };
                            action = LineAction.CompleteTagged;
                        }
                        else
                        {
                            _collecting = newlyMatched;
                            _collectingLines.Clear();
                            _collectingLines.Add(line);
                        }
                    }
                    else if (IsStreamingStopped && _pendingExclusive.Count > 0)
                    {
                        // Not a tag match. While streaming is stopped, an untagged line is
                        // the reply to the oldest pending exclusive request, if any.
                        exclusiveCompletion = _pendingExclusive.Dequeue();
                        action = LineAction.CompleteExclusive;
                    }
                }
            }

            switch (action)
            {
                case LineAction.CompleteTagged:
                    tagCompletion.TrySetResult(tagLines);
                    break;
                case LineAction.CompleteExclusive:
                    exclusiveCompletion.TrySetResult(line);
                    break;
                default:
                    DataReceived?.Invoke(line);
                    break;
            }
        }

        // Must be called while holding _stateLock. Returns the dequeued pending request
        // if `line` starts a tagged response, else null.
        private PendingTagged TryMatchTag(string line)
        {
            foreach (var kvp in _pendingByTag)
            {
                Queue<PendingTagged> queue = kvp.Value;
                if (queue.Count == 0) continue;

                if (line.StartsWith("-> " + kvp.Key, StringComparison.Ordinal))
                    return queue.Dequeue();
            }
            return null;
        }

        private sealed class PendingTagged
        {
            public TaggedResponse Info { get; }
            public TaskCompletionSource<string[]> Completion { get; } = new TaskCompletionSource<string[]>();
            public PendingTagged(TaggedResponse info) => Info = info;
        }
    }
}
