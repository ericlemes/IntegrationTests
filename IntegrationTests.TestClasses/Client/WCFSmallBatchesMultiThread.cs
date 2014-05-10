using IntegrationTests.ServiceClasses;
using IntegrationTests.ServiceClasses.Domain;
using Microsoft.Build.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTests.TestClasses.Client
{
    public  class WCFSmallBatchesMultiThread: Microsoft.Build.Utilities.Task
    {
        [Required]
        public int TotalBatches
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
        public string WebServiceUri
        {
            get;
            set;
        }

        [Required]
        public string EndpointType
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
        public bool UseTask
        {
            get;
            set;
        }

        public override bool Execute()
        {
            Binding binding;

            if (this.EndpointType == "http")
            {
                /*binding = new BasicHttpBinding();
                ((BasicHttpBinding)binding).MaxReceivedMessageSize = 2048 * 1024;*/
                binding = new CustomBinding();
                ((CustomBinding)binding).Elements.Add(new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8));
                ((CustomBinding)binding).SendTimeout = new TimeSpan(0, 50, 0);
                ((CustomBinding)binding).ReceiveTimeout = new TimeSpan(0, 50, 0);
                ((CustomBinding)binding).OpenTimeout = new TimeSpan(0, 50, 0);
                ((CustomBinding)binding).CloseTimeout = new TimeSpan(0, 50, 0);
                HttpTransportBindingElement element = new HttpTransportBindingElement();
                element.MaxReceivedMessageSize = 2048 * 1024;
                element.KeepAliveEnabled = false;
                element.RequestInitializationTimeout = new TimeSpan(1, 0, 0);
                ((CustomBinding)binding).Elements.Add(element);
            }
            else if (this.EndpointType == "nettcp")
            {
                binding = new NetTcpBinding();
                ((NetTcpBinding)binding).MaxReceivedMessageSize = 1024 * 1024;
                ((NetTcpBinding)binding).Security.Mode = SecurityMode.None;
                ((NetTcpBinding)binding).CloseTimeout = new TimeSpan(0, 50, 10);
                ((NetTcpBinding)binding).OpenTimeout = new TimeSpan(0, 50, 10);
                ((NetTcpBinding)binding).ReceiveTimeout = new TimeSpan(0, 50, 10);
                ((NetTcpBinding)binding).SendTimeout = new TimeSpan(0, 50, 10);
            }
            else
                throw new ArgumentException("Invalid value for EndpointType. Expected: http, nettcp");
            EndpointAddress address = new EndpointAddress(new Uri(WebServiceUri));

            IntegrationTestsService.IntegrationTestsServiceClient client = new IntegrationTestsService.IntegrationTestsServiceClient(binding, address);

            Log.LogMessage("Doing " + TotalBatches.ToString() + " batch calls with " + BatchSize.ToString() + " itens each");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            int count = 1;
            ConcurrentQueue<ManualResetEvent> waitObjectQueue = new ConcurrentQueue<ManualResetEvent>();
            Task task = null;
            for (int i = 0; i < TotalBatches; i++)
            {
                int start = count;
                int end = count + (BatchSize - 1);
                count += BatchSize;
                
                if (UseTask)
                {
                    task = Task.Factory.StartNew(() => {
                        ThreadJob(client, waitObjectQueue, start, end);
                    });                    
                }
                else
                {
                    Thread thread = new Thread(() => {
                        ThreadJob(client, waitObjectQueue, start, end);
                    });
                    thread.Start();
                }
            }

            if (task != null)
                task.Wait();

            while (waitObjectQueue.Count > 0){
                ManualResetEvent e;
                if (waitObjectQueue.TryDequeue(out e))
                    e.WaitOne();
            }

            watch.Stop();
            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

            return true;
        }

        private void ThreadJob(IntegrationTestsService.IntegrationTestsServiceClient client, ConcurrentQueue<ManualResetEvent> waitObjectQueue, int start, int end)
        {
            ManualResetEvent e = new ManualResetEvent(false);
            waitObjectQueue.Enqueue(e);

            ServiceTable[] stArray = client.GetServiceTables(start, end);
            foreach (ServiceTable t in stArray)
            {

                ServiceTable t2 = new ServiceTable();
                t2.ServiceTableID = t.ServiceTableID;
                t2.DescServiceTable = t.DescServiceTable;
                t2.Value = t.Value;
                t2.CreationDate = t.CreationDate;
                t2.StringField1 = t.StringField1;
                t2.StringField2 = t.StringField2;

                DAO.ProcessServiceTable(ConnString, t2);
            }
            e.Set();
        }

    }
}
