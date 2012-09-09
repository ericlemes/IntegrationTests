using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Build.Framework;
using IntegrationTests.ServiceClasses;
using IntegrationTests.TestClasses.Server.TcpServer2;

namespace IntegrationTests.TestClasses.Server
{
	public class TcpTestServer2 : Task
	{

		[Required]
		public string ConnString
		{
			get;
			set;
		}

		private int port = 8081;
		public int Port
		{
			get { return port; }
			set { port = value; }
		}				

		private StreamUtil streamUtil;

		private int readCount = 0;		

		private const int bufferSize = 100 * 1024;

		private ManualResetEvent serverFinished = new ManualResetEvent(false);

		public override bool Execute()
		{
			Log.LogMessage("Listening to TCP Connections on port " + Port.ToString());

			IPAddress ipAddress = IPAddress.Any;
			TcpListener tcpListener = new TcpListener(ipAddress, Port);
			tcpListener.Start();
			tcpListener.BeginAcceptTcpClient(BeginAcceptSocketCallback, tcpListener);

			serverFinished.WaitOne();
			return true;
		}

		private void BeginAcceptSocketCallback(IAsyncResult result)
		{
			Log.LogMessage("Received socket connection");

			streamUtil = new StreamUtil();

			TcpListener tcpListener = (TcpListener)result.AsyncState;
			TcpClient tcpClient = tcpListener.EndAcceptTcpClient(result);			
			NetworkStream clientStream = tcpClient.GetStream();

			ConnectionContext ctx = new ConnectionContext(clientStream, ConnString, this);
			ctx.StartProcessingInputQueue();
			BeginRead(ctx);

			tcpListener.BeginAcceptTcpClient(BeginAcceptSocketCallback, tcpListener);
		}

		public void BeginRead(ConnectionContext connectionContext)
		{
			InputStreamContext ctx = new InputStreamContext(connectionContext, bufferSize);						
			ctx.ConnectionContext.ClientStream.BeginRead(ctx.Header, 0, sizeof(long), BeginReadCallback, ctx);					
		}

		private void ProcessRequest(InputStreamContext ctx)
		{
			lock (ctx.ConnectionContext)
				ctx.ConnectionContext.InputQueue.Enqueue(ctx);			
			BeginRead(ctx.ConnectionContext);			
		}

		private void EnqueueEmptyHeader(InputStreamContext ctx)
		{
			Log.LogMessage("Writing empty header");
			InputStreamContext inputContext = new InputStreamContext(ctx.ConnectionContext, bufferSize);
			inputContext.EmptyResponse = true;
			ctx.ConnectionContext.InputQueue.Enqueue(inputContext);					
		}

		private void ReadChunk(InputStreamContext ctx)
		{			
			int bytesToRead = ctx.RemainingBytes > ctx.Buffer.Length ? ctx.Buffer.Length : (int)ctx.RemainingBytes;
			ctx.ConnectionContext.ClientStream.BeginRead(ctx.Buffer, 0, bytesToRead, BeginReadCallback, ctx);
		}

		private void BeginReadCallback(IAsyncResult result)
		{			
			InputStreamContext ctx = result.AsyncState as InputStreamContext;
			int bytesRead = ctx.ConnectionContext.ClientStream.EndRead(result);			

			if (!ctx.HeaderRead)
			{
				readCount++;
				Log.LogMessage("Reading " + readCount.ToString());
				ctx.ProcessHeaderAfterRead();

				if (ctx.RemainingBytes == 0)
					EnqueueEmptyHeader(ctx);
				else
					ReadChunk(ctx);				
			}
			else
			{
				ctx.ProcessChunkAfterRead(bytesRead);

				if (ctx.RemainingBytes == 0)
					ProcessRequest(ctx);
				else
					ReadChunk(ctx);
			}		
		}

		public void BeginWriteCallback(IAsyncResult result)
		{
			OutputStreamContext ctx = result.AsyncState as OutputStreamContext;
			ctx.ConnectionContext.ClientStream.EndWrite(result);

			if (ctx.EmptyResponse)
				return;

			byte[] buffer = new byte[bufferSize];
			int bytesToWrite = ctx.OutputStream.Read(buffer, 0, buffer.Length);

			if (bytesToWrite > 0)
			{
				ctx.ConnectionContext.ClientStream.BeginWrite(buffer, 0, bytesToWrite, BeginWriteCallback, ctx);
			}
			else
			{
				Log.LogMessage("Finished writing ");
				ctx.WriteCompleted.Set();
			}
		}
	}

	


}
