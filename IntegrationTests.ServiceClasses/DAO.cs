using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IntegrationTests.ServiceClasses.Domain;
using System.Data.SqlClient;
using System.Data;

namespace IntegrationTests.ServiceClasses
{
    public class DAO
    {
        public static ServiceTable GetServiceTable(string ConnString, int ServiceTableID)
        {
            ServiceTable result = new ServiceTable();

            SqlConnection conn = new SqlConnection(ConnString);
            conn.Open();

            SqlCommand cmd = new SqlCommand("select ServiceTableID, DescServiceTable, Value, CreationDate, StringField1, StringField2 " +
                "from ServiceTable where ServiceTableID = @ServiceTableID", conn);

            using (conn)
            {
                SqlParameter p1 = cmd.Parameters.Add("@ServiceTableID", SqlDbType.Int);
                p1.Value = ServiceTableID;

                SqlDataReader rd = cmd.ExecuteReader();
                rd.Read();
                using (rd)
                {
                    result.ServiceTableID = rd.GetInt32(0);
                    result.DescServiceTable = rd.GetString(1);
                    result.Value = (float)rd.GetDouble(2);
                    result.CreationDate = rd.GetDateTime(3);
                    result.StringField1 = rd.GetString(4);
                    result.StringField2 = rd.GetString(5);
                }
            }

            return result;
        }

        public static void ProcessServiceTable(string ConnString, ServiceTable table)
        {
            SqlConnection conn = new SqlConnection(ConnString);
            conn.Open();

            SqlCommand cmd = new SqlCommand("insert into ClientTable (ClientTableID, DescClientTable, Value, CreationDate, StringField1, StringField2)" +
                "values (@ClientTableID, @DescClientTable, @Value, @CreationDate, @StringField1, @StringField2)", conn);

            using (conn)
            {

                SqlParameter p1 = cmd.Parameters.Add("@ClientTableID", SqlDbType.Int);
                SqlParameter p2 = cmd.Parameters.Add("@DescClientTable", SqlDbType.VarChar, 200);
                SqlParameter p3 = cmd.Parameters.Add("@Value", SqlDbType.Float);
                SqlParameter p4 = cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime);
                SqlParameter p5 = cmd.Parameters.Add("@StringField1", SqlDbType.VarChar, 200);
                SqlParameter p6 = cmd.Parameters.Add("@StringField2", SqlDbType.VarChar, 200);

                p1.Value = table.ServiceTableID;
                p2.Value = table.DescServiceTable;
                p3.Value = table.Value;
                p4.Value = table.CreationDate;
                p5.Value = table.StringField1;
                p6.Value = table.StringField2;

                cmd.ExecuteNonQuery();
            }
        }

        public static void ClearClientTable(string ConnString)
        {
            ServiceTable result = new ServiceTable();

            SqlConnection conn = new SqlConnection(ConnString);
            conn.Open();

            SqlCommand cmd = new SqlCommand("delete from ClientTable", conn);

            using (conn)
                cmd.ExecuteNonQuery();
        }
    }
}
