using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCG.EPi.Enterprise.Logging
{
    public class LogBase : ILog
    {
        protected IList<ILogEntry> _logStack;
        protected int _lastGetIndex = 0;

        public LogBase()
        {
            Clear();
        }

        public virtual void Log(string message, MessageType type = MessageType.Message)
        {
            var entry = new LogEntry(message, type);
            _logStack.Add(entry);
        }

        public virtual void Clear()
        {
            _logStack = new List<ILogEntry>();
        }

        public virtual string GetLog(int size = int.MaxValue)
        {
            var skip = size == int.MaxValue ? 0 : _logStack.Count - size - 1;

            var messages = _logStack.Skip(skip);

            return InternalGetLog(messages);
        }

        public virtual string GetLogChanges()
        {
            var skip = _lastGetIndex;

            var messages = _logStack.Skip(skip);

            _lastGetIndex = _logStack.Count;

            return InternalGetLog(messages);
        }

        protected virtual string InternalGetLog(IEnumerable<ILogEntry> entries)
        {
            return string.Join("", entries.Select(e => string.Format("{0}: {1}: {2} \r\n", e.Created, e.Type, e.Message)));
        }
    }
}
