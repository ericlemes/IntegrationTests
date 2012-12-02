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

namespace IntegrationTests.TestClasses.Client
{
    public class MSMQTest : Task
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

        public string ConnString
        {
            get;
            set;
        }        

        private MessageQueue inputQueue;

        private int messageCount;
        private bool finished = false;

        public override bool Execute()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Log.LogMessage("Starting MSMQ transfer with " + TotalBatches.ToString() + " batchs with " + BatchSize.ToString() + " items each");

            messageCount = TotalBatches;

            inputQueue = new MessageQueue(InputQueueName);
            inputQueue.ReceiveCompleted += new ReceiveCompletedEventHandler(inQueue_ReceiveCompleted);
            inputQueue.BeginReceive();

            MessageQueue queue = new MessageQueue(OutputQueueName);            
            int count = 1;            

            for (int i = 0; i < TotalBatches; i++)
            {
                Message msg = new Message();
                msg.BodyStream = new MemoryStream();
                
                StreamUtil.GenerateBigRequest(msg.BodyStream, false, count, count + (BatchSize - 1));
                msg.BodyStream.Seek(0, SeekOrigin.Begin);
                queue.Send(msg, MessageQueueTransactionType.Single);                

                count += BatchSize;                
            }

            while (!finished)
                Thread.Sleep(250);

            watch.Stop();
            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

            return true;
        }

        private void inQueue_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e)
        {            
            Log.LogMessage("Received message. Waiting for " + messageCount.ToString() + " messages");

            messageCount--;
            StreamUtil.ImportarStream(ConnString, e.Message.BodyStream);            

            if (messageCount <= 0)
                finished = true;
            else
                inputQueue.BeginReceive();  
        }
    }
}
