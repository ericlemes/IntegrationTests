using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using System.Diagnostics;
using Microsoft.Build.Framework;
using RabbitMQ.Client;
using System.IO;
using IntegrationTests.ServiceClasses;
using System.Threading;

namespace IntegrationTests.TestClasses.RabbitMQ.Client
{
	public class RabbitMQClientTest : Task
	{
		[Required]
		public string ConnString
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
		public string HostName
		{
			get;
			set;
		}

		private int messageCount;

		private ManualResetEvent finishedEvent = new ManualResetEvent(false);

		private IModel inputChannel;

		public override bool Execute()
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();

			Log.LogMessage("Starting Rabbit MQ transfer with " + TotalBatches.ToString() + " batchs with " + BatchSize.ToString() + " items each");

			ConnectionFactory factory = new ConnectionFactory();
			factory.HostName = HostName;
			IConnection conn = factory.CreateConnection();
			IModel channel = conn.CreateModel();			
			channel.QueueDeclare(OutputQueueName, false, false, false, null);			
			IBasicProperties props = channel.CreateBasicProperties();
			props.ContentType = "text/xml";
			props.DeliveryMode = 2; //persistent

			inputChannel = conn.CreateModel();			
			inputChannel.QueueDeclare(InputQueueName, false, false, false, null);			

			messageCount = TotalBatches;

			System.Threading.Tasks.Task.Factory.StartNew(ProcessInputQueue);

			int count = 1;
			for (int i = 0; i < TotalBatches; i++)
			{
				MemoryStream ms = new MemoryStream();
				StreamUtil.GenerateBigRequest(ms, false, count, count + (BatchSize - 1));
				channel.BasicPublish("", OutputQueueName, props, ms.GetBuffer());

				count += BatchSize;
				Log.LogMessage("Sent " + count.ToString());
			}

			finishedEvent.WaitOne();

			watch.Stop();
			Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");
			conn.Close();

			return true;
		}

		private void ProcessInputQueue()
		{			
			while (true)
			{
				BasicGetResult result = inputChannel.BasicGet(InputQueueName, true);
				if (result == null)
				{
					Thread.Sleep(250);
					continue;
				}
				MemoryStream ms = new MemoryStream(result.Body);
				messageCount--;

				Log.LogMessage("Waiting for more " + messageCount.ToString());

				StreamUtil.ImportarStream(ConnString, ms);

				if (messageCount <= 0)				
					break;									
			}

			finishedEvent.Set();
		}
	}
}
