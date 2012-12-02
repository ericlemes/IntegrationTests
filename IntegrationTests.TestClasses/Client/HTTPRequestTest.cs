using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using IntegrationTests.ServiceClasses;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Threading;
using System.Diagnostics;

namespace IntegrationTests.TestClasses.Client
{
    public class HTTPRequestTest : Task
    {
        [Required]
        public string ConnString
        {
            get;
            set;
        }

        [Required]
        public int BigRequestSize
        {
            get;
            set;
        }

        [Required]
        public string Uri
        {
            get;
            set;
        }

        [Required]
        public bool Flush
        {
            get;
            set;
        }        

        private bool finished = false;
        private bool asyncError = false;

        public override bool Execute()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(Uri));            
            request.Method = "POST";
            request.ContentType = "text/xml";

            request.BeginGetRequestStream(GetRequestStreamCallback, request);

            while (!finished)
                Thread.Sleep(250);

            if (asyncError)
            {
                return false;
            }
            else
            {
                watch.Stop();
                Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");
                return true;
            }
            
        }

        private void GetRequestStreamCallback(IAsyncResult result)
        {
            HttpWebRequest request = (HttpWebRequest)result.AsyncState;
            if (Flush)
                request.Headers.Add("Flush:true");

            Stream requestStream = request.EndGetRequestStream(result);
            Log.LogMessage("Writing request with " + BigRequestSize.ToString() + " itens. Flush " + Flush.ToString());            
                        
            StreamUtil.GenerateBigRequest(requestStream, true, BigRequestSize);
            Log.LogMessage("Finished writing request.");
            request.BeginGetResponse(GetResponseCallback, request);
        }

        private void GetResponseCallback(IAsyncResult result)
        {
            HttpWebRequest request = (HttpWebRequest)result.AsyncState;
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(result);
            }
            catch (Exception ex)
            {
                Log.LogMessage(ex.Message);
                finished = true;
                asyncError = true;
                return;
            }

            Stream responseStream = response.GetResponseStream();
                
            Log.LogMessage("Reading response");
            
            StreamUtil.ImportarStream(ConnString, responseStream);
            finished = true;            
        }
    }
}
