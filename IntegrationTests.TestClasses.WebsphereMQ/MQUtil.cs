using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IntegrationTests.TestClasses.WebsphereMQ
{
    public static class MQUtil
    {
        public static void StreamToMQMessage(Stream stream, IBM.WMQ.MQMessage msg)
        {
            stream.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[100 * 1024];

            int read = stream.Read(buffer, 0, buffer.Length);
            while (read > 0)
            {
                msg.Write(buffer, 0, read);
                read = stream.Read(buffer, 0, buffer.Length);
            }
        }

        public static void MQMessageToStream(IBM.WMQ.MQMessage msg, Stream stream)
        {
            byte[] buffer = new byte[100 * 1024];
            int bytesToRead = msg.DataLength > buffer.Length ? buffer.Length : msg.DataLength;
            buffer = msg.ReadBytes(bytesToRead);            
            stream.Write(buffer, 0, bytesToRead);

            while (msg.DataLength > 0)
            {
                bytesToRead = msg.DataLength > buffer.Length ? buffer.Length : msg.DataLength;
                buffer = msg.ReadBytes(bytesToRead);                
                stream.Write(buffer, 0, bytesToRead);
            }
            stream.Seek(0, SeekOrigin.Begin);
        }
    }
}
