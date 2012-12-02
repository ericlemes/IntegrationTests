using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Xml;
using System.Data;
using System.IO;
using System.Globalization;
using IntegrationTests.ServiceClasses.Domain;
using System.Threading;

namespace IntegrationTests.ServiceClasses
{
    public class FileUtil
    {
        public void ExtrairParaArquivo(string ConnString, int IDInicial, int IDFinal, string file)
        {
            SqlConnection conn = new SqlConnection(ConnString);
            conn.Open();

            SqlCommand cmd = new SqlCommand("select ServiceTableID, DescServiceTable, Value, CreationDate, StringField1, StringField2 " +
                "from ServiceTable where ServiceTableID between @IDInicial and @IDFinal", conn);

            XmlTextWriter xw = new XmlTextWriter(file, Encoding.UTF8);

            using (xw)
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("table");

                using (conn)
                {
                    SqlParameter p1 = cmd.Parameters.Add("@IDInicial", SqlDbType.Int);
                    p1.Value = IDInicial;
                    SqlParameter p2 = cmd.Parameters.Add("@IDFinal", SqlDbType.Int);
                    p2.Value = IDFinal;

                    SqlDataReader rd = cmd.ExecuteReader();
                    using (rd)
                    {
                        while (rd.Read())
                        {
                            int ServiceTableID = rd.GetInt32(0);
                            string DescServiceTable = rd.GetString(1);
                            string Value = ((float)rd.GetDouble(2)).ToString("0.00");
                            string CreationDate = rd.GetDateTime(3).ToString("dd/MM/yyyy hh:mm:ss");
                            string StringField1 = rd.GetString(4);
                            string StringField2 = rd.GetString(5);

                            xw.WriteStartElement("record");
                            xw.WriteElementString("ServiceTableID", ServiceTableID.ToString());
                            xw.WriteElementString("DescServiceTable", DescServiceTable);
                            xw.WriteElementString("Value", Value);
                            xw.WriteElementString("CreationDate", CreationDate);
                            xw.WriteElementString("StringField1", StringField1);
                            xw.WriteElementString("StringField2", StringField2);
                            xw.WriteEndElement();
                        }
                    }
                }
                xw.WriteEndElement();
            }
        }

        public void ImportarArquivo(string connString, string file)
        {
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);            
            //StreamUtil u = new StreamUtil();
            StreamUtil.ImportarStream(connString, fs);            
        }

        public int ProcessRequest(string requestFile)
        {
            FileStream fs = new FileStream(requestFile, FileMode.Open, FileAccess.Read, FileShare.None);
            byte[] buffer = new byte[sizeof(int)];
            fs.Read(buffer, 0, sizeof(int));
            return BitConverter.ToInt32(buffer, 0);
        }

        public void ProcessClientRequest(string ConnString, string responseFile, int size)
        {            
            ExtrairParaArquivo(ConnString, 1, size, responseFile);
        }

        public void ProcessClientBigRequest(string ConnString, string requestFile, string responseFile)
        {
            FileStream requestFileStream = new FileStream(requestFile, FileMode.Open);       
            FileStream responseFileStream = new FileStream(responseFile, FileMode.Create);            
            StreamUtil.ProcessClientBigRequest(ConnString, requestFileStream, responseFileStream, true, null);
        }

        public static void WaitForUnlock(string file)
        {
            FileStream fs = null;
            bool tryAgain = true;
            while (tryAgain)
            {
                try
                {
                    fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    using (fs)
                        fs.Close();
                    tryAgain = false;
                }
                catch (IOException)
                {
                    tryAgain = true;
                    Thread.Sleep(200);
                }

            }
        }


    }
}
