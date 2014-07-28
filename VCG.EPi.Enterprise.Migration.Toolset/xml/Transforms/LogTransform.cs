using VCG.EPi.Enterprise.Migration.Toolset.Types;
using VCG.EPi.Enterprise.Migration.Toolset.Extensions;
using VCG.EPi.Enterprise.Xml.Transforms;
using System.Xml;
using System.Linq;
using VCG.EPi.Enterprise.IO;
using System;
using System.Collections.Generic;
using VCG.EPi.Enterprise.Xml;
using System.IO;
using VCG.EPi.Enterprise.Logging;
using VCG.EPi.Enterprise.Optimization;
using System.Xml.XPath;

namespace VCG.EPi.Enterprise.Migration.Toolset.Xml.Transforms
{
	public abstract class LogTransform : IXPathLogTransform
	{
		protected ILog _log;
        public event LogEventHandler OnLog;

        public abstract XmlDocument Transform(XmlDocument source);

        protected virtual void TriggerOnLogEntry(string message, MessageType type = MessageType.Message)
        {
            if (OnLog != null)
                OnLog(this, new LogEventArgs(message, type));
        }

		protected void Log(string message, MessageType type = MessageType.Message)
		{
			if (_log == null) return;
			TriggerOnLogEntry(message, type);
		}
    }
}
