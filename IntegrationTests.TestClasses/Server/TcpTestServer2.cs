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
		private int writeCount = 0;		

		public override bool Execute()
		{
			Log.LogMessage("Listening to TCP Connections on port " + Port.ToString());

			IPAddress ipAddress = IPAddress.Any;
			TcpListener tcpListener = new TcpListener(ipAddress, Port);
			tcpListener.Start();
			tcpListener.BeginAcceptTcpClient(BeginAcceptSocketCallback, tcpListener);			

			while (true)
				Thread.Sleep(250);
		}

		private void BeginAcceptSocketCallback(IAsyncResult result)
		{
			Log.LogMessage("Received socket connection");

			streamUtil = new StreamUtil();

			TcpListener tcpListener = (TcpListener)result.AsyncState;
			TcpClient tcpClient = tcpListener.EndAcceptTcpClient(result);			
			NetworkStream clientStream = tcpClient.GetStream();

			TcpServer2ConnectionContext ctx = new TcpServer2ConnectionContext();
			ctx.FirstResponse = true;
			ctx.ClientStream = clientStream;

			BeginRead(ctx);

			tcpListener.BeginAcceptTcpClient(BeginAcceptSocketCallback, tcpListener);
		}

		private void BeginRead(TcpServer2ConnectionContext connectionContext)
		{
			TcpServer2InputStreamContext ctx = new TcpServer2InputStreamContext();
			ctx.RequestStream = new MemoryStream();
			ctx.ConnectionContext = connectionContext;
			ctx.ClientStream = connectionContext.ClientStream;
			ctx.Header = new byte[sizeof(long)];
			ctx.ClientStream.BeginRead(ctx.Header, 0, sizeof(long), BeginReadCallback, ctx);					
		}

		private void BeginReadCallback(IAsyncResult result)
		{			
			TcpServer2InputStreamContext ctx = result.AsyncState as TcpServer2InputStreamContext;
			int bytesRead = ctx.ClientStream.EndRead(result);			

			if (!ctx.HeaderRead)
			{
				readCount++;
				Log.LogMessage("Reading " + readCount.ToString());
				ctx.HeaderRead = true;
				ctx.RemainingBytes = BitConverter.ToInt64(ctx.Header, 0);
			}
			else
			{
				ctx.RemainingBytes -= bytesRead;
				ctx.RequestStream.Write(ctx.Buffer, 0, bytesRead);
				if (ctx.RemainingBytes == 0)
					ctx.FinishedReading = true;
			}

			if (ctx.RemainingBytes <= 0)
			{
				if (ctx.FinishedReading)
				{
					TcpServer2OutputStreamContext outputContext = new TcpServer2OutputStreamContext();
					outputContext.ConnectionContext = ctx.ConnectionContext;
					outputContext.OutputStream = new MemoryStream();
					
					ctx.RequestStream.Seek(0, SeekOrigin.Begin);					
					
					streamUtil.ProcessClientBigRequest(ConnString, ctx.RequestStream, outputContext.OutputStream, false, null);					

					outputContext.OutputStream.Seek(0, SeekOrigin.Begin);
					outputContext.ClientStream = ctx.ClientStream;

					byte[] buffer = BitConverter.GetBytes(outputContext.OutputStream.Length);

					writeCount++;
					Log.LogMessage("Finished reading " + readCount.ToString());
					Log.LogMessage("Writing " + writeCount.ToString());

					BeginRead(ctx.ConnectionContext);

					if (ctx.ConnectionContext.FirstResponse)
					{
						ctx.ConnectionContext.FirstResponse = false;
						outputContext.ClientStream.BeginWrite(buffer, 0, buffer.Length, BeginWriteCallback, outputContext);
					}
					else
					{
						ctx.ConnectionContext.OutputQueue.Enqueue(outputContext);
					}
				}
				else
				{
					Log.LogMessage("Writing empty header");
					TcpServer2OutputStreamContext outputContext = new TcpServer2OutputStreamContext();
					outputContext.ConnectionContext = ctx.ConnectionContext;
					outputContext.EmptyResponse = true;
					outputContext.ClientStream = ctx.ClientStream;
					ctx.ConnectionContext.OutputQueue.Enqueue(outputContext);					
				}
			}
			else
			{
				ctx.Buffer = new byte[256];
				int bytesToRead = ctx.RemainingBytes > ctx.Buffer.Length ? ctx.Buffer.Length : (int)ctx.RemainingBytes;								
				ctx.ClientStream.BeginRead(ctx.Buffer, 0, bytesToRead, BeginReadCallback, ctx);
			}
		}

		private void BeginWriteCallback(IAsyncResult result)
		{
			TcpServer2OutputStreamContext ctx = result.AsyncState as TcpServer2OutputStreamContext;
			ctx.ClientStream.EndWrite(result);

			if (ctx.EmptyResponse)
				return;

			byte[] buffer = new byte[256];
			int bytesWritten = ctx.OutputStream.Read(buffer, 0, buffer.Length);

			if (bytesWritten > 0)
			{
				ctx.ClientStream.BeginWrite(buffer, 0, bytesWritten, BeginWriteCallback, ctx);
			}
			else
			{
				Log.LogMessage("Finished writing " + writeCount.ToString());
				TcpServer2OutputStreamContext ctx2 = ctx.ConnectionContext.OutputQueue.Dequeue();
				if (ctx2.EmptyResponse)
				{
					buffer = BitConverter.GetBytes((long)0);
					ctx2.ClientStream.BeginWrite(buffer, 0, buffer.Length, BeginWriteCallback, ctx2);
				}
				else
				{
					buffer = BitConverter.GetBytes(ctx2.OutputStream.Length);
					ctx2.ClientStream.BeginWrite(buffer, 0, buffer.Length, BeginWriteCallback, ctx2);
				}
			}
		}
	}

	internal class TcpServer2OutputStreamContext
	{
		public MemoryStream OutputStream
		{
			get;
			set;
		}

		public NetworkStream ClientStream
		{
			get;
			set;
		}

		public bool EmptyResponse
		{
			get;
			set;
		}

		public TcpServer2ConnectionContext ConnectionContext
		{
			get;
			set;
		}
	}

	internal class TcpServer2InputStreamContext
	{
		public byte[] Header
		{
			get;
			set;
		}

		public NetworkStream ClientStream
		{
			get;
			set;
		}

		public MemoryStream RequestStream
		{
			get;
			set;
		}

		public long RemainingBytes
		{
			get;
			set;
		}

		public bool HeaderRead
		{
			get;
			set;
		}

		public byte[] Buffer
		{
			get;
			set;
		}

		public bool FinishedReading
		{
			get;
			set;
		}

		public TcpServer2ConnectionContext ConnectionContext
		{
			get;
			set;
		}
	}

	internal class TcpServer2ConnectionContext
	{
		public bool FirstResponse
		{
			get;
			set;
		}

		public NetworkStream ClientStream
		{
			get;
			set;
		}

		private Queue<TcpServer2OutputStreamContext> outputQueue = new Queue<TcpServer2OutputStreamContext>();
		public Queue<TcpServer2OutputStreamContext> OutputQueue
		{
			get { return outputQueue; }			
		}
	}
}
