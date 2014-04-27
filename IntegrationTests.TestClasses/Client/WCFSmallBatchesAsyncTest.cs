using IntegrationTests.ServiceClasses;
using IntegrationTests.ServiceClasses.Domain;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTests.TestClasses.Client
{
    public class WCFSmallBatchesAsyncTest : Microsoft.Build.Utilities.Task
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
                ((NetTcpBinding)binding).CloseTimeout = new TimeSpan(0, 0, 10);
                ((NetTcpBinding)binding).OpenTimeout = new TimeSpan(0, 0, 10);
                ((NetTcpBinding)binding).ReceiveTimeout = new TimeSpan(0, 0, 10);
                ((NetTcpBinding)binding).SendTimeout = new TimeSpan(0, 0, 10);
            }
            else
                throw new ArgumentException("Invalid value for EndpointType. Expected: http, nettcp");
            EndpointAddress address = new EndpointAddress(new Uri(WebServiceUri));

            IntegrationTestsService.IntegrationTestsServiceClient client = new IntegrationTestsService.IntegrationTestsServiceClient(binding, address);

            Log.LogMessage("Doing " + TotalBatches.ToString() + " batch calls with " + BatchSize.ToString() + " itens each");

            Stopwatch watch = new Stopwatch();
            watch.Start();

            int count = 1;
            Queue<Task<ServiceTable[]>> tasks = new Queue<Task<ServiceTable[]>>();            
            for (int i = 0; i < TotalBatches; i++)
            {
                tasks.Enqueue(client.GetServiceTablesAsynchronousAsync(count, count + (BatchSize - 1)));                
                count += BatchSize;
                
            }

            Queue<Task> queue2 = new Queue<Task>();

            while (tasks.Count > 0)
            {                
                Task<ServiceTable[]> task = tasks.Dequeue();
                                
                task.Wait();
                ServiceTable[] stArray = task.Result;

                foreach (ServiceTable t in stArray)
                {

                    ServiceTable t2 = new ServiceTable();
                    t2.ServiceTableID = t.ServiceTableID;
                    t2.DescServiceTable = t.DescServiceTable;
                    t2.Value = t.Value;
                    t2.CreationDate = t.CreationDate;
                    t2.StringField1 = t.StringField1;
                    t2.StringField2 = t.StringField2;

                    queue2.Enqueue(DAO.ProcessServiceTableAsync(ConnString, t2));
                }                
            }

            while (queue2.Count > 0)
                queue2.Dequeue().Wait();

            watch.Stop();
            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

            return true;
        }

    }
}
