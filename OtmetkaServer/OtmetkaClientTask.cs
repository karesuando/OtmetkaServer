using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Net;

namespace OtmetkaServer
{
    using JsonSerializer = DataContractJsonSerializer;

    internal class OtmetkaClientTask
    {
        const int TIMEOUT = 150;
        const int BUFFERSIZE = 256000;
        const int SERVICE_SHUTDOWN = -1;
        const string Path = @"C:\Users\tombo\OneDrive\Desktop\Temp\"; 

        private static readonly CancellationTokenSource OtmetkaCTS;

        private static readonly Dictionary<EndPoint, Task> TaskDictionary;

        public static void CancelAndWaitForAll()
        {
            OtmetkaCTS.Cancel();
            WaitForAll();
        }

        private static void WaitForAll()
        {
            if (TaskDictionary != null && TaskDictionary.Values.Count > 0)
            {
                Task[] Tasks = TaskDictionary.Values.ToArray();
                Task.WaitAll(Tasks);
            }
        }

        static OtmetkaClientTask() 
        {
            OtmetkaCTS = new CancellationTokenSource();
            TaskDictionary = new Dictionary<EndPoint, Task>();
        }

        public static void Run(Socket Socket)
        {
            Action<object> TaskMethod = ClientCommuncationTask;
            Task Thread = Task.Factory.StartNew(TaskMethod, Socket);
            TaskDictionary.Add(Socket.RemoteEndPoint, Thread);
        }

        [DataContract]
        private struct SubscriberData
        {
            [DataMember]
            public string RequestType;
            [DataMember]
            public string Data;
        }

        private static void ClientCommuncationTask(object Parameter)
        {
            byte[] Buffer = new byte[BUFFERSIZE];
            byte[] SizeBuffer = new byte[sizeof(int)];
            Socket Client = (Socket)Parameter;
            CancellationToken Token = OtmetkaCTS.Token;

            try
            {
                while (!Token.IsCancellationRequested)
                {
                    if (Client.Available == 0)
                        Thread.Sleep(500);
                    else
                    {
                        int Timer;
                        int Offset, BytesRead, BytesToRead;

                        Timer = 0;
                        Offset = 0;
                        BytesRead = Client.Receive(SizeBuffer, 0, SizeBuffer.Length, SocketFlags.None);
                        BytesToRead = BitConverter.ToInt32(SizeBuffer, 0);
                        if (BytesToRead == SERVICE_SHUTDOWN)
                            return;
                        if (BytesToRead > Buffer.Length)
                            Array.Resize(ref Buffer, BytesToRead);
                        Client.ReceiveBufferSize = BytesToRead;
                        while (BytesToRead > 0 && Timer < TIMEOUT)
                        {
                            while (Client.Available > 0)
                            {
                                BytesRead = Client.Receive(Buffer, Offset, BytesToRead, SocketFlags.None);
                                BytesToRead -= BytesRead;
                                Offset += BytesRead;
                                Timer = 0;
                            }
                            Thread.Sleep(100);
                            Timer++;
                        }
                        if (BytesToRead == 0)
                        {
                            ProcessData(Buffer, Client.ReceiveBufferSize);
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                LogServerEvent("ClientCommuncationTask() exception: " + ex.Message);
            }
        }

        private static void ProcessData(byte[] Buffer, int BufferSize)
        {
            int FileNo = 1;
            int Index = 0;
            byte[] SizeBytes = new byte[sizeof(int)];

            Buffer = GZipDecompress(Buffer, BufferSize);
            while (Index < Buffer.Length)
            {
                System.Buffer.BlockCopy(Buffer, Index, SizeBytes, 0, sizeof(int));
                int Size = BitConverter.ToInt32(SizeBytes, 0);
                byte[] Data = new byte[Size];
                Index += sizeof(int);
                System.Buffer.BlockCopy(Buffer, Index, Data, 0, Size);
                Index += Size;
                var MemStream = new MemoryStream(Data);
                var Serializer = new JsonSerializer(typeof(SubscriberData));
                var SubscriberInfo = (SubscriberData)Serializer.ReadObject(MemStream);
                string Extension = SubscriberInfo.RequestType;
                string FileName = "Temp" + FileNo.ToString() + Extension;
                File.WriteAllText(Path + FileName, SubscriberInfo.Data);
                FileNo++;
            }
        }
        private static byte[] GZipDecompress(byte[] Buffer, int BufferSize)
        {
            using (var InputStream = new MemoryStream(Buffer, 0, BufferSize))
            {
                using (var GZipStream = new GZipStream(InputStream, CompressionMode.Decompress))
                {
                    var OutputStream = new MemoryStream();
                    GZipStream.CopyTo(OutputStream);
                    return OutputStream.ToArray();
                }
            }
        }
        private static void LogServerEvent(string message)
        {
            try
            {
                string DirPath = Path + "OtmetkaServerLog.txt";
                using (StreamWriter Writer = new StreamWriter(DirPath, true))
                {
                    string dt = DateTime.Now.ToString("MM-dd-yyyy HH:mm::ss");
                    Writer.WriteLine(dt + "> " + message);
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
