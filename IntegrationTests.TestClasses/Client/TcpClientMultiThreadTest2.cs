using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using System.IO;
using System.Net.Sockets;
using IntegrationTests.ServiceClasses;
using System.Diagnostics;
using System.Threading;
using Microsoft.Build.Framework;

namespace IntegrationTests.TestClasses.Client
{
	public class TcpClientMultiThreadTest2 : Task
	{

		[Required]
		public int TotalBatches
		{
			get;
			set;
		}

		[Required]
		public int BatchSize
		{
			get;
			set;
		}

		[Required]
		public string ConnString
		{
			get;
			set;
		}

		[Required]
		public string HostName
		{
			get;
			set;
		}

		[Required]
		public int Port
		{
			get;
			set;
		}

		private Queue<MemoryStream> memoryStreamQueue = new Queue<MemoryStream>();

		private StreamUtil util = new StreamUtil();		

		private int batchesSent = 0;

		private int recordCount = 1;

		private ManualResetEvent sendDone = new ManualResetEvent(false);
		private ManualResetEvent receiveDone = new ManualResetEvent(false);

		public override bool Execute()
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();			

			Log.LogMessage("Starting TCP transfer (multi thread) with " + TotalBatches.ToString() + " batchs with " + BatchSize.ToString() + " items each");

			TcpClient tcpClient = new TcpClient();
			tcpClient.Connect(HostName, Port);
			NetworkStream stream = tcpClient.GetStream();			

			WriteHeader(stream);
			ReadHeader(stream);

			sendDone.WaitOne();
			receiveDone.WaitOne();

			tcpClient.Close();
			watch.Stop();			

			Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

			return true;
		}

		private void WriteHeader(NetworkStream stream)
		{
			batchesSent++;

			if (batchesSent > TotalBatches)
			{
				//Mark end of transfer.
				byte[] b = new byte[sizeof(long)];
				b = BitConverter.GetBytes((long)0);
				stream.Write(b, 0, sizeof(long));
				sendDone.Set();
				return;
			}

			Log.LogMessage("Sending request " + batchesSent.ToString());

			TcpClient2OutputStreamContext ctx = new TcpClient2OutputStreamContext();
			ctx.OutputStream = new MemoryStream();
			ctx.ClientStream = stream;
			util.GenerateBigRequest(ctx.OutputStream, false, recordCount, recordCount + (BatchSize - 1));
			ctx.OutputStream.Seek(0, SeekOrigin.Begin);			

			byte[] header = BitConverter.GetBytes(ctx.OutputStream.Length);
			stream.BeginWrite(header, 0, header.Length, BeginWriteCallback, ctx);			

			recordCount += BatchSize;			
		}

		private void BeginWriteCallback(IAsyncResult result)
		{
			TcpClient2OutputStreamContext ctx = (TcpClient2OutputStreamContext)result.AsyncState;
			ctx.ClientStream.EndWrite(result);			

			if (ctx.OutputStream.Position < ctx.OutputStream.Length)
			{
				byte[] buffer = new byte[256];
				int read = ctx.OutputStream.Read(buffer, 0, buffer.Length);
				ctx.ClientStream.BeginWrite(buffer, 0, read, BeginWriteCallback, ctx);
			}
			else
			{
				WriteHeader(ctx.ClientStream);
			}
		}

		private int contextCount = 0;

		private void ReadHeader(NetworkStream stream)
		{
			contextCount++;
			TcpClient2InputStreamContext ctx = new TcpClient2InputStreamContext();
			ctx.ID = contextCount;
			ctx.ClientStream = stream;
			ctx.Header = new byte[sizeof(long)];
			ctx.HeaderRead = false;
			
			stream.BeginRead(ctx.Header, 0, ctx.Header.Length, BeginReadCallback, ctx);
		}

		private void BeginReadCallback(IAsyncResult result)
		{
			TcpClient2InputStreamContext ctx = (TcpClient2InputStreamContext)result.AsyncState;
			int bytesRead = ctx.ClientStream.EndRead(result);
			Log.LogMessage("Read " + bytesRead.ToString() + " bytes");
			
			if (!ctx.HeaderRead)
			{
				Log.LogMessage("Reading Header");
				ctx.ResponseSize = BitConverter.ToInt64(ctx.Header, 0);
				ctx.TotalRead = 0;
				Log.LogMessage("Response Size: " + ctx.ResponseSize.ToString());
				ctx.Header = null; //I don't need this buffer anymore. 
				ctx.HeaderRead = true;
				ctx.InputStream = new MemoryStream();
				ctx.InputStream.SetLength(ctx.ResponseSize);

				if (ctx.ResponseSize > 0)
				{
					ctx.Buffer = new byte[256];
					int bytesToRead = (ctx.ResponseSize - ctx.TotalRead) > ctx.Buffer.Length ? ctx.Buffer.Length : (int)(ctx.ResponseSize - ctx.TotalRead);
					Log.LogMessage("Reading " + bytesToRead.ToString() + " bytes");
					ctx.ClientStream.BeginRead(ctx.Buffer, 0, bytesToRead, BeginReadCallback, ctx);
				}
				else
					receiveDone.Set();
			}
			else
			{
				ctx.TotalRead += bytesRead;
				Log.LogMessage("Writing " + bytesRead.ToString() + " to input stream");
				ctx.InputStream.Write(ctx.Buffer, 0, bytesRead);				

				if (ctx.TotalRead < ctx.ResponseSize)					
				{					
					int bytesToRead = (ctx.ResponseSize - ctx.TotalRead) > ctx.Buffer.Length ? ctx.Buffer.Length : (int)(ctx.ResponseSize - ctx.TotalRead);
					Log.LogMessage("Reading " + bytesToRead.ToString() + " bytes");
					ctx.Buffer = new byte[256];
					ctx.ClientStream.BeginRead(ctx.Buffer, 0, bytesToRead, BeginReadCallback, ctx);
				}
				else
				{
					Log.LogMessage("Finished reading " + ctx.InputStream.Length.ToString() + " bytes");
					ctx.InputStream.Seek(0, SeekOrigin.Begin);
					Log.LogMessage("Before import stream " + ctx.ID.ToString());
					util.ImportarStream(ConnString, ctx.InputStream);
					Log.LogMessage("Stream imported " + ctx.ID.ToString());

					ReadHeader(ctx.ClientStream);
				}
			}														
		}
	}

	internal class TcpClient2InputStreamContext
	{
		public int ID
		{
			get;
			set;
		}

		public NetworkStream ClientStream
		{
			get;
			set;
		}

		public byte[] Header
		{
			get;
			set;
		}

		public byte[] Buffer
		{
			get;
			set;
		}

		public long ResponseSize
		{
			get;
			set;
		}

		public MemoryStream InputStream
		{
			get;
			set;
		}

		public bool HeaderRead
		{
			get;
			set;
		}

		public long TotalRead
		{
			get;
			set;
		}		
	}

	internal class TcpClient2OutputStreamContext
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
	}
}
