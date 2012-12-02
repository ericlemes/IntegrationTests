using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using IntegrationTests.ServiceClasses;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Diagnostics;

namespace IntegrationTests.TestClasses.Client
{
    public class TcpClientMultiThreadTest : Task
    {
        [Required]
        public int TotalBatches
        {
            get;
            set;
        }

        [Required]
        public int BatchSize
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
        public string HostName
        {
            get;
            set;
        }

        [Required]
        public int Port
        {
            get;
            set;
        }

        private Queue<MemoryStream> memoryStreamQueue = new Queue<MemoryStream>();               

        public override bool Execute()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Thread t = new Thread(ProcessMemoryStreamQueue);
            t.Start();

            Log.LogMessage("Starting TCP transfer (multi thread) with " + TotalBatches.ToString() + " batchs with " + BatchSize.ToString() + " items each");            

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(HostName, Port);
            NetworkStream stream = tcpClient.GetStream();

            int count = 1;
            for (int i = 0; i < TotalBatches; i++)
            {
                Log.LogMessage("Processing " + i.ToString());

                MemoryStream ms = new MemoryStream();
                
                StreamUtil.GenerateBigRequest(ms, false, count, count + (BatchSize - 1));
                ms.Seek(0, SeekOrigin.Begin);

                WriteHeader(stream, ms);
                
                //Sends the request
                ms.CopyTo(stream);

                long totalBytes = GetBytesToRead(stream);
                
                //Sleeps if have 10 responses to process. 
                while (memoryStreamQueue.Count >= 10)
                    Thread.Sleep(100);

                MemoryStream responseStream = ReadResponse(totalBytes, stream);
                //Don't process response. Just queue it. The other thread will process it.
                lock (memoryStreamQueue)
                    memoryStreamQueue.Enqueue(responseStream);

                count += BatchSize;                
            }
            //Mark end of transfer.
            stream.Write(new byte[8], 0, 8);
                       
            tcpClient.Close();
            watch.Stop();
            t.Abort();

            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");

            return true;
        }

        private void ProcessMemoryStreamQueue()
        {
            while (true)
            {
                if (memoryStreamQueue.Count > 0)
                {
                    MemoryStream ms = null;
                    lock (memoryStreamQueue)
                    {
                        ms = memoryStreamQueue.Dequeue();                        
                    }
                    StreamUtil.ImportarStream(ConnString, ms);                    
                }
                Thread.Sleep(200);
            }
        }

        private void WriteHeader(Stream stream, Stream streamToSend)
        {
            byte[] header = BitConverter.GetBytes(streamToSend.Length);
            stream.Write(header, 0, header.Length);
        }

        private long GetBytesToRead(Stream stream)
        {
            byte[] header = new byte[sizeof(long)];
            int read = stream.Read(header, 0, header.Length);
            return BitConverter.ToInt64(header, 0);
        }

        private MemoryStream ReadResponse(long responseSize, Stream stream)
        {
            MemoryStream inputStream = new MemoryStream();
            byte[] buffer = new byte[100 * 1024];
            int bytesToRead;
            int read;
            long totalBytes = responseSize;

            do
            {
                bytesToRead =  totalBytes > buffer.Length ? buffer.Length : (int)totalBytes;
                read = stream.Read(buffer, 0, bytesToRead);

                inputStream.Write(buffer, 0, read);
                totalBytes -= read;
            }
            while (totalBytes > 0);
                        
            inputStream.Seek(0, SeekOrigin.Begin);
            return inputStream;
        }

    }
}
