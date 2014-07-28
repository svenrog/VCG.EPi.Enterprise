using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCG.EPi.Enterprise.Logging
{
    public interface ILog
    {
        void Log(string message, MessageType type = MessageType.Message);
        string GetLog(int size = int.MaxValue);
        string GetLogChanges();
        void Clear();
    }
}
