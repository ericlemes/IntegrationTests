using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace IntegrationTests.TestClasses.Server.TcpServer4
{
	public class OutputStreamContext
	{

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

		private ConnectionContextTPL connectionContext;
		public ConnectionContextTPL ConnectionContext
		{
			get { return connectionContext; }
		}

		public OutputStreamContext(ConnectionContextTPL connectionContext)
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
