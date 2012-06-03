using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using IntegrationTests.ServiceClasses.Domain;

namespace IntegrationTests.WCFServiceApp
{    
    [ServiceContract]
    public interface IIntegrationTestsService
    {
        [OperationContract]
        ServiceTable GetServiceTable(int ServiceTableID);

        [OperationContract]
        List<ServiceTable> GetServiceTables(int IDInicial, int IDFinal);      
    }
    
}
