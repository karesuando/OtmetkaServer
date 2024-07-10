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

        public static IPAddress GetLocalIPAddress()
        {
            string HostName = Dns.GetHostName();
            IPHostEntry Host = Dns.GetHostEntry(HostName);
            foreach (var IPAddress in Host.AddressList)
            {
                if (IPAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    return IPAddress;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        static void Main(string[] args)
        {
            IPAddress LocalAddress = GetLocalIPAddress();
            Console.WriteLine(LocalAddress.ToString());
            TcpListener Listener = new TcpListener(LocalAddress, 4620);
            Listener.Start();
            while (true)
            {
                var ClientSocket = Listener.AcceptSocket();
                OtmetkaClientTask.Run(ClientSocket);
            }
        }
    }
}

