﻿using System;
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
		private int writeCount = 0;		

		private const int bufferSize = 100 * 1024;

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

			ConnectionContext ctx = new ConnectionContext(clientStream);			
			BeginRead(ctx);

			tcpListener.BeginAcceptTcpClient(BeginAcceptSocketCallback, tcpListener);
		}

		private void BeginRead(ConnectionContext connectionContext)
		{
			InputStreamContext ctx = new InputStreamContext(connectionContext, bufferSize);						
			ctx.ConnectionContext.ClientStream.BeginRead(ctx.Header, 0, sizeof(long), BeginReadCallback, ctx);					
		}

		private void ProcessRequest(InputStreamContext ctx)
		{
			OutputStreamContext outputContext = new OutputStreamContext(ctx.ConnectionContext);
			ctx.RequestStream.Seek(0, SeekOrigin.Begin);
			streamUtil.ProcessClientBigRequest(ConnString, ctx.RequestStream, outputContext.OutputStream, false, null);
			outputContext.OutputStream.Seek(0, SeekOrigin.Begin);
			byte[] buffer = BitConverter.GetBytes(outputContext.OutputStream.Length);
			writeCount++;
			Log.LogMessage("Finished reading " + readCount.ToString());
			Log.LogMessage("Writing " + writeCount.ToString());
			BeginRead(ctx.ConnectionContext);
			if (ctx.ConnectionContext.FirstResponse)
			{
				ctx.ConnectionContext.FirstResponse = false;
				outputContext.ConnectionContext.ClientStream.BeginWrite(buffer, 0, buffer.Length, BeginWriteCallback, outputContext);
			}
			else
			{
				ctx.ConnectionContext.OutputQueue.Enqueue(outputContext);
			}
		}

		private void EnqueueEmptyHeader(InputStreamContext ctx)
		{
			Log.LogMessage("Writing empty header");
			OutputStreamContext outputContext = new OutputStreamContext(ctx.ConnectionContext);
			outputContext.EmptyResponse = true;
			ctx.ConnectionContext.OutputQueue.Enqueue(outputContext);					
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

		private void BeginWriteCallback(IAsyncResult result)
		{
			OutputStreamContext ctx = result.AsyncState as OutputStreamContext;
			ctx.ConnectionContext.ClientStream.EndWrite(result);

			if (ctx.EmptyResponse)
				return;

			byte[] buffer = new byte[bufferSize];
			int bytesWritten = ctx.OutputStream.Read(buffer, 0, buffer.Length);

			if (bytesWritten > 0)
			{
				ctx.ConnectionContext.ClientStream.BeginWrite(buffer, 0, bytesWritten, BeginWriteCallback, ctx);
			}
			else
			{
				Log.LogMessage("Finished writing " + writeCount.ToString());				

				OutputStreamContext ctx2 = ctx.ConnectionContext.OutputQueue.Dequeue();				
				if (ctx2.EmptyResponse)
				{
					buffer = BitConverter.GetBytes((long)0);
					ctx2.ConnectionContext.ClientStream.BeginWrite(buffer, 0, buffer.Length, BeginWriteCallback, ctx2);
				}
				else
				{
					buffer = BitConverter.GetBytes(ctx2.OutputStream.Length);
					ctx2.ConnectionContext. ClientStream.BeginWrite(buffer, 0, buffer.Length, BeginWriteCallback, ctx2);
				}
			}
		}
	}

	


}
