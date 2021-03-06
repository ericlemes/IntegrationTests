﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IntegrationTests.TestClasses.Server.TcpServer2
{
	public class InputStreamContext
	{
		private byte[] header = new byte[sizeof(long)];
		public byte[] Header
		{
			get { return header; }			
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

		private byte[] buffer;
		public byte[] Buffer
		{
			get { return buffer; }			
		}

		private ConnectionContext connectionContext;
		public ConnectionContext ConnectionContext
		{
			get { return connectionContext; }
		}		

		public InputStreamContext(ConnectionContext connectionContext, int bufferSize)
		{
			this.connectionContext = connectionContext;
			this.buffer = new byte[bufferSize];
		}

		public void ProcessHeaderAfterRead()
		{
			this.headerRead = true;
			this.remainingBytes = BitConverter.ToInt64(this.Header, 0);
		}

		public void ProcessChunkAfterRead(int bytesRead)
		{
			this.remainingBytes -= bytesRead;
			this.RequestStream.Write(this.Buffer, 0, bytesRead);
		}

		public bool EmptyResponse { 
			get; 
			set; 
		}
	}
}
