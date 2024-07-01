using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.IO.Compression;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices.ComTypes;
using System.Collections;
using System.Reflection;
using System.Diagnostics.Tracing;

namespace OtmetkaServer
{
    internal class OtmetkaServerApplication
    {
        private static IPEndPoint GetLocalEndPoint()
        {
            using (Socket Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                Socket.Connect("8.8.8.8", 65530);
                return Socket.LocalEndPoint as IPEndPoint;
            }
        }

        static void Main(string[] args)
        {
            IPEndPoint LocalEndPoint = GetLocalEndPoint();
            TcpListener Listener = new TcpListener(LocalEndPoint.Address, 4620);
            Listener.Start();
            while (true)
            {
                var ClientSocket = Listener.AcceptSocket();
                OtmetkaClientTask.Run(ClientSocket);
            }
        }
    }
}

