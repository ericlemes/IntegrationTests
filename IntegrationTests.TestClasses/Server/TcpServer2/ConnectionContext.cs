using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace IntegrationTests.TestClasses.Server.TcpServer2
{

	internal class ConnectionContext
	{
		private bool firstResponse = true;

		public bool FirstResponse
		{
			get { return firstResponse; }
			set { firstResponse = value; }
		}

		private NetworkStream clientStream;

		public NetworkStream ClientStream
		{
			get { return clientStream; }
		}

		private Queue<OutputStreamContext> outputQueue = new Queue<OutputStreamContext>();
		public Queue<OutputStreamContext> OutputQueue
		{
			get { return outputQueue; }
		}

		public ConnectionContext(NetworkStream clientStream)
		{
			this.clientStream = clientStream;
		}
	}

}
