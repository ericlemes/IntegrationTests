using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using IntegrationTests.ServiceClasses;
using System.IO;
using Microsoft.Build.Framework;
using System.Threading.Tasks;

namespace IntegrationTests.TestClasses.Client
{
	public class TcpClientMultiThreadTest4 : Microsoft.Build.Utilities.Task
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

		private int batchesSent = 0;
		private int responsesProcessed = 0;

		private int recordCount = 1;

		private ManualResetEvent sendDone = new ManualResetEvent(false);
		private ManualResetEvent receiveDone = new ManualResetEvent(false);
		private ManualResetEvent processingDone = new ManualResetEvent(false);

		private const int bufferSize = 100 * 1024;

		private Queue<TcpClient2InputStreamContext> inputQueue = new Queue<TcpClient2InputStreamContext>();

		private TaskFactory writeTaskFactory = new TaskFactory();

		public override bool Execute()
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();

			Log.LogMessage("Starting TCP transfer (multi thread) with " + TotalBatches.ToString() + " batchs with " + BatchSize.ToString() + " items each");

			TcpClient tcpClient = new TcpClient();
			tcpClient.SendTimeout = 10000;
			tcpClient.ReceiveTimeout = 10000;
			tcpClient.Connect(HostName, Port);
			NetworkStream stream = tcpClient.GetStream();

			WriteHeader(stream);
			ReadHeader(stream);

			Thread t = new Thread(ProcessInputQueue);
			t.Start();

			sendDone.WaitOne();
			receiveDone.WaitOne();

			tcpClient.Close();
			watch.Stop();

			processingDone.WaitOne();

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
			StreamUtil.GenerateBigRequest(ctx.OutputStream, false, recordCount, recordCount + (BatchSize - 1));
			ctx.OutputStream.Seek(0, SeekOrigin.Begin);								

			byte[] header = BitConverter.GetBytes(ctx.OutputStream.Length);

			Task.Factory.FromAsync<byte[], int, int>(stream.BeginWrite, stream.EndWrite, header, 0, header.Length, ctx).ContinueWith(BeginWriteCallback).ContinueWith(TaskExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);			

			recordCount += BatchSize;
		}

		private void TaskExceptionHandler(Task task)
		{
			Log.LogError(task.Exception.InnerException.Message);			
		}

		private void BeginWriteCallback(Task task)
		{
			TcpClient2OutputStreamContext ctx = (TcpClient2OutputStreamContext)task.AsyncState;			

			if (ctx.OutputStream.Position < ctx.OutputStream.Length)
			{
				byte[] buffer = new byte[bufferSize];
				int read = ctx.OutputStream.Read(buffer, 0, buffer.Length);
				
				Task t = Task.Factory.FromAsync<byte[], int, int>(ctx.ClientStream.BeginWrite, ctx.ClientStream.EndWrite, buffer, 0, read, ctx);
				t.ContinueWith(BeginWriteCallback).ContinueWith(TaskExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);

				//ctx.ClientStream.BeginWrite(buffer, 0, read, BeginWriteCallback, ctx);
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

			Task<int>.Factory.FromAsync<byte[], int, int>(stream.BeginRead, stream.EndRead, ctx.Header, 0, ctx.Header.Length, ctx).ContinueWith(BeginReadCallback).ContinueWith(TaskExceptionHandler, TaskContinuationOptions.OnlyOnFaulted); ;			
		}

		private void BeginReadCallback(Task<int> task)
		{
			TcpClient2InputStreamContext ctx = (TcpClient2InputStreamContext)task.AsyncState;
			int bytesRead = task.Result;
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
					ctx.Buffer = new byte[bufferSize];
					int bytesToRead = (ctx.ResponseSize - ctx.TotalRead) > ctx.Buffer.Length ? ctx.Buffer.Length : (int)(ctx.ResponseSize - ctx.TotalRead);
					Log.LogMessage("Reading " + bytesToRead.ToString() + " bytes");

					Task<int>.Factory.FromAsync<byte[], int, int>(ctx.ClientStream.BeginRead, ctx.ClientStream.EndRead, ctx.Buffer, 0, bytesToRead,
						ctx).ContinueWith(BeginReadCallback).ContinueWith(TaskExceptionHandler, TaskContinuationOptions.OnlyOnFaulted); ;					
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
					ctx.Buffer = new byte[bufferSize];

					Task<int>.Factory.FromAsync<byte[], int, int>(ctx.ClientStream.BeginRead, ctx.ClientStream.EndRead, ctx.Buffer, 0, bytesToRead, ctx).ContinueWith(BeginReadCallback).ContinueWith(TaskExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
				}
				else
				{
					Log.LogMessage("Finished reading " + ctx.InputStream.Length.ToString() + " bytes");
					lock (inputQueue)
						inputQueue.Enqueue(ctx);

					ReadHeader(ctx.ClientStream);
				}
			}
		}

		public void ProcessInputQueue()
		{
			while (true)
			{
				TcpClient2InputStreamContext ctx = null;
				lock (inputQueue)
				{
					if (inputQueue.Count > 0)
						ctx = inputQueue.Dequeue();
				}

				if (ctx == null)
				{
					Thread.Sleep(250);
					continue;
				}

				Parallel.Invoke(() =>
				{
					ctx.InputStream.Seek(0, SeekOrigin.Begin);
					Log.LogMessage("Before import stream " + ctx.ID.ToString());
					StreamUtil.ImportarStream(ConnString, ctx.InputStream);
					Log.LogMessage("Stream imported " + ctx.ID.ToString());
					responsesProcessed++;
				});

				if (responsesProcessed == TotalBatches)
					break;

			}

			processingDone.Set();
		}


	}
}
