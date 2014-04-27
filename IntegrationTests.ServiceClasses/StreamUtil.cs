using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using IntegrationTests.ServiceClasses.Domain;
using System.Globalization;
using Microsoft.Build.Utilities;

namespace IntegrationTests.ServiceClasses
{
	public delegate void FlushDelegate();

	public static class StreamUtil
	{
		public static void ProcessClientBigRequest(string ConnString, Stream requestStream, Stream responseStream, bool dispose, FlushDelegate flushDelegate)
		{
			StreamUtil.ProcessClientBigRequest(ConnString, requestStream, responseStream, dispose, flushDelegate, null);
		}

		public static void ProcessClientBigRequest(string ConnString, Stream requestStream, Stream responseStream, bool dispose, FlushDelegate flushDelegate, TaskLoggingHelper log)
		{
			XmlTextReader xr = new XmlTextReader(requestStream);

			XmlTextWriter xw = new XmlTextWriter(responseStream, Encoding.UTF8);

			xw.WriteStartDocument();
			xw.WriteStartElement("table");

			bool first = true;

			//using (xr)
			//{
				xr.Read();
				xr.Read();
				xr.ReadStartElement("request");
				while (xr.Name == "id")
				{
					int serviceTableID = Convert.ToInt32(xr.ReadElementString("id"));
					ServiceTable st = DAO.GetServiceTable(ConnString, serviceTableID);
					if (first)
					{
						if (log != null)
							log.LogMessage("Processing ID " + serviceTableID.ToString() + " response " + st.ServiceTableID.ToString());
						first = false;
					}

					xw.WriteStartElement("record");
					xw.WriteElementString("ServiceTableID", st.ServiceTableID.ToString());
					xw.WriteElementString("DescServiceTable", st.DescServiceTable);
					xw.WriteElementString("Value", st.Value.ToString("0.00"));
					xw.WriteElementString("CreationDate", st.CreationDate.ToString("dd/MM/yyyy hh:mm:ss"));
					xw.WriteElementString("StringField1", st.StringField1);
					xw.WriteElementString("StringField2", st.StringField2);
					xw.WriteEndElement();
					if (flushDelegate != null)
						flushDelegate();
				}
				xr.ReadEndElement();

			//}
                xr.Dispose();
			xw.WriteEndElement();
			xw.Flush();

			if (dispose)
				xw.Close();
		}

		public static void GenerateBigRequest(Stream stream, bool dispose, int size)
		{
			XmlTextWriter xw = new XmlTextWriter(stream, Encoding.UTF8);

			xw.WriteStartDocument();
			xw.WriteStartElement("request");
			for (int i = 0; i < size; i++)
			{
				xw.WriteElementString("id", (i + 1).ToString());
			}
			xw.WriteEndElement();
			xw.Flush();

			if (dispose)
				xw.Close();
		}

		public static void GenerateBigRequest(Stream stream, bool dispose, int start, int end)
		{
			XmlTextWriter xw = new XmlTextWriter(stream, Encoding.UTF8);

			xw.WriteStartDocument();
			xw.WriteStartElement("request");
			for (int i = start; i <= end; i++)
			{
				xw.WriteElementString("id", (i).ToString());
			}
			xw.WriteEndElement();
			xw.Flush();

			if (dispose)
				xw.Close();
		}

		public static XmlTextWriter GenerateBigRequestStart(Stream stream)
		{
			XmlTextWriter xw = new XmlTextWriter(stream, Encoding.UTF8);
			xw.WriteStartDocument();
			xw.WriteStartElement("request");
			return xw;
		}

		public static void GenerateBigRequestItem(XmlTextWriter xw, int id)
		{
			xw.WriteElementString("id", id.ToString());
		}

		public static void GenerateBigRequestEnd(XmlTextWriter xw)
		{
			xw.WriteEndElement();
		}

		public static void ImportarStream(string connString, Stream stream)
		{
			StreamUtil.ImportarStream(connString, stream, null);
		}

		public static void ImportarStream(string connString, Stream stream, TaskLoggingHelper log)
		{
			XmlTextReader rd = new XmlTextReader(stream);

			CultureInfo c = new CultureInfo("pt-BR");
			c.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy hh:mm:ss";

			bool first = true;

			using (rd)
			{
				rd.Read();
				rd.Read();
				rd.ReadStartElement("table");
				while (rd.Name == "record")
				{
					rd.ReadStartElement("record");
					ServiceTable st = new ServiceTable();
					st.ServiceTableID = Convert.ToInt32(rd.ReadElementContentAsString("ServiceTableID", ""));
					if (first)
					{
						if (log != null)
							log.LogMessage("Importing ID " + st.ServiceTableID.ToString());
						first = false;
					}
					st.DescServiceTable = rd.ReadElementContentAsString("DescServiceTable", "");
					st.Value = Convert.ToSingle(rd.ReadElementContentAsString("Value", ""));
					st.CreationDate = Convert.ToDateTime(rd.ReadElementContentAsString("CreationDate", ""), c.DateTimeFormat);
					st.StringField1 = rd.ReadElementContentAsString("StringField1", "");
					st.StringField2 = rd.ReadElementContentAsString("StringField2", "");
					rd.ReadEndElement();

					/*if (Log != null)
						Log.LogMessage("ServiceTableID: " + st.ServiceTableID.ToString());*/

					DAO.ProcessServiceTable(connString, st);
				}
				rd.ReadEndElement();
			}
		}

	}
}
