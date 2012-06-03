using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using IntegrationTests.ServiceClasses.Domain;
using Microsoft.Build.Framework;
using System.ServiceModel;
using System.Threading;
using IntegrationTests.ServiceClasses;
using System.ServiceModel.Channels;
using System.Diagnostics;

namespace IntegrationTests.TestClasses.Client
{
    public class WCFSmallRequestsTest : Task
    {
        [Required]
        public int TotalRequests
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

        public override bool Execute()
        {

            Binding binding;

            if (this.EndpointType == "http")
            {
                binding = new BasicHttpBinding();
                ((BasicHttpBinding)binding).MessageEncoding = WSMessageEncoding.Text;
                ((BasicHttpBinding)binding).TextEncoding = Encoding.UTF8;
                ((BasicHttpBinding)binding).TransferMode = TransferMode.Buffered;
                ((BasicHttpBinding)binding).Security.Mode = BasicHttpSecurityMode.None;
            }
            else if (this.EndpointType == "nettcp")
            {
                binding = new NetTcpBinding();
                ((NetTcpBinding)binding).MaxReceivedMessageSize = 1024 * 1024;
                ((NetTcpBinding)binding).Security.Mode = SecurityMode.None;
                ((NetTcpBinding)binding).CloseTimeout = new TimeSpan(0, 1, 0);
                ((NetTcpBinding)binding).OpenTimeout = new TimeSpan(0, 1, 10);
                ((NetTcpBinding)binding).ReceiveTimeout = new TimeSpan(0, 1, 10);
                ((NetTcpBinding)binding).SendTimeout = new TimeSpan(0, 1, 10);
            }
            else
                throw new ArgumentException("Invalid value for EndpointType. Expected: http, nettcp");
            EndpointAddress address = new EndpointAddress(new Uri(WebServiceUri));

            IntegrationTestsService.IntegrationTestsServiceClient client = new IntegrationTestsService.IntegrationTestsServiceClient(binding, address);

            Log.LogMessage("Doing " + TotalRequests.ToString() + " calls");

            Stopwatch watch = new Stopwatch();
            watch.Start();
            
            for (int i = 1; i <= TotalRequests; i++)
            {
                ServiceTable t = null;
                bool tryAgain = true;
                while (tryAgain)
                {
                    try
                    {
                        t = client.GetServiceTable(i);
                        tryAgain = false;
                    }
                    catch (EndpointNotFoundException)
                    {
                        Thread.Sleep(100);
                        t = client.GetServiceTable(i);
                        tryAgain = true;
                    }
                }

                ServiceTable t2 = new ServiceTable();
                t2.ServiceTableID = t.ServiceTableID;
                t2.DescServiceTable = t.DescServiceTable;
                t2.Value = t.Value;
                t2.CreationDate = t.CreationDate;
                t2.StringField1 = t.StringField1;
                t2.StringField2 = t.StringField2;

                DAO.ProcessServiceTable(ConnString, t2);            
            }

            watch.Stop();
            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

            
            
            return true;
        }
    }
}
