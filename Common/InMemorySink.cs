using System;
using System.Collections.Concurrent;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Atomex.Client.Desktop.Common
{
    class InMemorySink : ILogEventSink
    {
        private readonly Action<string> _logAction;

        public InMemorySink(Action<string> logAction)
        {
            _logAction = logAction;
        }

        readonly ITextFormatter _textFormatter = new MessageTemplateTextFormatter("[{Level}] {Message}{Exception}");

        public ConcurrentQueue<string> Events { get; } = new ConcurrentQueue<string>();

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));

            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);

            Events.Enqueue(renderSpace.ToString());

            _logAction.Invoke(renderSpace.ToString());
        }
    }
}