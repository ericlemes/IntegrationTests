using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.ServiceModel.Channels;
using System.ServiceModel;
using IntegrationTests.ServiceClasses;
using IntegrationTests.ServiceClasses.Domain;
using System.Diagnostics;

namespace IntegrationTests.TestClasses.Client
{
    public class WCFSmallBatchesTest : Task
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
                binding = new BasicHttpBinding();
                ((BasicHttpBinding)binding).MaxReceivedMessageSize = 2048 * 1024;
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
            for (int i = 0; i < TotalBatches; i++)
            {
                ServiceTable[] stArray = client.GetServiceTables(count, count + (BatchSize - 1));

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
                count += BatchSize;
            }

            watch.Stop();
            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

            return true;
        }
    }
}
