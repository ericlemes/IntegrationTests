using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IntegrationTests.ServiceClasses;
using Microsoft.Build.Utilities;
using System.Threading;
using Microsoft.Build.Framework;
using System.Diagnostics;

namespace IntegrationTests.TestClasses.Client
{
    public class FileTransferBigRequest : Task
    {
        [Required]
        public string OutputDir
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
        
        private bool finished = false;        

        public override bool Execute()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            FileSystemWatcher watcher = new FileSystemWatcher(InputDir);
            watcher.Created += new FileSystemEventHandler(watcher_Created);
            watcher.EnableRaisingEvents = true;

            Log.LogMessage("Writing big request with " + BigRequestSize.ToString() + " items");

            FileStream fs = new FileStream(OutputDir + "\\bigrequest.xml", FileMode.Create);
            
            StreamUtil.GenerateBigRequest(fs, true, BigRequestSize);

            while (!finished)
                Thread.Sleep(250);

            watch.Stop();

            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

            return true;
        }

        private void watcher_Created(object sender, FileSystemEventArgs e)
        {
            FileUtil util = new FileUtil();

            if (e.Name == "response.xml")
            {
                Log.LogMessage("Received " + InputDir + "\\response.xml");
                ((FileSystemWatcher)sender).EnableRaisingEvents = false;

                string file = InputDir + "\\response.xml";

                Log.LogMessage("Waiting for file to be unlocked");
                FileUtil.WaitForUnlock(file);
                Log.LogMessage("File unlocked");

                util.ImportarArquivo(ConnString, file);

                finished = true;
            }
        }
    }
}
