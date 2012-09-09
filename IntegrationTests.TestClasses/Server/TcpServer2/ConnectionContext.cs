using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using IntegrationTests.ServiceClasses;
using System.Threading;
using System.IO;

namespace IntegrationTests.TestClasses.Server.TcpServer2
{

	public class ConnectionContext
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

		private Queue<InputStreamContext> inputQueue = new Queue<InputStreamContext>();
		public Queue<InputStreamContext> InputQueue
		{
			get { return inputQueue; }
		}

		public ConnectionContext(NetworkStream clientStream, string connString, TcpTestServer2 server)
		{
			this.clientStream = clientStream;
			this.connString = connString;
			this.tcpServer = server;
		}

		private string connString;		

		private StreamUtil streamUtil = new StreamUtil();

		private Thread inputQueueThread;

		private TcpTestServer2 tcpServer;		

		public void ProcessInputQueue()
		{
			while (true)
			{
				InputStreamContext ctx = null;
				lock (this)
				{
					if (inputQueue.Count > 0)
						ctx = inputQueue.Dequeue();
				}

				if (ctx == null)
				{
					Thread.Sleep(250);
					continue;
				}

				OutputStreamContext outputContext = new OutputStreamContext(this);
				if (ctx.EmptyResponse)
				{
					outputContext.EmptyResponse = true;
					byte[] buffer = BitConverter.GetBytes((long)0);
					ClientStream.BeginWrite(buffer, 0, buffer.Length, tcpServer.BeginWriteCallback, outputContext);
				}
				else
				{
					ctx.RequestStream.Seek(0, SeekOrigin.Begin);
					streamUtil.ProcessClientBigRequest(connString, ctx.RequestStream, outputContext.OutputStream, false, null);
					outputContext.OutputStream.Seek(0, SeekOrigin.Begin);

					byte[] buffer = BitConverter.GetBytes(outputContext.OutputStream.Length);
					ClientStream.BeginWrite(buffer, 0, buffer.Length, tcpServer.BeginWriteCallback, outputContext);

					outputContext.WriteCompleted.WaitOne();
				}	
			}
		}

		public void StartProcessingInputQueue()
		{
			inputQueueThread = new Thread(ProcessInputQueue);
			inputQueueThread.Start();
		}

	}

}
