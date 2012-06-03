using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace IntegrationTests.TestClasses.Util
{
    public class DataGeneration : Task
    {
        [Required]
        public string ConnString
        {
            get;
            set;
        }

        public override bool Execute()
        {
            SqlConnection conn = new SqlConnection(ConnString);
            conn.Open();

            SqlCommand cmd = new SqlCommand("insert into ServiceTable (ServiceTableID, DescServiceTable, Value, CreationDate, StringField1, StringField2)" +
                "values (@ServiceTableID, @DescServiceTable, @Value, @CreationDate, @StringField1, @StringField2)", conn);

            using (conn)
            {

                SqlParameter p1 = cmd.Parameters.Add("@ServiceTableID", SqlDbType.Int);
                SqlParameter p2 = cmd.Parameters.Add("@DescServiceTable", SqlDbType.VarChar, 200);
                SqlParameter p3 = cmd.Parameters.Add("@Value", SqlDbType.Float);
                SqlParameter p4 = cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime);
                SqlParameter p5 = cmd.Parameters.Add("@StringField1", SqlDbType.VarChar, 200);
                SqlParameter p6 = cmd.Parameters.Add("@StringField2", SqlDbType.VarChar, 200);

                Random r = new Random();

                int count = 0;                
                for (int i = 1; i <= 2000000; i++)
                {
                    p1.Value = i;
                    p2.Value = "Server Value " + i.ToString();
                    p3.Value = r.Next();
                    p4.Value = DateTime.Now;
                    p5.Value = "Useless Field 1: " + r.Next().ToString();
                    p6.Value = "Useless Field 1: " + r.Next().ToString();

                    cmd.ExecuteNonQuery();

                    count++;
                    if (count >= 1000)
                    {
                        count = 0;
                        Log.LogMessage("Generated " + i.ToString() + "/2000000 records");
                    }
                }

                return true;
            }
        }
    }
}
