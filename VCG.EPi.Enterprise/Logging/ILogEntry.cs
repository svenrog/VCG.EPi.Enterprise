using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCG.EPi.Enterprise.Logging
{
    public interface ILogEntry
    {
        string Message { get; }
        MessageType Type { get; }
        DateTime Created { get; }
    }
}
