using System;
using System.IO;

namespace VCG.EPi.Enterprise.IO
{
	public class ProgressStream : Stream
	{
		#region Private Data Members
		private Stream _innerStream;
        private long _size = -1;
		#endregion

		#region Constructor

        public ProgressStream(Stream streamToReportOn, long size) : this(streamToReportOn)
        {
            _size = size;
        }

		public ProgressStream(Stream streamToReportOn)
		{
			if (streamToReportOn != null)
			{
				_innerStream = streamToReportOn;

                if (_size < 0)
                    _size = _innerStream.Length;
			}
			else
			{
				throw new ArgumentNullException("streamToReportOn");
			}
		}
		#endregion

		#region Events

		public event ProgressStreamReportEventHandler BytesRead;
		public event ProgressStreamReportEventHandler BytesWritten;
		public event ProgressStreamReportEventHandler BytesMoved;

		protected virtual void OnBytesRead(int bytesMoved)
		{
			if (BytesRead != null)
			{
				var args = new ProgressStreamReportEventArgs(bytesMoved, _size, _innerStream.Position, true);

				BytesRead(this, args);
			}
		}

		protected virtual void OnBytesWritten(int bytesMoved)
		{
			if (BytesWritten != null)
			{
				var args = new ProgressStreamReportEventArgs(bytesMoved, _size, _innerStream.Position, false);
				BytesWritten(this, args);
			}
		}

		protected virtual void OnBytesMoved(int bytesMoved, bool isRead)
		{
			if (BytesMoved != null)
			{
				var args = new ProgressStreamReportEventArgs(bytesMoved, _size, _innerStream.Position, isRead);
				BytesMoved(this, args);
			}
		}
		#endregion

		#region Stream Members

		public override bool CanRead
		{
			get { return _innerStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _innerStream.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return _innerStream.CanWrite; }
		}

		public override void Flush()
		{
			_innerStream.Flush();
		}

		public override long Length
		{
			get 
            {
                if (_size > -1)
                    return _size;

                return _innerStream.Length; 
            }
		}

		public override long Position
		{
			get
			{
				return _innerStream.Position;
			}
			set
			{
				_innerStream.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var bytesRead = _innerStream.Read(buffer, offset, count);

			OnBytesRead(bytesRead);
			OnBytesMoved(bytesRead, true);

			return bytesRead;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _innerStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_innerStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_innerStream.Write(buffer, offset, count);

			OnBytesWritten(count);
			OnBytesMoved(count, false);
		}

		public override void Close()
		{
			_innerStream.Close();
			base.Close();
		}
		#endregion
	}

	public class ProgressStreamReportEventArgs : EventArgs
	{
		public int BytesMoved { get; private set; }		
		public long StreamLength { get; private set; }
		public long StreamPosition { get; private set; }
		public bool WasRead { get; private set; }

		public ProgressStreamReportEventArgs() : base() { }

		public ProgressStreamReportEventArgs(int bytesMoved, long streamLength, long streamPosition, bool wasRead) : this()
		{
			this.BytesMoved = bytesMoved;
			this.StreamLength = streamLength;
			this.StreamPosition = streamPosition;
			this.WasRead = WasRead;
		}
	}

	public delegate void ProgressStreamReportEventHandler(object sender, ProgressStreamReportEventArgs args);
}
