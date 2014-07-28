using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCG.EPi.Enterprise.Logging
{
    public delegate void LogEventHandler(object sender, LogEventArgs e);

    public class LogEventArgs : EventArgs
    {
        public ILogEntry Entry { get; private set; }
        public LogEventArgs(string message, MessageType type = MessageType.Message) { Entry = new LogEntry(message, type); }
    }
}
