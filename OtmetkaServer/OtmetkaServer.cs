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
        static void Main(string[] args)
        {
            TcpListener Listener;
            {
                string HostName = Dns.GetHostName();
                IPHostEntry HostInfo = Dns.GetHostEntry(HostName);
                IPAddress LocalAddr = HostInfo.AddressList[1];
                Listener = new TcpListener(LocalAddr, 4620);
            }
            Listener.Start();
            while (true)
            {
                var ClientSocket = Listener.AcceptSocket();
                OtmetkaClientTask.Run(ClientSocket);
            }
        }
    }
}

