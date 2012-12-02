using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using IntegrationTests.ServiceClasses;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Threading;

namespace IntegrationTests.TestClasses.Server
{
    public class TcpTestServer : Task
    {   
        [Required]
        public string ConnString
        {
            get;
            set;
        }

        private int port = 8081;        
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public override bool Execute()
        {
            Log.LogMessage("Listening to TCP Connections on port " + Port.ToString());

            IPAddress ipAddress = IPAddress.Any;
            TcpListener tcpListener = new TcpListener(ipAddress, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(BeginAcceptSocketCallback, tcpListener);

            while (true)
                Thread.Sleep(250);                                    
        }

        private void BeginAcceptSocketCallback(IAsyncResult result)
        {            
            TcpListener tcpListener = (TcpListener)result.AsyncState;
            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(result);
            
            tcpListener.BeginAcceptTcpClient(BeginAcceptSocketCallback, tcpListener);

            Stream clientStream = tcpClient.GetStream();

            int headerBytes;
            int count = 1;

            do {
                byte[] header = new byte[8];
                headerBytes = clientStream.Read(header, 0, 8);
                long size = BitConverter.ToInt64(header, 0);

                Log.LogMessage("Processing " + count.ToString());
                count++;

                MemoryStream ms = ReadBatch(clientStream, size);

                if (ms.Length > 0)               
                    WriteOutput(ms, clientStream);                                    
                else               
                    headerBytes = 0;                
            }
            while (headerBytes > 0);
            Log.LogMessage("Finished request.");
        }

        private MemoryStream ReadBatch(Stream clientStream, long size)
        {            
            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[100 * 1024];

            long tmpSize = size;

            int read;
            do{
                int bytesToRead = size > buffer.Length ? buffer.Length : (int)size;
                read = clientStream.Read(buffer, 0, bytesToRead);

                ms.Write(buffer, 0, read);

                tmpSize -= read;                
            }
            while (tmpSize > 0);                      

            return ms;
        }

        private void WriteOutput(MemoryStream ms, Stream clientStream)
        {
            byte[] header = new byte[sizeof(Int64)];

            ms.Seek(0, SeekOrigin.Begin);
            MemoryStream outputStream = new MemoryStream();
            StreamUtil.ProcessClientBigRequest(ConnString, ms, outputStream, false, null);
            outputStream.Seek(0, SeekOrigin.Begin);
            header = BitConverter.GetBytes(outputStream.Length);
            clientStream.Write(header, 0, header.Length);
            outputStream.CopyTo(clientStream);            
        }
    }
}
