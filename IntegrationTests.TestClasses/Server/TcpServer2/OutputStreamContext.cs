using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IntegrationTests.TestClasses.Server.TcpServer2
{
	internal class OutputStreamContext
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

		private ConnectionContext connectionContext;
		public ConnectionContext ConnectionContext
		{
			get { return connectionContext; }
		}

		public OutputStreamContext(ConnectionContext connectionContext)
		{
			this.connectionContext = connectionContext;
		}
	}				


}
