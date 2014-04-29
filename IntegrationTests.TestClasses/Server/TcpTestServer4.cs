using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using IntegrationTests.ServiceClasses;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using IntegrationTests.TestClasses.Server.TcpServer4;
using System.Threading.Tasks;

namespace IntegrationTests.TestClasses.Server
{
	public class TcpTestServer4 : Microsoft.Build.Utilities.Task
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

			TcpListener tcpListener = (TcpListener)result.AsyncState;			
			TcpClient tcpClient = tcpListener.EndAcceptTcpClient(result);
			tcpClient.SendTimeout = 10000;
			tcpClient.ReceiveTimeout = 10000;
			NetworkStream clientStream = tcpClient.GetStream();

			ConnectionContextTPL ctx = new ConnectionContextTPL(clientStream, ConnString, this, Log);
			ctx.StartProcessingInputQueue();
			BeginRead(ctx);

			tcpListener.BeginAcceptTcpClient(BeginAcceptSocketCallback, tcpListener);
		}

		public void BeginRead(ConnectionContextTPL connectionContext)
		{
			InputStreamContext ctx = new InputStreamContext(connectionContext, bufferSize);

			Task<int>.Factory.FromAsync<byte[], int, int>(ctx.ConnectionContext.ClientStream.BeginRead,
				ctx.ConnectionContext.ClientStream.EndRead, ctx.Header, 0, sizeof(long), ctx).ContinueWith(BeginReadCallback);
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
			Task<int>.Factory.FromAsync<byte[], int, int>(ctx.ConnectionContext.ClientStream.BeginRead,
				ctx.ConnectionContext.ClientStream.EndRead, ctx.Buffer, 0, bytesToRead, ctx).ContinueWith(BeginReadCallback);			
		}

		private void BeginReadCallback(Task<int> task)
		{
			InputStreamContext ctx = task.AsyncState as InputStreamContext;
			int bytesRead = task.Result;

			if (!ctx.HeaderRead)
			{
				readCount++;                
				ctx.ProcessHeaderAfterRead();
                if (ctx.RemainingBytes > 20000)
                {
                    Log.LogMessage("Corrupt Header. String Representation: " + Encoding.UTF8.GetString(ctx.Header) + " Long Representation: " + ctx.RemainingBytes.ToString());
                    foreach (byte b in ctx.Header)
                        Log.LogMessage(b.ToString());
                    throw new Exception("Corrupt header.");
                }
                
                Log.LogMessage("Reading ID: " + readCount.ToString() + " Bytes read: " + bytesRead + " Remaining bytes: " + ctx.RemainingBytes.ToString());

				if (ctx.RemainingBytes == 0)
					EnqueueEmptyHeader(ctx);
				else
					ReadChunk(ctx);
			}
			else
			{
				ctx.ProcessChunkAfterRead(bytesRead);
                Log.LogMessage("Chunk received: " + bytesRead.ToString() + " bytes read. Remaining: " + ctx.RemainingBytes.ToString());

				if (ctx.RemainingBytes == 0)
					ProcessRequest(ctx);
				else
					ReadChunk(ctx);
			}
		}

		public void BeginWriteCallback(Task task)
		{
			OutputStreamContext ctx = task.AsyncState as OutputStreamContext;		

			if (ctx.EmptyResponse)
				return;

			byte[] buffer = new byte[bufferSize];
			int bytesToWrite = ctx.OutputStream.Read(buffer, 0, buffer.Length);

			if (bytesToWrite > 0)
			{
				Task.Factory.FromAsync<byte[], int, int>(ctx.ConnectionContext.ClientStream.BeginWrite,
					ctx.ConnectionContext.ClientStream.EndWrite, buffer, 0, bytesToWrite, ctx).ContinueWith(BeginWriteCallback);				
			}
			else
			{				
				ctx.WriteCompleted.Set();
			}
		}

	}
}
