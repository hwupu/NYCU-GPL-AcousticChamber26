using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GPL.Core;

namespace GPL.Logging
{
    /// <summary>
    /// CSV event log, one row per event, long format (generalized from HW2's ExperimentLog). Every row is flushed immediately so the
    /// data survives a crash or a headset power-off, and mirrored to a callback (console). A resumed session appends to the same
    /// file without a second header; <see cref="Open"/> refuses to append if the file's header differs (the chamber's columns changed).
    /// <c>elapsed_s</c> counts from the moment this log object was opened (after a resume: from the resume).
    /// </summary>
    public sealed class EventLog : IDisposable
    {
        static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        readonly TextWriter _writer;
        readonly IClock _clock;
        readonly Action<string> _mirror;
        readonly DateTime _start;
        readonly string[] _row;

        public LogSchema Schema { get; }
        public LogContext Context { get; }
        public int RowsWritten { get; private set; }

        public EventLog(TextWriter writer, IClock clock, LogSchema schema, LogContext context, bool writeHeader = true, Action<string> mirror = null)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            Context = context ?? new LogContext();
            _mirror = mirror;
            _start = clock.Now;
            _row = new string[schema.Columns.Count];
            if (writeHeader) WriteLine(schema.Header);
        }

        /// <summary>
        /// Opens (creates or appends to) a CSV file. A new or empty file gets the header. An existing file must start with exactly this
        /// schema's header, otherwise <see cref="InvalidOperationException"/> (never mix two layouts in one file).
        /// </summary>
        public static EventLog Open(string path, IClock clock, LogSchema schema, LogContext context, Action<string> mirror = null)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            bool hasContent = File.Exists(path) && new FileInfo(path).Length > 0;
            if (hasContent)
            {
                string first;
                using (var r = new StreamReader(path, Utf8NoBom)) first = r.ReadLine();
                if (first != schema.Header)
                    throw new InvalidOperationException($"'{Path.GetFileName(path)}' has a different column layout; cannot append.\nfile:   {first}\nschema: {schema.Header}");
            }
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            var writer = new StreamWriter(stream, Utf8NoBom) { AutoFlush = true };
            return new EventLog(writer, clock, schema, context, writeHeader: !hasContent, mirror: mirror);
        }

        /// <summary>
        /// Writes one event. <paramref name="values"/> may set chamber columns and override position columns
        /// (stage, block, condition, trial, attempt); it may not set event, timestamp or elapsed. Unknown columns throw.
        /// </summary>
        public void Write(string evt, IDictionary<string, string> values = null, string note = "")
        {
            if (string.IsNullOrEmpty(evt)) throw new ArgumentException("Event name required.", nameof(evt));
            for (int i = 0; i < _row.Length; i++) _row[i] = "";

            var now = _clock.Now;
            Put(LogSchema.Event, evt);
            Put(LogSchema.Timestamp, Fmt.Timestamp(now));
            Put(LogSchema.Elapsed, Fmt.F((now - _start).TotalSeconds));
            Put(LogSchema.SessionId, Context.SessionId);
            Put(LogSchema.Chamber, Context.Chamber);
            Put(LogSchema.ChamberVersion, Context.ChamberVersion);
            Put(LogSchema.StudyId, Context.StudyId);
            Put(LogSchema.Mode, Context.Mode);
            Put(LogSchema.Preset, Context.Preset);
            Put(LogSchema.ParamsHash, Context.ParamsHash);
            Put(LogSchema.Stage, Context.Stage);
            Put(LogSchema.Block, Context.Block);
            Put(LogSchema.Condition, Context.Condition);
            Put(LogSchema.Trial, Context.Trial.HasValue ? Fmt.I(Context.Trial.Value) : "");
            Put(LogSchema.Attempt, Context.Attempt.HasValue ? Fmt.I(Context.Attempt.Value) : "");
            Put(LogSchema.Note, note ?? "");

            if (values != null)
                foreach (var kv in values)
                {
                    if (LogSchema.Automatic.Contains(kv.Key)) throw new ArgumentException($"Column '{kv.Key}' is written by the log itself.");
                    int i = Schema.IndexOf(kv.Key);
                    if (i < 0) throw new ArgumentException($"Column '{kv.Key}' is not declared in the log schema.");
                    _row[i] = kv.Value ?? "";
                }

            WriteLine(Csv.Join(_row));
            RowsWritten++;
        }

        public void Write(string evt, string note) => Write(evt, null, note);

        void Put(string column, string value) => _row[Schema.IndexOf(column)] = value;

        void WriteLine(string line)
        {
            _writer.WriteLine(line);
            _writer.Flush();
            _mirror?.Invoke(line);
        }

        public void Dispose() => _writer.Dispose();
    }
}
