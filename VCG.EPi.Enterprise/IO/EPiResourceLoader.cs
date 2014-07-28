using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Xml;

namespace VCG.EPi.Enterprise.IO
{
	public class EPiResourceLoader
	{
		public event ProgressStreamReportEventHandler OnProgress;
        public event ErrorEventHandler OnError;
        public event EventHandler OnComplete;

		private void TriggerOnProgress(object sender, ProgressStreamReportEventArgs args)
		{
			if (OnProgress != null)
				OnProgress.Invoke(sender, args);
		}

        private void TriggerOnError(object sender, ErrorEventArgs args)
        {
            if (OnError != null)
                OnError.Invoke(sender, args);
        }

        private void TriggerOnComplete(object sender, EventArgs args)
        {
            if (OnComplete != null)
                OnComplete.Invoke(sender, args);
        }

		public XmlDocument LoadResource(string path, string name)
		{
			try
			{
				using (FileStream file = File.Open(path, FileMode.Open, FileAccess.Read))
				{
					return LoadResource(file, name);
				}
			}
			catch (Exception ex)
			{
                TriggerOnError(this, new ErrorEventArgs(ex));
			}

            return null;
		}

		public XmlDocument LoadResource(Stream stream, string name)
		{
			try
			{
				using (ZipFile zip = new ZipFile(stream))
				{
					foreach (ZipEntry entry in zip)
					{
						if (!entry.IsFile) continue;
						if (!entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;

						using (Stream outStream = zip.GetInputStream(entry))
						{
                            using (ProgressStream progress = new ProgressStream(outStream, entry.CompressedSize))
                            {
                                progress.BytesRead += TriggerOnProgress;

							    XmlDocument xmldoc = new XmlDocument();
							    xmldoc.Load(progress);

								TriggerOnComplete(this, EventArgs.Empty);

							    return xmldoc;
                            }
						}
					}
				}
			}
			catch (Exception ex) 
            {
                TriggerOnError(this, new ErrorEventArgs(ex));
            }

			return null;
		}
	}
}
