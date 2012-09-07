using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IntegrationTests.TestClasses.Server.TcpServer2
{
	internal class InputStreamContext
	{
		public byte[] Header
		{
			get;
			set;
		}

		private MemoryStream requestStream = new MemoryStream();
		public MemoryStream RequestStream
		{
			get { return requestStream; }
		}

		private long remainingBytes;
		public long RemainingBytes
		{
			get { return remainingBytes; }
		}

		private bool headerRead;
		public bool HeaderRead
		{
			get { return headerRead; }
		}

		public byte[] Buffer
		{
			get;
			set;
		}

		private TcpServer2ConnectionContext connectionContext;
		public TcpServer2ConnectionContext ConnectionContext
		{
			get { return connectionContext; }
		}

		public bool FinishedReading
		{
			get;
			set;
		}

		public InputStreamContext(TcpServer2ConnectionContext connectionContext)
		{
			this.connectionContext = connectionContext;
		}

		public void ReadHeader()
		{
			this.headerRead = true;
			this.remainingBytes = BitConverter.ToInt64(this.Header, 0);
		}

		public void ProcessChunk(int bytesRead)
		{
			this.remainingBytes -= bytesRead;
			this.RequestStream.Write(this.Buffer, 0, bytesRead);
		}
	}
}
