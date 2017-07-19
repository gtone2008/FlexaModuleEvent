using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace FlexaModuleEvent
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        private static Queue queue1 = new Queue();//PDCOUNT2
        private static Queue queue2 = new Queue();//PDERROR
        private static Queue queue3 = new Queue();//UNITINFO


        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form form1 = new Form1();
            var clientHostList = ConfigurationManager.AppSettings["FlexaServers"].Split(',');
            foreach (var host in clientHostList)
            {
                form1.Controls[0].Text += host + "   " + DateTime.Now + "\n";
                ThreadPool.QueueUserWorkItem(start => startWork(host, "moduleevent\0"), null);
            }
            ThreadPool.QueueUserWorkItem(start => writeFileThread(), null);
            Application.Run(form1);
        }

        private static void writeFileThread()
        {
            int flag1 = 0, flag2 = 0, flag3 = 0;
            Random rd = new Random();
            while (true)
            {
                if (queue1.Count > 0)
                {
                    if (File.Exists("Data/PDCOUNT2.txt") && flag1 > 19)
                    {
                        DateTime dt = DateTime.Now;
                        string dt1 = string.Format("{0:yyyyMMddHHmmssffff}", dt);
                        File.Move("Data/PDCOUNT2.txt", "Data/" + dt1 + ".a");
                        flag1 = 0;
                    }
                    writeFile(queue1.Dequeue(), "Data/PDCOUNT2.txt");
                    flag1++;
                }
                if (queue2.Count > 0)
                {
                    if (File.Exists("Data/PDERROR.txt"))
                    {
                        DateTime dt = DateTime.Now;
                        string dt1 = string.Format("{0:yyyyMMddHHmmssffff}", dt);
                        File.Move("Data/PDERROR.txt", "Data/" + dt1 + rd.Next() + ".b");
                        //flag2 = 0;
                    }
                    writeFile(queue2.Dequeue(), "Data/PDERROR.txt");
                    //flag2++;
                }
                if (queue3.Count > 0)
                {
                    if (File.Exists("Data/UNITINFO.txt"))
                    {
                        DateTime dt = DateTime.Now;
                        string dt1 = string.Format("{0:yyyyMMddHHmmssffff}", dt);
                        File.Move("Data/UNITINFO.txt", "Data/" + dt1 + rd.Next() + ".c");
                        //flag3 = 0;
                    }
                    writeFile(queue3.Dequeue(), "Data/UNITINFO.txt");
                    //flag3++;
                }
            }
        }

        private static void startWork(string host, string strCommand)
        {
            try
            {
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(host), 53341);
                Socket clentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clentSocket.BeginConnect(ipe, result =>
                {
                    if (result.IsCompleted)
                    {
                        sendCommand(strCommand, clentSocket);
                        receiveMessage(clentSocket);
                    }
                    else
                        return;
                }, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error: " + ex.Message);
            }
        }

        private static void receiveMessage(Socket clentSocket)
        {
            if (clentSocket == null || !clentSocket.Connected) return;
            byte[] recBytes = new byte[4096];
            clentSocket.BeginReceive(recBytes, 0, recBytes.Length, SocketFlags.None, asyncResult =>
            {
                int length = clentSocket.EndReceive(asyncResult);
                string msg = Encoding.ASCII.GetString(recBytes, 0, length);
                Regex rg1 = new Regex(@"\[BAY.{5,15}PDCOUNT2([\s\S]*?)*?,");
                Regex rg2 = new Regex(@"\[BAY.{5,15}PDERROR([\s\S]*?)*?,");
                Regex rg3 = new Regex(@"\[BAY.{5,15}UNITINFO([\s\S]*?)*?,");
                var collMsg1 = rg1.Matches(msg);
                var collMsg2 = rg2.Matches(msg);
                var collMsg3 = rg3.Matches(msg);
                foreach (var m in collMsg1)
                {
                    queue1.Enqueue(m);
                }
                foreach (var m in collMsg2)
                {
                    queue2.Enqueue(m);
                }
                foreach (var m in collMsg3)
                {
                    queue3.Enqueue(m);
                }
                receiveMessage(clentSocket);
            }, null);
        }

        private static void sendCommand(string strCommand, Socket clentSocket)
        {
            if (clentSocket == null || !clentSocket.Connected) return;
            byte[] sendBytes = Encoding.ASCII.GetBytes(strCommand);
            clentSocket.BeginSend(sendBytes, 0, sendBytes.Length, SocketFlags.None, result =>
            {
                clentSocket.EndSend(result);
            }, null);
        }

        private static void writeFile(object msg, string name)
        {
            using (StreamWriter sw = File.AppendText(name))
            {
                sw.Write(msg + "\r\n");
                sw.Close();
            }
        }
    }
}