using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace IntegrationTests.TestClasses.Server.TcpServer3
{
	internal class OutputStreamContext
	{
		public int ID
		{
			get;
			set;
		}

		private MemoryStream outputStream = new MemoryStream();
		public MemoryStream OutputStream
		{
			get { return outputStream; }
		}

		public bool EmptyResponse
		{
			get;
			set;
		}

		private ConnectionContext connectionContext;
		public ConnectionContext ConnectionContext
		{
			get { return connectionContext; }
		}

		public OutputStreamContext(ConnectionContext connectionContext)
		{
			this.connectionContext = connectionContext;
		}

		private ManualResetEvent writeCompleted = new ManualResetEvent(false);
		public ManualResetEvent WriteCompleted
		{
			get { return writeCompleted; }
		}
	}				


}
