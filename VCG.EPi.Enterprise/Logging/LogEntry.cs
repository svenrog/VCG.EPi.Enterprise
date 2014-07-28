using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCG.EPi.Enterprise.Logging
{
    public class LogEntry : ILogEntry
    {
        protected string _message;
        protected MessageType _type;
        protected DateTime _created;

        public virtual string Message { get { return _message; } }
        public virtual MessageType Type { get { return _type; } }
        public virtual DateTime Created { get { return _created; } }

        public LogEntry(string message)
        {
            _created = DateTime.Now;
            _message = message;
            _type = MessageType.Message;

        }
        public LogEntry(string message, MessageType type)
        {
            _created = DateTime.Now;
            _message = message;
            _type = type;
        }

    }
}
