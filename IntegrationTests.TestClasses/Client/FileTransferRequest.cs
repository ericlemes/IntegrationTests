using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Threading;
using IntegrationTests.ServiceClasses;
using System.Diagnostics;

namespace IntegrationTests.TestClasses.Client
{
    public class FileTransferRequest : Task
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
        public int Records
        {
            get;
            set;
        }

        private bool finished = false;
        private FileUtil util = new FileUtil();

        public override bool Execute()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            FileSystemWatcher watcher = new FileSystemWatcher(InputDir);
            watcher.Created += new FileSystemEventHandler(watcher_Created);
            watcher.EnableRaisingEvents = true;

            FileStream fs = new FileStream(OutputDir + "\\request.xml", FileMode.Create);
            Log.LogMessage("Generating " + OutputDir + " with " + Records.ToString());
            using (fs)
                fs.Write(BitConverter.GetBytes(Records), 0, 3);

            Log.LogMessage("Waiting for " + InputDir + "\\response.xml");

            while (!finished)
                Thread.Sleep(250);

            watch.Stop();

            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

            return true;
        }

        private void watcher_Created(object sender, FileSystemEventArgs e)
        {
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
