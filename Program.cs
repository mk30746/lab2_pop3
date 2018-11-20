using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Media;

namespace pop3
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> PastMails = new List<string>();
            List<string> CurrentMails = new List<string>();
            List<string> NewMails = new List<string>();
            string[] SpaceStr = new string[] { " " };
            string[] Separator = new string[] { ":" };
            string[] SplitStr;
            string line;
            int counter = 0;
            List<string> config = new List<string>();

            System.IO.StreamReader file = new System.IO.StreamReader(@"PopApp.config");
            while ((line = file.ReadLine()) != null)
            {
                SplitStr = line.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                config.Add(SplitStr[1]);
            }
            file.Close();

            string username = config[0];
            string passsword = config[1];
            string adres = config[2];
            int port = Convert.ToInt32(config[3]);
            int delayTime;
            if (Convert.ToInt32(config[4]) >= 5)
            {
                delayTime = Convert.ToInt32(config[4]);
            }
            else
            {
                Console.WriteLine("Delay time too low, setting to 5 sec");
                delayTime = 5;
            }

            Socket NetStreamSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            NetStreamSocket.Connect(adres, port);

            IPEndPoint remoteIpEndPoint = NetStreamSocket.RemoteEndPoint as IPEndPoint;
            IPEndPoint localIpEndPoint = NetStreamSocket.LocalEndPoint as IPEndPoint;


            if (remoteIpEndPoint != null)
            {
                // Using the RemoteEndPoint property.
                Console.WriteLine("I am connected to " + remoteIpEndPoint.Address + " on port number " + remoteIpEndPoint.Port);
            }

            if (localIpEndPoint != null)
            {
                // Using the LocalEndPoint property.
                Console.WriteLine("My local IP address is: " + localIpEndPoint.Address + " on port number " + localIpEndPoint.Port);
            }
            string str = string.Empty;
            string strTemp = string.Empty;
            using (var NetStream = new NetworkStream(NetStreamSocket))
            {
                using (StreamReader StrmRead = new StreamReader(NetStream))
                {
                    using (StreamWriter StrmWriter = new StreamWriter(NetStream))
                    {
                        StrmWriter.WriteLine("USER " + username);
                        StrmWriter.Flush();
                        StrmRead.ReadLine();
                        StrmWriter.WriteLine("PASS " + passsword);
                        StrmWriter.Flush();
                        StrmRead.ReadLine();
                        StrmWriter.WriteLine("UIDL");
                        StrmWriter.Flush();


                        while ((strTemp = StrmRead.ReadLine()) != null)
                        {
                            if (strTemp == "." || strTemp.IndexOf("-ERR") != -1)
                            {
                                break;
                            }
                            if (strTemp == "." || strTemp.IndexOf("+OK") == -1)
                            {
                                SplitStr = strTemp.Split(SpaceStr, StringSplitOptions.RemoveEmptyEntries);
                                PastMails.Add(SplitStr[0]);
                                PastMails.Add(SplitStr[1]);
                            }
                            str = str + strTemp + "\n";
                        }
                        Console.WriteLine("Found "+PastMails.Count/2+" old mails");
                        str = string.Empty;
                    }
                }
            }
            NetStreamSocket.Shutdown(SocketShutdown.Both);
            NetStreamSocket.Disconnect(true);
            NetStreamSocket.Close();
            NetStreamSocket.Dispose();
            NetStreamSocket = null;

            Thread.Sleep(5 * 1000);
            do
            {
                while (!Console.KeyAvailable)
                {
                    NetStreamSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    NetStreamSocket.Connect(adres, port);
                    remoteIpEndPoint = NetStreamSocket.RemoteEndPoint as IPEndPoint;
                    localIpEndPoint = NetStreamSocket.LocalEndPoint as IPEndPoint;
                    using (var myStream = new NetworkStream(NetStreamSocket))
                    {
                        using (StreamReader StrmRead = new StreamReader(myStream))
                        {
                            using (StreamWriter StrmWriter = new StreamWriter(myStream))
                            {
                                StrmWriter.WriteLine("USER " + username);
                                StrmWriter.Flush();
                                //Console.WriteLine(sr.ReadLine());
                                StrmRead.ReadLine();
                                StrmWriter.WriteLine("PASS " + passsword);
                                StrmWriter.Flush();
                                StrmRead.ReadLine();
                                StrmWriter.WriteLine("RSET");
                                StrmWriter.Flush();
                                StrmWriter.WriteLine("UIDL");
                                StrmWriter.Flush();
                                while ((strTemp = StrmRead.ReadLine()) != null)
                                {
                                    if (strTemp == "." || strTemp.IndexOf("-ERR") != -1)
                                    {
                                        break;
                                    }
                                    if (strTemp == "." || strTemp.IndexOf("+OK") == -1)
                                    {
                                        SplitStr = strTemp.Split(SpaceStr, StringSplitOptions.RemoveEmptyEntries);
                                        CurrentMails.Add(SplitStr[0]);
                                        CurrentMails.Add(SplitStr[1]);
                                    }
                                    str = str + strTemp + "\n";
                                }
                                for (int i = 1; i < CurrentMails.Count; i = i + 2)
                                {
                                    if (!PastMails.Contains(CurrentMails[i]))
                                    {
                                        NewMails.Add(CurrentMails[i - 1]);
                                    }
                                }
                                if (NewMails.Count > 0)
                                {
                                    if (NewMails.Count == 1)
                                    {
                                        Console.WriteLine("New message found.\n");
                                        counter++;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Found " + NewMails.Count + " new messages.\n");
                                    }

                                    for (int i = 0; i < NewMails.Count; i++)
                                    {
                                        StrmWriter.WriteLine("TOP " + NewMails[i] + " 0");
                                        StrmWriter.Flush();
                                        while ((strTemp = StrmRead.ReadLine()) != null)
                                        {
                                            if (strTemp == "." || strTemp.IndexOf("-ERR") != -1)
                                            {
                                                break;
                                            }
                                            if (Regex.IsMatch(strTemp, "^*Subject:*"))
                                            {
                                                Console.WriteLine("Subject: " + strTemp);
                                            }
                                            if (Regex.IsMatch(strTemp, @"^*From: ([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$"))
                                            {
                                                SplitStr = strTemp.Split(SpaceStr, StringSplitOptions.RemoveEmptyEntries);
                                                Console.WriteLine("From: " + SplitStr[1] + "\n");
                                            }
                                            //Console.Write(strTemp);
                                            str = str + strTemp + "\n\n";
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("No new messages.\n");
                                }


                                PastMails.Clear();
                                PastMails.AddRange(CurrentMails);
                                CurrentMails.Clear();
                                NewMails.Clear();


                            }
                        }
                    }
                    NetStreamSocket.Shutdown(SocketShutdown.Both);
                    NetStreamSocket.Disconnect(true);
                    NetStreamSocket.Close();
                    NetStreamSocket.Dispose();
                    NetStreamSocket = null;
                    Thread.Sleep(delayTime * 1000);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Q);
            Console.WriteLine("Received "+counter+" mails");
            Console.WriteLine("Press any key to end.");
            Console.ReadKey();
            
            return;
        }
    }
}
