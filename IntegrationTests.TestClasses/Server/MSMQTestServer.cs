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
                
        private int messageCount = 0;                
        private StreamUtil util = new StreamUtil();
        private MessageQueue inputQueue;
        private MessageQueue outputQueue;        

        public override bool  Execute()
        {
            Log.LogMessage("Starting MSMQ Server");

            outputQueue = new MessageQueue(OutputQueueName);

            inputQueue = new MessageQueue(InputQueueName);
            inputQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(queue_ReceiveCompleted);
            inputQueue.BeginReceive();

            while (true)
                Thread.Sleep(250);                        
        }

        private void queue_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e)
        {
            
            Log.LogMessage("Message received " + messageCount.ToString());
            messageCount++;

            Message resp = new Message();
            resp.BodyStream = new MemoryStream();
            
            util.ProcessClientBigRequest(ConnString, e.Message.BodyStream, resp.BodyStream, false, null);
            resp.BodyStream.Seek(0, SeekOrigin.Begin);
            
            outputQueue.Send(resp, MessageQueueTransactionType.Single);

            inputQueue.BeginReceive();
        }


    }
}
