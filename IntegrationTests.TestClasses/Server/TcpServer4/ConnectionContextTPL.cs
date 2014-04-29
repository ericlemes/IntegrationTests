using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using IntegrationTests.TestClasses.Server.TcpServer2;
using System.Threading;
using IntegrationTests.ServiceClasses;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;

namespace IntegrationTests.TestClasses.Server.TcpServer4
{
	public class ConnectionContextTPL
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

		public ConnectionContextTPL(NetworkStream clientStream, string connString, TcpTestServer4 server, TaskLoggingHelper log)
		{
			this.clientStream = clientStream;
			this.connString = connString;
			this.tcpServer = server;
			this.log = log;
		}

		private TaskLoggingHelper log;

		private string connString;				

		private Thread inputQueueThread;

		private TcpTestServer4 tcpServer;		

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

                processCount++;
                

				OutputStreamContext outputContext = new OutputStreamContext(this);
				if (ctx.EmptyResponse)
				{
                    log.LogMessage("Processing " + processCount.ToString() + " empty response.");
					outputContext.EmptyResponse = true;
					byte[] buffer = BitConverter.GetBytes((long)0);
					System.Threading.Tasks.Task.Factory.FromAsync<byte[], int, int>(ClientStream.BeginWrite, ClientStream.EndWrite,
						buffer, 0, buffer.Length, outputContext).ContinueWith(tcpServer.BeginWriteCallback).ContinueWith(TaskExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);					
				}
				else
				{
                    log.LogMessage("Processing " + processCount.ToString() + " NON empty response.");

					ctx.RequestStream.Seek(0, SeekOrigin.Begin);
					try
					{
						StreamUtil.ProcessClientBigRequest(connString, ctx.RequestStream, outputContext.OutputStream, false, null);
					}
					catch (Exception ex)
					{
						log.LogMessage("Exception em ProcessInputQueue: " + ex.Message);
						log.LogMessage("Stream size: " + ctx.RequestStream.Length.ToString());
						ctx.RequestStream.Seek(0, SeekOrigin.Begin);
						StreamReader sr = new StreamReader(ctx.RequestStream);
						log.LogMessage(sr.ReadToEnd());
						throw;
					}
					outputContext.OutputStream.Seek(0, SeekOrigin.Begin);

					byte[] buffer = BitConverter.GetBytes(outputContext.OutputStream.Length);

					System.Threading.Tasks.Task.Factory.FromAsync<byte[], int, int>(ClientStream.BeginWrite, ClientStream.EndWrite,
						buffer, 0, buffer.Length, outputContext).ContinueWith(tcpServer.BeginWriteCallback).ContinueWith(TaskExceptionHandler, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);

					outputContext.WriteCompleted.WaitOne();
				}	
			}
		}

		private void TaskExceptionHandler(System.Threading.Tasks.Task task)
		{
			log.LogError(task.Exception.InnerException.Message);
			throw task.Exception.InnerException;
		}


		public void StartProcessingInputQueue()
		{
			inputQueueThread = new Thread(ProcessInputQueue);
			inputQueueThread.Start();
		}

        public int processCount { get; set; }
    }
}
