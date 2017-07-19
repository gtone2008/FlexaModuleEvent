using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlexaModuleEvent
{
    public partial class Form1 : Form
    {
        private Queue<string> queue1 = new Queue<string>();//PDCOUNT2
        private Queue<string> queue2 = new Queue<string>();//PDERROR
        private Queue<string> queue3 = new Queue<string>();//UNITINFO
        private Queue<string> queue4 = new Queue<string>();//BOARDCOUNT

        private string[] clientHostList;
        //ArrayList threadList = new ArrayList();
        //ArrayList socketList = new ArrayList();

        public Form1()
        {
            InitializeComponent();
            timer1.Enabled = false;
            timer1.Interval = 300000;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            clientHostList = ConfigurationManager.AppSettings["FlexaServers"].Split(',');
            ThreadStart();
        }

        private void ThreadStart()
        {
            TraceHelper.GetInstance().test("-------------------------");
            foreach (var host in clientHostList)
            {
                Thread th = new Thread(start => startWork(host, "moduleevent\0"));
                th.IsBackground = true;
                th.Name = host;
                th.Start();
                //threadList.Add(th);
            }
            for (int i = 0; i < 7; i++)
            {
                Thread tw = new Thread(start => writeFileThread());
                tw.IsBackground = true;
                tw.Start();
            }

        }

        private void writeFileThread()
        {
            int flag1 = 0; //flag2 = 0; flag3 = 0;    
            int n = Thread.CurrentThread.ManagedThreadId;
            int x;
            Random rd = new Random();
            while (true)
            {
                x = rd.Next();
                try
                {

                    if (File.Exists($"Data/PDCOUNT2{n}.txt") && flag1 > 19)
                    {
                        File.Move($"Data/PDCOUNT2{n}.txt", "Data/" + DateTime.Now.ToString($"{x}{n}MMddHHmmssffff") + ".a");
                        flag1 = 0;
                    }
                    if (queue1.Count > 0)
                    {
                        writeFile(queue1.Dequeue(), $"Data/PDCOUNT2{n}.txt");
                        flag1++;
                    }
                    if (File.Exists($"Data/PDERROR{n}.txt"))
                    {
                        File.Move($"Data/PDERROR{n}.txt", "Data/" + DateTime.Now.ToString($"{x}{n}MMddHHmmssffff") + ".b");
                    }
                    if (queue2.Count > 0)
                    {

                        writeFile(queue2.Dequeue(), $"Data/PDERROR{n}.txt");
                    }

                    if (File.Exists($"Data/UNITINFO{n}.txt"))
                    {

                        File.Move($"Data/UNITINFO{n}.txt", "Data/" + DateTime.Now.ToString($"{x}{n}MMddHHmmssffff") + ".c");
                    }

                    if (queue3.Count > 0)
                    {
                        writeFile(queue3.Dequeue(), $"Data/UNITINFO{n}.txt");
                    }

                    if (File.Exists($"Data/BOARDCOUNT{n}.txt") && flag1 > 9)
                    {

                        File.Move($"Data/BOARDCOUNT{n}.txt", "Data/" + DateTime.Now.ToString($"{x}{n}MMddHHmmssffff") + ".d");
                    }

                    if (queue4.Count > 0)
                    {
                        writeFile(queue4.Dequeue(), $"Data/BOARDCOUNT{n}.txt");
                    }

                }
                catch (Exception ex)
                {
                    //TraceHelper.GetInstance().test($"writeFileThread{n}---" + ex.Message);
                    Thread.Sleep(10);
                }
            }
        }



        /// 设置心跳    
        //private static void heartbeat(Socket clentSocket)
        //{
        //    byte[] inValue = new byte[] { 1, 0, 0, 0, 0x88, 0x13, 0, 0, 0xd0, 0x07, 0, 0 };// 首次探测时间5 秒, 间隔侦测时间2 秒  
        //    clentSocket.IOControl(IOControlCode.KeepAliveValues, inValue, null);
        //}

        private void startWork(string host, string strCommand)
        {

            try
            {
                Socket clentSocket;
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(host), 53341);
                clentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clentSocket.Connect(ipe);
                //heartbeat(clentSocket);
                clentSocket.ReceiveTimeout = 60000;
                clentSocket.SendTimeout = 60000;
                TraceHelper.GetInstance().test(host + ":" + AppDomain.GetCurrentThreadId() + "--connected--" + "--" + clentSocket.LocalEndPoint);
                sendCommand(strCommand, clentSocket, host);
                receiveMessage(clentSocket, host);

            }
            catch (SocketException ex)
            {
                TraceHelper.GetInstance().test(host + "---connect error " + ex.Message);
                Thread.Sleep(300000);
                startWork(host, strCommand);
            }
        }

        /// <summary>
        /// test connect
        /// </summary>
        /// <param name="clentSocket"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private bool testService(Socket clentSocket, string host)
        {
            byte[] recBytes = new byte[8192];
            try
            {
                sendCommand("ccmclist\0", clentSocket, host);
                if (clentSocket.Receive(recBytes) > 0)
                {
                    return true;
                }
                else
                    return false;
            }
            catch (SocketException ex)
            {
                TraceHelper.GetInstance().test(host + "---test error " + ex.ErrorCode);
                if (clentSocket.Connected == true)
                {
                    clentSocket.Shutdown(SocketShutdown.Both);
                    clentSocket.Close();
                }
                startWork(host, "moduleevent\0");
                return false;
            }
        }

        private void receiveMessage(Socket clentSocket, string host)
        {

            //TraceHelper.GetInstance().test(host + "---begin receive");
            Regex rg1 = new Regex(@"\[BAY.{5,15}PDCOUNT2([\s\S]*?)*?,", RegexOptions.Compiled);
            Regex rg2 = new Regex(@"\[BAY.{5,15}PDERROR([\s\S]*?)*?,", RegexOptions.Compiled);
            Regex rg3 = new Regex(@"\[BAY.{5,15}UNITINFO([\s\S]*?)*?,", RegexOptions.Compiled);
            Regex rg4 = new Regex(@"\[BAY.{5,15}BOARDCOUNT([\s\S]*?)*?,", RegexOptions.Compiled);
            byte[] recBytes = new byte[1024 * 1024];
            string msg = "";
            while (true)
            {
                try
                {
                    //throw new Exception("aaa");
                    msg = Encoding.ASCII.GetString(recBytes, 0, clentSocket.Receive(recBytes));
                    MatchCollection collMsg1 = rg1.Matches(msg);
                    MatchCollection collMsg2 = rg2.Matches(msg);
                    MatchCollection collMsg3 = rg3.Matches(msg);
                    MatchCollection collMsg4 = rg4.Matches(msg);

                    if (collMsg1.Count > 0)
                    {
                        getPackage(host, collMsg1, queue1);
                    }
                    if (collMsg2.Count > 0)
                    {
                        getPackage(host, collMsg2, queue2);
                    }
                    if (collMsg3.Count > 0)
                    {
                        getPackage(host, collMsg3, queue3);
                    }
                    if (collMsg4.Count > 0)
                    {
                        getPackage(host, collMsg4, queue4);
                    }

                }

                catch (Exception ex)
                {

                    if (ex.Message.EndsWith("respond"))
                    {
                        //TraceHelper.GetInstance().test(host + "---no respond:");
                        Thread.Sleep(10);
                        continue;
                    }
                    else
                    {
                        TraceHelper.GetInstance().test(host + "---error receive:" + ex.Message);
                        break;
                    }
                }
                finally
                {
                    msg = string.Empty; recBytes = new byte[1024 * 1024];
                }
                Thread.Sleep(0);
            }
            if (!testService(clentSocket, host))
            {
                //TraceHelper.GetInstance().test(host + "---reconnect");
                if (clentSocket.Connected == true)
                {
                    clentSocket.Shutdown(SocketShutdown.Both);
                    clentSocket.Close();
                }

                startWork(host, "moduleevent\0");
            }
            else
            {
                //TraceHelper.GetInstance().test(host + "---timeout receive");   
                sendCommand("moduleevent\0", clentSocket, host);
                receiveMessage(clentSocket, host);
            }
        }




        /// <summary>
        /// handle reciver msg
        /// </summary>

        private void getPackage(string host, MatchCollection collMsg, Queue<string> queue)
        {
            try
            {

                foreach (Match m in collMsg)
                {
                    queue.Enqueue(m.Value);
                }

            }
            catch (Exception)
            {
                //TraceHelper.GetInstance().test("msg error---" + host + "--" + ex.Message);
                //throw ex;
            }
        }


        private void sendCommand(string strCommand, Socket clentSocket, string host)
        {
            byte[] sendBytes = Encoding.ASCII.GetBytes(strCommand);
            try
            {
                clentSocket.Send(sendBytes);
            }
            catch
            {
                if (clentSocket.Connected == true)
                {
                    clentSocket.Shutdown(SocketShutdown.Both);
                    clentSocket.Close();
                }

                startWork(host, "moduleevent\0");
            }
        }

        private void writeFile(string msg, string name)
        {
            try
            {
                if (!string.IsNullOrEmpty(msg))
                {
                    using (StreamWriter sw = File.AppendText(name))
                    {
                        sw.Write(msg + "\r\n");
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                TraceHelper.GetInstance().test("writeFile---" + ex.Message);
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            TraceHelper.GetInstance().test("exit");
            this.Close();
            Application.Exit();
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
            TraceHelper.GetInstance().test("--------------------------------");
            //foreach (Thread thre in threadList)
            //{
            //    TraceHelper.GetInstance().test(thre.Name + "--" + AppDomain.GetCurrentThreadId());
            //}
            //foreach (Socket item in socketList)
            //{
            //    string rem = "";
            //    try
            //    {
            //        rem = item.RemoteEndPoint.ToString();
            //        TraceHelper.GetInstance().test(rem);
            //    }
            //    catch (Exception ex)
            //    {
            //        TraceHelper.GetInstance().test(ex.Message);
            //    }

            //string remoteIP = rem.Remove(rem.IndexOf(':'));

            //}
        }
    }
}