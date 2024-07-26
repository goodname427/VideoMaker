using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    internal class Command
    {
        /// <summary>
        /// 执行命令行
        /// </summary>
        /// <param name="cmd"></param>
        public static void Execute(string cmd)
        {
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c" + cmd;
            process.StartInfo.UseShellExecute = false; //是否使用操作系统shell启动
            process.StartInfo.CreateNoWindow = false; //是否在新窗口中启动该进程的值 (不显示程序窗口)
            process.Start();
            process.WaitForExit(); //等待程序执行完退出进程
            process.Close();

        }
        /// <summary>
        /// 执行命令行并返回执行结果
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static string ExecuteWithResult(string cmd)
        {
            var process = new Process();
            //初始化
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false; //是否使用操作系统shell启动
            process.StartInfo.RedirectStandardInput = true; //接受来自调用程序的输入信息
            process.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
            process.StartInfo.RedirectStandardError = true; //重定向标准错误输出
            process.StartInfo.CreateNoWindow = true; //不显示程序窗口
            //执行指令
            process.Start();
            process.StandardInput.WriteLine(cmd + "&exit");
            process.StandardInput.AutoFlush = true;
            var result = process.StandardOutput.ReadToEnd();
            //退出cmd
            process.WaitForExit();//等待程序执行完退出进程
            process.Close();
            return result;
        }
    }
}
