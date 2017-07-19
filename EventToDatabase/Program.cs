using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using MySql.Data.MySqlClient;

namespace EventToDatabase
{
    internal class Program
    {
        private static readonly string _conStr = ConfigurationManager.ConnectionStrings["mysqlconn"].ConnectionString;
        private static readonly string _dir = ConfigurationManager.AppSettings["dir"];
        private static void Main(string[] args)
        {
            StartWork();
            //Console.ReadLine();
        }

        private static void ReadTxt(string path, char type)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string msgs = "";
                string sqlStr1 = "";
                while (sr.Peek() > -1)
                {
                    msgs = sr.ReadLine();
                    if (msgs.Trim() == "")
                        break;
                    string[] msg = msgs.Split('\t');
                    int x1 = msg[0].IndexOf('#');
                    int x2 = msg[0].LastIndexOf('#');
                    int y1 = msg[0].IndexOf(':');
                    int y2 = msg[0].LastIndexOf(':');
                    string line = msg[0].Substring(1, x1 - 1);
                    string moduleID = msg[0].Substring(x1 + 1, x2 - x1 - 1);
                    string times = msg[0].Substring(y1 + 1, y2 - y1 - 1);
                    switch (type)
                    {
                        case 'a':
                            string sql1 = "INSERT INTO PDCOUNT2(Line, ModuleID, Times, PickupCount, PartNo, FIDL,ProgramName) VALUES ('{0}',{1},'{2}',{3},'{4}','{5}','{6}');";
                            sql1 = string.Format(sql1, line, moduleID, times, msg[8], msg[16], msg[18], msg[2]);
                            sqlStr1 += sql1;
                            break;

                        case 'b':
                            string sql2 = "INSERT INTO PDERROR(Line, ModuleID, Times,ProgramName,ErrorCode,PosNo,PartNo,FIDL,NozzleSer) VALUES ('{0}',{1},'{2}','{3}','{4}','{5}','{6}','{7}','{8}');";
                            sql2 = string.Format(sql2, line, moduleID, times, msg[2], msg[4], msg[12], msg[14], msg[16], msg[17]);
                            sqlStr1 += sql2;
                            break;

                        case 'c':
                            string sql3 = "INSERT INTO UNITINFO(Line, ModuleID, Times, UNITINFO, UNITType) VALUES ('{0}',{1},'{2}','{3}','{4}');";
                            sql3 = string.Format(sql3, line, moduleID, times, msg[1], msg[2]);
                            sqlStr1 += sql3;
                            break;
                        case 'd':
                            string sql4 = "INSERT INTO BOARDCOUNT(Line, ModuleID, Times, ProgramName, BoardsSkipped,BoardCount) VALUES ('{0}',{1},'{2}','{3}',{4},{5});";
                            sql4 = string.Format(sql4, line, moduleID, times, msg[2], msg[4],msg[5]);
                            sqlStr1 += sql4;
                            break;
                    }
                }
                sr.Close();
                if (ExecuteNonQuery(sqlStr1))
                {
                    File.Delete(path);
                    //Console.WriteLine(path + msgs.Length);
                }
            }
        }

        private static void StartWork()
        {
            //DirectoryInfo dir = new DirectoryInfo("D:\PROJECT\FlexaModuleEvent\FlexaModuleEvent\bin\Debug\Data");
            DirectoryInfo dir = new DirectoryInfo(_dir);
            FileInfo[] files1 = dir.GetFiles("*.a");
            FileInfo[] files2 = dir.GetFiles("*.b");
            FileInfo[] files3 = dir.GetFiles("*.c");
            FileInfo[] files4 = dir.GetFiles("*.d");
            if (files1.Length > 0)
            {
                foreach (var file in files1)
                {
                    Console.WriteLine(file.FullName);
                    ReadTxt(file.FullName, 'a');
                }
            }
            if (files2.Length > 0)
            {
                foreach (var file in files2)
                {
                    Console.WriteLine(file.FullName);
                    ReadTxt(file.FullName, 'b');
                }
            }
            if (files3.Length > 0)
            {
                foreach (var file in files3)
                {
                    Console.WriteLine(file.FullName);
                    ReadTxt(file.FullName, 'c');
                }
            }
            if (files4.Length > 0)
            {
                foreach (var file in files4)
                {
                    Console.WriteLine(file.FullName);
                    ReadTxt(file.FullName, 'd');
                }
            }
        }

        #region 执行事务

        /// <summary>

        /// 执行事务

        /// </summary>

        public static bool ExecuteNonQuery(string cmdText)
        {
            using (MySqlConnection conn = new MySqlConnection(_conStr))
            {
                MySqlTransaction sqlTran = null;
                try
                {
                    if (conn.State != ConnectionState.Open) { conn.Open(); } 
                    sqlTran = conn.BeginTransaction();
                    MySqlCommand cmd = conn.CreateCommand();
                    cmd.Transaction = sqlTran;
                    cmd.CommandText = cmdText;
                    cmd.ExecuteNonQuery();
                    sqlTran.Commit();
                    return true;
                }
                catch (Exception)
                {
                    sqlTran.Rollback();
                    return false;
                }
            }
        }

        #endregion 执行事务
    }
}