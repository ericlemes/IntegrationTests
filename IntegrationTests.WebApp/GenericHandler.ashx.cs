using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IntegrationTests.ServiceClasses;

namespace IntegrationTests.WebApp
{
    /// <summary>
    /// Summary description for GenericHandler
    /// </summary>
    public class GenericHandler : IHttpHandler
    {
        private static string connString;

        private HttpContext context;

        private bool flush;

        private void GetConnString()
        {
            connString = System.Configuration.ConfigurationManager.AppSettings["ConnString"];
        }        

        public void ProcessRequest(HttpContext context)
        {
            if (String.IsNullOrEmpty(connString))
                GetConnString();

            if (context.Request.Headers["Flush"] == "true")
                flush = true;

            context.Response.ContentType = "text/xml";
            this.context = context;
            
            StreamUtil u = new StreamUtil();            
            u.ProcessClientBigRequest(connString, context.Request.InputStream, context.Response.OutputStream, true, Flush);
            context.Response.Flush();
            context.Response.End();
        }

        private void Flush()
        {
            if (flush)
                context.Response.Flush();
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}