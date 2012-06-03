using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntegrationTests.ServiceClasses.Domain
{
    public class ServiceTable
    {   
        public int ServiceTableID
        {
            get;
            set;
        }

        public string DescServiceTable
        {
            get;
            set;
        }

        public float Value
        {
            get;
            set;
        }

        public DateTime CreationDate
        {
            get;
            set;
        }

        public string StringField1
        {
            get;
            set;
        }

        public string StringField2
        {
            get;
            set;
        }
    }    
}
