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

namespace IntegrationTests.TestClasses.Server
{
	public class MSMQTestServer : Task
	{
		[Required]
		public string OutputQueueName
		{
			get;
			set;
		}

		[Required]
		public string InputQueueName
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

		private int messageCount = 0;
		private int responseCount = 0;
		private MessageQueue inputQueue;
		private MessageQueue outputQueue;

		private ManualResetEvent processingDone = new ManualResetEvent(false);

		private Queue<Message> processingInputQueue = new Queue<Message>();

		public override bool Execute()
		{
			Log.LogMessage("Starting MSMQ Server");

			outputQueue = new MessageQueue(OutputQueueName);

			inputQueue = new MessageQueue(InputQueueName);
			inputQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(queue_ReceiveCompleted);
			inputQueue.BeginReceive();

			System.Threading.Tasks.Task.Factory.StartNew(ProcessInputQueue);

			processingDone.WaitOne();
			return true;
		}

		private void queue_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e)
		{
			lock (processingInputQueue)
				processingInputQueue.Enqueue(e.Message);

			Log.LogMessage("Message received " + messageCount.ToString());
			messageCount++;		

			inputQueue.BeginReceive();
		}

		private void ProcessInputQueue()
		{
			while (true)
			{
				Message msg = null;
				lock (processingInputQueue)
				{
					if (processingInputQueue.Count > 0)
						msg = processingInputQueue.Dequeue();
				}
				if (msg == null)
				{
					Thread.Sleep(50);
					continue;
				}					

				Message resp = new Message();
				resp.BodyStream = new MemoryStream();

				StreamUtil.ProcessClientBigRequest(ConnString, msg.BodyStream, resp.BodyStream, false, null);
				resp.BodyStream.Seek(0, SeekOrigin.Begin);

				responseCount++;

				Log.LogMessage("Putting response " + responseCount.ToString());
				outputQueue.Send(resp, IsTransactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);
				Log.LogMessage("Response on queue" + responseCount.ToString());
			}			
		}

	}
}
