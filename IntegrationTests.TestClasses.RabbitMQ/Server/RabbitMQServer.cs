using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using RabbitMQ.Client;
using Microsoft.Build.Framework;
using System.IO;
using IntegrationTests.ServiceClasses;
using System.Threading;

namespace IntegrationTests.TestClasses.RabbitMQ.Server
{
	public class RabbitMQServer : Task
	{
		[Required]
		public string ConnString
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


		public override bool Execute()
		{
			Log.LogMessage("Starting RabbitMQ Server");

			ConnectionFactory factory = new ConnectionFactory();
			factory.HostName = "localhost";
			IConnection conn = factory.CreateConnection();

			IModel inputChannel = conn.CreateModel();
			inputChannel.QueueDeclare(InputQueueName, false, false, false, null);

			IConnection conn2 = factory.CreateConnection();			

			IBasicProperties props = inputChannel.CreateBasicProperties();
			props.ContentType = "text/xml";
			props.DeliveryMode = 2; //persistent

			IModel outputChannel = conn2.CreateModel();			
			outputChannel.QueueDeclare(OutputQueueName, false, false, false, null);			

			int count = 0;

			while (true)
			{
				BasicGetResult result = inputChannel.BasicGet(InputQueueName, true);
				if (result == null)
				{
					Thread.Sleep(250);
					continue;
				}

				MemoryStream ms = new MemoryStream(result.Body);
				result = null; // let GC work.
				MemoryStream respStream = new MemoryStream();

				StreamUtil.ProcessClientBigRequest(ConnString, ms, respStream, false, null);

				outputChannel.BasicPublish("", OutputQueueName, props, respStream.GetBuffer());
				respStream = null; //let GC work

				count++;

				Log.LogMessage("Processed " + count.ToString());

			}
		}

	}
}
