using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IntegrationTests.ServiceClasses;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Threading;

namespace IntegrationTests.TestClasses.Server
{
    public class FileTransferTestServer : Task
    {
        [Required]
        public string ConnString
        {
            get;
            set;
        }

        [Required]
        public string InputDir
        {
            get;
            set;
        }

        [Required]
        public string OutputDir
        {
            get;
            set;
        }

        private FileUtil _util = new FileUtil();

        public override bool Execute()       
        {
            FileSystemWatcher watcher = new FileSystemWatcher(InputDir);
            watcher.Created += new FileSystemEventHandler(watcher_Created);
            watcher.EnableRaisingEvents = true;

            while (true)
                Thread.Sleep(250);            
        }                   

        private void watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (e.Name == "request.xml")
            {
                Log.LogMessage("Received request.xml. Writing response.xml");
                FileUtil.WaitForUnlock(InputDir + "\\request.xml");
                _util.ProcessClientRequest(ConnString, InputDir + "\\tempresponse.xml", _util.ProcessRequest(InputDir + "\\request.xml"));
                Log.LogMessage("Response written");
                if (File.Exists(OutputDir + "\\response.xml"))
                    File.Delete(OutputDir + "\\response.xml");
                File.Copy(InputDir + "\\tempresponse.xml", OutputDir + "\\response.xml");
                Log.LogMessage("Response copied to " + InputDir + "\\response.xml");
            }
            else if (e.Name == "bigrequest.xml")
            {
                Log.LogMessage("Received bigrequest.xml");
                Log.LogMessage("Waiting for file to be unlocked");                
                FileUtil.WaitForUnlock(InputDir + "\\bigrequest.xml");
                Log.LogMessage("bigrequest.xml unlocked.");
                _util.ProcessClientBigRequest(ConnString, InputDir + "\\bigrequest.xml", InputDir + "\\tempresponse.xml");
                Log.LogMessage("Starting copy");
                if (File.Exists(OutputDir + "\\response.xml"))
                    File.Delete(OutputDir + "\\response.xml");
                File.Copy(InputDir + "\\tempresponse.xml", OutputDir + "\\response.xml");
                Log.LogMessage("Finished the copy: source " + InputDir + "\\tempresponse.xml, destination: " + OutputDir + "\\response.xml");
            }
        }


    }
}
