using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

/* 
============================================
               TCP Thing
============================================

Author: Kary Wall
Version 1.5
Date: 12/30/21

This app is for testing various TCP connections and socket options
and/or various TCP connection based testing. Right now only keep-alive 
is configurable by passing in arguments. It needs some refactoring,
cleanup and better coding but it's something I modify as needed 
since it's just a test tool for corner-case issues.


USAGE: TCPThing <IPAddress | Hostname> <TCPPort> <IsServer {0|1}> [KeepAlive {0=SystemSetting | UserValue}]

Arg1 <required>: The IP
Arg2 <required>: The port
Arg3 <required>: Run as server or client: 1=server, client=0 
Arg4 <optional>: If arg 4 is present, turn on keep alive
                 0 = use system value (default or reg key).
                >0 = use this value as a user-defined keep-alive value.

Examples:

Create a listener on port 80: TCPThing 127.0.0.1 80 1 0
Create a client on port 80: TCPThing 127.0.0.1 80 0 0
Create a listener w/keep-alive on port 50000: TCPThing 127.0.0.1 5000 1 1
Create a client w/keep-alive of 1 minutes: TCPThing 127.0.0.1 80 0 1
Create a client w/keep-alive of 30 minutes: TCPThing 127.0.0.1 80 0 30
Create a server w/keep-alive of 4 minutes on port 8081: TCPThing 127.0.0.1 8081 1 4
Create a client on port 80 w/keep-alive using system default values: TCPThing 127.0.0.1 80 0 0

Note: When running as a server on a remote machine, you must ensure the port used is open in the firewall.

 */

namespace TCPThing
{
    class Program
    {
        static TcpListener listener;
        static TcpClient client;
        static IPAddress ip = null;
        static int port = 0;
        static int userKeepAliveTime = -1;
        static int systemKeepAliveTime = 120;
        static object systemKeepAliveRegTime;
        static string keepAliveChosenText = "None";
        static System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        static int rtnVal = 0;

        //Regex patterns to identify valid hostname or IP address
        static string validIpAddress = @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$";
        static string validHostname = @"^(([a-zA-Z]|[a-zA-Z][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z]|[A-Za-z][A-Za-z0-9\-]*[A-Za-z0-9])$";

        static void Main(string[] args)
        {
            PrintTitle();

            if (args.Length < 3)
            {
                PrintMessage("USAGE:\r\n");
                PrintMessage("Create a listener: TCPThing 127.0.0.1 80 1 0");
                PrintMessage("Create a client: TCPThing 127.0.0.1 80 0 0");
                PrintMessage("Create a listener w/keep-alive: TCPThing 127.0.0.1 80 1 1");
                PrintMessage("Create a client w/keep-alive: TCPThing 127.0.0.1 80 0 1");
                PrintMessage("Create a client w/keep-alive of 30 seconds: TCPThing 127.0.0.1 80 0 30");
                PrintMessage("Create a server w/keep-alive of 1 minute: TCPThing 127.0.0.1 80 1 60");
                PrintMessage("Create a client w/keep-alive system default values: TCPThing 127.0.0.1 80 0 1");
                PrintMessage(String.Empty);

                // Press any key to exit.
                Console.ReadKey();
                rtnVal = unchecked((int)0x80070057); //ArgumentException for console return code
                Environment.Exit(rtnVal);
            }

            // Grab the current registry keep-alive time
            systemKeepAliveRegTime = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Tcpip\Parameters", "KeepAliveTime", null);

            // If reg-key was set, set our system KAtime to that, otherwise assume default of 120
            if (null != systemKeepAliveRegTime)
            {
                Int32.TryParse(systemKeepAliveRegTime.ToString(), out systemKeepAliveTime);
            }

            // Enable keep-alive if 4th argument exists. If the value is 0 use system setting, 
            // otherwise use the 4th arg as the user-supplied value
            if (args.Length == 4)
            {
                Int32.TryParse(args[3], out int tempVal);
                if (tempVal == 0)
                {
                    userKeepAliveTime = systemKeepAliveTime;
                    keepAliveChosenText = "System";
                }
                else if (tempVal > 0)
                {
                    userKeepAliveTime = tempVal;
                    keepAliveChosenText = "User";
                }
            }

            try
            {
                #region Setup
                // uses dns if hostname supplied as first argument, the IP if the IP is supplied. 
                // If neither are supplied, uses the first IP returned if there are multiple IPs on the box.

                if (Regex.IsMatch(args[0], validHostname))
                {
                    // Hostname was used, perform a DNS lookup to get the IP
                    Console.WriteLine("Performing DNS lookup for " + args[0]);
                    stopWatch.Start();

                    IPHostEntry ipentry = System.Net.Dns.GetHostEntry(args[0]);
                    ip = ipentry.AddressList[0];

                    Console.WriteLine("Found {0} --> {1}", args[0], ip);
                    PrintMessage("DNS Latency: " + stopWatch.ElapsedMilliseconds + "ms");

                    stopWatch.Reset();
                }
                else if (Regex.IsMatch(args[0], validIpAddress))
                {
                    ip = IPAddress.Parse(args[0]);
                }
                else
                {
                    throw new ArgumentException("1st argument is neither an IP or a hostname");
                }

                if (null == ip)
                {
                    throw new ArgumentException("IP is null");
                }

                // second argument is always the second argument passed in
                port = Int32.Parse(args[1]);
                #endregion

                // Acts as a TCP server or client based on value of third argument.
                // 1 = act as server, 0 = act as client
                #region Server 

                if (Int32.Parse(args[2]) == 1) // Act as server
                {
                    Console.Title = "TCPThing as Server";
                    listener = new TcpListener(ip, port);

                    // setup keep-alive for server if arg 4 > -1
                    if (userKeepAliveTime > -1)
                    {
                        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        byte[] keepAliveVals = { 0x01, 0x00, 0x00, 0x00, 0x10, 0x27, 0x00, 0x00, 0x98, 0x3a, 0x00, 0x00 };
                        listener.Server.IOControl(IOControlCode.KeepAliveValues, keepAliveVals, BitConverter.GetBytes(userKeepAliveTime));
                    }

                    listener.Start();

                    Console.WriteLine("TCPThing is listening...");
                    Console.WriteLine("IP:{0}", args[0]);
                    Console.WriteLine("Port:{0}\r\n", port);
                    Console.WriteLine("Time: {0}", DateTime.Now);
                    Console.WriteLine("User Keep-Alive: {0}", userKeepAliveTime);
                    Console.WriteLine("System Keep-Alive: {0}", systemKeepAliveTime);
                    Console.WriteLine("Keep-Alive in use: {0}\r\n", keepAliveChosenText);
                    DoListen(listener);
                }
                #endregion
                else // Act as client if server/client arg is 0
                #region Client
                {
                    Console.Title = "TCPThing as Client";

                reconnect:

                    client = new TcpClient();

                    // setup keep-alive for client if arg 4 > -1
                    if (userKeepAliveTime > -1)
                    {
                        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        byte[] keepAliveVals = { 0x01, 0x00, 0x00, 0x00, 0x10, 0x27, 0x00, 0x00, 0x98, 0x3a, 0x00, 0x00 };
                        client.Client.IOControl(IOControlCode.KeepAliveValues, keepAliveVals, BitConverter.GetBytes(userKeepAliveTime));
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("TCPThing is connecting to {0}:{1}...\r\n", args[0], port);
                    stopWatch.Start();
                    client.Connect(args[0], port);

                    if (client.Connected)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("{0}: Connection successful to {1} --> {2}\r\n", DateTime.Now, args[0], ip);
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine("Local Endpoint: {0}", client.Client.LocalEndPoint);
                        Console.WriteLine("User Keep-Alive: {0}", userKeepAliveTime);
                        Console.WriteLine("System Keep-Alive: {0}", systemKeepAliveTime);
                        Console.WriteLine("Keep-Alive in use: {0}", keepAliveChosenText);
                        Console.WriteLine("TCP Latency: " + stopWatch.ElapsedMilliseconds.ToString() + "ms\r\n");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Not connected: " + args[0]);
                        client.Client.Shutdown(SocketShutdown.Both);
                        client.Client.Close();
                        client.Close();
                    }

                    // Wait here once the client is connected - use ReadKey() which waits on user input.
                    // If the user presses any key, close the client connection and try to reconnect again.
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("\r\nPress any key to reconnect...");
                    Console.ReadKey();
                    Console.WriteLine("\r\n");
                    stopWatch.Reset();

                    if (client.Connected)
                    {
                        client.Client.Shutdown(SocketShutdown.Both);
                        client.Client.Disconnect(false);
                        client.Client.Close();
                        client.Close();
                    }
                    goto reconnect;
                }
                #endregion

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                PrintMessage("Error: " + ex.Message);
                rtnVal = ex.HResult;
            }

            // exit the app
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(rtnVal);
        }

        static void DoListen(TcpListener listener)
        {
        // This method is just for the server. It constantly listens for new connections.
        waitForNextConnection:

            Socket socket = listener.AcceptSocket(); // this is a blocking call which stalls until a new connection arrives.
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("{0}: Connection accepted from {1} ", DateTime.Now, socket.RemoteEndPoint);
            Console.WriteLine("Local Endpoint: {0}", listener.Server.LocalEndPoint);
            Console.ForegroundColor = ConsoleColor.Gray;

            // Uncomment the code below if you want the server/listener accecpt only one connection,
            // wait 20 seconds (example), then auto-close the connection gracefully (FIN) and start waiting for new connections.

            //int counter = 20;

            //while (socket.Connected)
            //{
            //    System.Threading.Thread.Sleep(1000);
            //    Console.WriteLine("Connected..., closing in {0} seconds", counter);
            //    counter -= 1;
            //    if(counter <1 )
            //    {
            //        socket.Close();
            //        break; 
            //    }
            //}

            // Console.WriteLine("Socket closed, waiting for new connection...");

        goto waitForNextConnection;
        }

        static void PrintTitle()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("========================================");
            Console.WriteLine("               TCP Thing                ");
            Console.WriteLine("========================================\r\n");
            Console.Title = "TCPThing";
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        static void PrintMessage(String message)
        {
            Console.WriteLine(message + Environment.NewLine);
        }
    }
}
