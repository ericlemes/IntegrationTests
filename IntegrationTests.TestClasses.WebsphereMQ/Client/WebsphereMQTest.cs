using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using IntegrationTests.ServiceClasses;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Diagnostics;

namespace IntegrationTests.TestClasses.WebsphereMQ.Client
{
    public class WebsphereMQTest : Task
    {
        [Required]
        public string QueueManagerName
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

        private IBM.WMQ.MQQueueManager queueManager;
        private IBM.WMQ.MQQueue inputQueue;
        private bool finished = false;
        private int messageCount = 0;        

        public override bool Execute()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Log.LogMessage("Starting Websphere MQ transfer with " + TotalBatches.ToString() + " batchs with " + BatchSize.ToString() + " items each");            

            queueManager = new IBM.WMQ.MQQueueManager(QueueManagerName);
            messageCount = TotalBatches;

            inputQueue = queueManager.AccessQueue(InputQueueName, IBM.WMQ.MQC.MQOO_INPUT_SHARED);
            Thread t = new Thread(ProcessMQQueue);
            t.Start();            

            int count = 1;
            for (int i = 0; i < TotalBatches; i++)
            {
                IBM.WMQ.MQMessage msg = new IBM.WMQ.MQMessage();

                MemoryStream ms = new MemoryStream();                
                StreamUtil.GenerateBigRequest(ms, false, count, count + (BatchSize - 1));
                MQUtil.StreamToMQMessage(ms, msg);
                queueManager.Put(OutputQueueName, msg);
                count += BatchSize;
                Log.LogMessage("Sent " + count.ToString());                
            }

            while (!finished)
                Thread.Sleep(250);

            watch.Stop();
            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

            return true;
        }

        private void ProcessMQQueue()
        {            
            bool keepListening = true;
            while (keepListening)
            {
                try
                {
                    IBM.WMQ.MQMessage msg = new IBM.WMQ.MQMessage();
                    inputQueue.Get(msg);
                    queueManager.Commit();
                    messageCount--;

                    MemoryStream ms = new MemoryStream();
                    MQUtil.MQMessageToStream(msg, ms);                    
                    msg.ClearMessage();

                    Log.LogMessage("Waiting for more " + messageCount.ToString());                    

                    StreamUtil.ImportarStream(ConnString, ms);

                    if (messageCount <= 0)
                    {
                        keepListening = false;
                        finished = true;
                    }
                }
                catch (IBM.WMQ.MQException exception)
                {
                    if (exception.ReasonCode != IBM.WMQ.MQC.MQRC_NO_MSG_AVAILABLE)
                        throw;
                    else
                        Thread.Sleep(100);
                }

            }
        }
    }
}
