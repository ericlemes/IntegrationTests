using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using IntegrationTests.ServiceClasses;
using System.Threading;
using System.IO;
using Microsoft.Build.Utilities;

namespace IntegrationTests.TestClasses.Server.TcpServer3
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

		public ConnectionContext(NetworkStream clientStream, string connString, TcpTestServer3 server, TaskLoggingHelper log)
		{
			this.clientStream = clientStream;
			this.connString = connString;
			this.tcpServer = server;
			this.log = log;
		}

		private TaskLoggingHelper log;

		private string connString;				

		private Thread inputQueueThread;

		private TcpTestServer3 tcpServer;		

		public void ProcessInputQueue()
		{
			while (true)
			{
				try
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
						byte[] header = new byte[sizeof(long)+sizeof(int)];
						BitConverter.GetBytes((long)0).CopyTo(header, 0);
						BitConverter.GetBytes((int)0).CopyTo(header, sizeof(long));
						ClientStream.BeginWrite(header, 0, header.Length, tcpServer.BeginWriteCallback, outputContext);
					}
					else
					{
						ctx.RequestStream.Seek(0, SeekOrigin.Begin);
						log.LogMessage("Before process ID " + ctx.ID.ToString() + " size " + ctx.RequestStream.Length.ToString());
						StreamUtil.ProcessClientBigRequest(connString, ctx.RequestStream, outputContext.OutputStream, false, null, log);
						outputContext.OutputStream.Seek(0, SeekOrigin.Begin);
						outputContext.ID = ctx.ID;

						byte[] header = new byte[sizeof(long) + sizeof(int)];
						BitConverter.GetBytes(outputContext.OutputStream.Length).CopyTo(header, 0);
						BitConverter.GetBytes(ctx.ID).CopyTo(header, sizeof(long));
						ClientStream.BeginWrite(header, 0, header.Length, tcpServer.BeginWriteCallback, outputContext);

						outputContext.WriteCompleted.WaitOne();
					}
				}
				catch (Exception e)
				{
					log.LogMessage(e.Message);
					throw e;
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
