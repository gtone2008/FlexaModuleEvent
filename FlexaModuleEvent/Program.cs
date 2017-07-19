using System;
using System.Threading;
using System.Windows.Forms;

namespace FlexaModuleEvent
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);          
            bool bExist;
            Mutex MyMutex = new Mutex(true, " Only Run Once A Time ", out  bExist);
            if (bExist)
            {
                Application.Run(new Form1());
                MyMutex.ReleaseMutex();
            }
            //else
            //{
            //    MessageBox.Show(" 程序已经运行！ ", " 信息提示 ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}

        }
    }
}