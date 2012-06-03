using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using IntegrationTests.ServiceClasses.Domain;
using IntegrationTests.ServiceClasses;
using System.Configuration;

namespace IntegrationTests.WCFServiceApp
{
    public class IntegrationTestsService : IIntegrationTestsService
    {
        private static string connString;

        private void GetConnString()
        {

            connString = System.Configuration.ConfigurationManager.AppSettings["ConnString"];
        }
        
        public ServiceTable GetServiceTable(int ServiceTableID)
        {
            if (String.IsNullOrEmpty(connString))
                GetConnString();
            return DAO.GetServiceTable(connString, ServiceTableID);
        }

        public List<ServiceTable> GetServiceTables(int IDInicial, int IDFinal)
        {
            if (String.IsNullOrEmpty(connString))
                GetConnString();
            List<ServiceTable> l = new List<ServiceTable>();
            for (int i = IDInicial; i <= IDFinal; i++)
            {
                l.Add(DAO.GetServiceTable(connString, i));
            }
            return l;
        }
    }
}
