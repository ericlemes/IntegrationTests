using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace IntegrationTests.TestClasses.Server.TcpServer2
{
	internal class ConnectionContext
	{
		public bool FirstResponse
		{
			get;
			set;			
		}

		private NetworkStream clientStream;
		public NetworkStream ClientStream
		{
			get { return clientStream; }
		}

		private Queue<TcpServer2OutputStreamContext> outputQueue = new Queue<TcpServer2OutputStreamContext>();
		public Queue<TcpServer2OutputStreamContext> OutputQueue
		{
			get { return outputQueue; }
		}

		public ConnectionContext(NetworkStream clientStream)
		{
			this.clientStream = clientStream;
		}
	}
}
