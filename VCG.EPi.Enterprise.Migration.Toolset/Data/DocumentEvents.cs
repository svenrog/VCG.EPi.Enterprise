using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VCG.EPi.Enterprise.Migration.Toolset.Data
{
	public delegate void FileNotFoundEventHandler(object sender, FileNotFoundRetryEventArgs args);

	public class FileNotFoundRetryEventArgs : EventArgs
    {
        public string FileName { get; private set; }
		public Func<string, bool> RetryReference { get; private set; }

		public FileNotFoundRetryEventArgs(FileNotFoundException ex, Func<string, bool> retryReference) 
		{
			FileName = ex.FileName;
			RetryReference = retryReference;
		}
    }

	public delegate void DocumentLoadedEventHandler(object sender, DocumentLoadedEventArgs args);
	
	public class DocumentLoadedEventArgs : EventArgs
	{
		public Document Document { get; private set; }
		
		public DocumentLoadedEventArgs(Document document)
		{
			Document = document;
		}
	}

	public delegate void FileImportedEventHandler(object sender, FileImportedEventArgs args);

	public class FileImportedEventArgs : EventArgs
	{
		public XmlDocument File { get; private set; }
		
		public FileImportedEventArgs(XmlDocument file)
		{
			File = file;
		}
	}
}
