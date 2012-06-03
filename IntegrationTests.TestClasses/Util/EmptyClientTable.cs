using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using IntegrationTests.ServiceClasses;
using Microsoft.Build.Framework;

namespace IntegrationTests.TestClasses.Util
{
    public class EmptyClientTable : Task
    {
        [Required]
        public string ConnString
        {
            get;
            set;
        }

        public override bool Execute()
        {
            Log.LogMessage("Emptying client table");
            DAO.ClearClientTable(ConnString);
            Log.LogMessage("Done");

            return true;
        }
    }
}
