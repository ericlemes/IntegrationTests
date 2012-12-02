using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using IntegrationTests.ServiceClasses;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;

namespace IntegrationTests.TestClasses.Client
{
    public class TcpClientSingleThreadTest : Task
    {
        [Required]
        public string ConnString
        {
            get;
            set;
        }

        [Required]
        public int TotalBatches
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

        [Required]
        public int BatchSize
        {
            get;
            set;
        }        

        public override bool Execute()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(HostName, Port);

            Log.LogMessage("Starting TCP transfer (single thread) with " + TotalBatches.ToString() + " batchs with " + BatchSize.ToString() + " items each");            

            NetworkStream stream = tcpClient.GetStream();
            int count = 1;
            for (int i = 0; i < TotalBatches; i++)
            {
                Log.LogMessage("Processing " + count.ToString());

                MemoryStream ms = GenerateRequest(count);
                WriteHeader(ms, stream);
                long totalBytes = ReadHeader(stream);

                MemoryStream inputStream = ReadResponse(totalBytes, stream);
                StreamUtil.ImportarStream(ConnString, inputStream);

                count += BatchSize;                
            }            
            stream.Write(new byte[8], 0, 8);

            watch.Stop();
            Log.LogMessage("Total processing time: " + watch.Elapsed.TotalSeconds.ToString("0.00") + " seconds");
            tcpClient.Close();

            return true;
        }

        private MemoryStream GenerateRequest(int count)
        {
            MemoryStream ms = new MemoryStream();            
            StreamUtil.GenerateBigRequest(ms, false, count, count + (BatchSize - 1));
            ms.Seek(0, SeekOrigin.Begin);

            return ms;
        }

        private void WriteHeader(MemoryStream ms, Stream stream)
        {
            byte[] header = BitConverter.GetBytes(ms.Length);
            stream.Write(header, 0, header.Length);
            ms.CopyTo(stream);
        }

        private long ReadHeader(Stream stream)
        {
            byte[] header = new byte[sizeof(long)];
            int read = stream.Read(header, 0, header.Length);
            long totalBytes = BitConverter.ToInt64(header, 0);
            return totalBytes;
        }

        private MemoryStream ReadResponse(long responseSize, Stream stream)
        {
            MemoryStream inputStream = new MemoryStream();

            long totalBytes = responseSize;
            byte[] buffer = new byte[100 * 1024];
            int bytesToRead;
            int read;
            do
            {
                bytesToRead = totalBytes > buffer.Length ? buffer.Length : (int)totalBytes;
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
