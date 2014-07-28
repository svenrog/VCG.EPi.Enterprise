using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace VCG.EPi.Enterprise.IO
{
	public class StaticDataSource : IStaticDataSource, IDisposable
	{
		private Stream _stream;

		public Stream GetSource()
		{
			return _stream;
		}

		public void SetStream(Stream inputStream)
		{
			_stream = inputStream;
			_stream.Position = 0;
		}

		public void Dispose()
		{
			_stream.Dispose();
		}
	}
}