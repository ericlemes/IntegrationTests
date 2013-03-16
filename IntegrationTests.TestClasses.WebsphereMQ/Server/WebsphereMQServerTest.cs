using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using IntegrationTests.ServiceClasses;
using System.IO;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace IntegrationTests.TestClasses.WebsphereMQ.Server
{
    public class WebsphereMQServerTest : Task
    {
        [Required]
        public string ConnString
        {
            get;
            set;
        }

        [Required]
        public string QueueManagerName
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

        public override bool Execute()
        {
            Log.LogMessage("Starting Websphere MQ Server " + QueueManagerName);
						
            IBM.WMQ.MQQueueManager queueManager = new IBM.WMQ.MQQueueManager(QueueManagerName);
						Log.LogMessage("Opening queue " + InputQueueName);
            IBM.WMQ.MQQueue queue = queueManager.AccessQueue(InputQueueName, IBM.WMQ.MQC.MQOO_INPUT_SHARED);
            
            int count = 0;

            while (true)
            {
                try
                {
                    IBM.WMQ.MQMessage msg = new IBM.WMQ.MQMessage();										
                    queue.Get(msg);

                    MemoryStream ms = new MemoryStream();
                    MemoryStream respStream = new MemoryStream();

                    MQUtil.MQMessageToStream(msg, ms);
                    msg.ClearMessage();

                    StreamUtil.ProcessClientBigRequest(ConnString, ms, respStream, false, null);

                    MQUtil.StreamToMQMessage(respStream, msg);										
                    queueManager.Put(OutputQueueName, msg);										
                    queueManager.Commit();

                    msg.ClearMessage();

                    count++;

                    Log.LogMessage("Processed " + count.ToString());                    
                }
                catch (IBM.WMQ.MQException ex)
                {
                    if (ex.ReasonCode != IBM.WMQ.MQC.MQRC_NO_MSG_AVAILABLE)
                        throw;
                    else
                        Thread.Sleep(50);
                }
            }
        }

    }
}
