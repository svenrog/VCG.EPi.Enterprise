using System;
using System.ComponentModel;

namespace VCG.EPi.Enterprise.IO
{
    public delegate void ProgressEventHandler(object sender, ProgressLogEventArgs e);

    public class ProgressLogEventArgs : ProgressChangedEventArgs
    {
        public string MessageStack { get; private set; }
        public ProgressLogEventArgs(int progress, string stack) : base(progress, null) { MessageStack = stack; }
    }
}
