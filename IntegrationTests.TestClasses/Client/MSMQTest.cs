using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Messaging;
using System.IO;
using IntegrationTests.ServiceClasses;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IntegrationTests.TestClasses.Client
{
	public class MSMQTest : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string InputQueueName
		{
			get;
			set;
		}

		[Required]
		public string OutputQueueName
		{
			get;
			set;
		}

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
		public bool IsTransactional
		{
			get;
			set;
		}

		private MessageQueue inputQueue;

		private int messageCount;		

		private ManualResetEvent processingDone = new ManualResetEvent(false);

		private Queue<Message> messageInputQueue = new Queue<Message>();

		public override bool Execute()
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();

			Log.LogMessage("Starting MSMQ transfer with " + TotalBatches.ToString() + " batchs with " + BatchSize.ToString() + " items each");

			messageCount = TotalBatches;

			inputQueue = new MessageQueue(InputQueueName);
			inputQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(inQueue_ReceiveCompleted);			
			inputQueue.BeginReceive();

			System.Threading.Tasks.Task.Factory.StartNew(ProcessInputQueue);

			MessageQueue queue = new MessageQueue(OutputQueueName);

			int count = 1;

			for (int i = 0; i < TotalBatches; i++)
			{
				Message msg = new Message();
				msg.BodyStream = new MemoryStream();

				StreamUtil.GenerateBigRequest(msg.BodyStream, false, count, count + (BatchSize - 1));
				msg.BodyStream.Seek(0, SeekOrigin.Begin);
				queue.Send(msg, IsTransactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);

				count += BatchSize;
			}
			Log.LogMessage("Messages sent");

			processingDone.WaitOne();			

			watch.Stop();
			Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

			return true;
		}

		int enqueueCount = 0;
		private void inQueue_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e)
		{		
			
			lock (messageInputQueue)
			{
				messageInputQueue.Enqueue(e.Message);
				enqueueCount++;
				Log.LogMessage("Enqueued " + enqueueCount.ToString() + " messages");
			}
		
			if (enqueueCount < TotalBatches)
				inputQueue.BeginReceive();
		}

		private void ProcessInputQueue()
		{
			while (true)
			{
				Message msg = null;

				lock (messageInputQueue)
				{
					if (messageInputQueue.Count > 0)
						msg = messageInputQueue.Dequeue();
				}

				if (msg == null)
				{
					Thread.Sleep(50);
					continue;
				}
				
				StreamUtil.ImportarStream(ConnString, msg.BodyStream);
				Log.LogMessage("Processed message " + messageCount.ToString());					
				messageCount--;					

				if (messageCount <= 0)
					break;
			}
			processingDone.Set();
		}
	}
}
