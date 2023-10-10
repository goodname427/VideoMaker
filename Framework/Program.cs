using System;
using System.Collections.Generic;
using System.IO;
using System.Speech.Synthesis;

namespace Framework
{
    internal class Program
    {
        static Dictionary<string, string> s_config;
        static VideoMaker s_maker;

        static void Main(string[] args)
        {
            //GetAllVoice();
            //Start();
            Console.WriteLine(TextGetter.GetText().Result);
            Console.ReadLine();
        }

        static void Start()
        {
            //读取配置初始化视频制作器
            ReadConfig();
            s_maker = new VideoMaker()
            {
                VideoExtension = s_config["VideoExtension"],
                Voice = s_config["Voice"]
            };

            //设置参数
            string text = null;
            string fileName = null;
            switch (int.Parse(s_config["StartMode"]))
            {
                case 0:
                    (fileName, text) = TextGetter.GetText(int.Parse(s_config["MinSize"]), int.Parse(s_config["MaxSize"])).Result;
                    Console.WriteLine(fileName);
                    break;
                case 1:
                    text = File.ReadAllText(VideoMaker.WorkPath + s_config["TextPath"]);
                    fileName = s_config["OutputFileName"];
                    break;
            }

            //合成视频
            s_maker.GetVideo(VideoMaker.WorkPath + s_config["VideoPath"], text, fileName);

        }

        static void GetAllVoice()
        {
            var sy = new SpeechSynthesizer();
            foreach (var voice in sy.GetInstalledVoices())
                Console.WriteLine($"{voice.VoiceInfo.Name} {voice.Enabled}");
        }

        /// <summary>
        /// 读取配置文件
        /// </summary>
        static void ReadConfig()
        {
            s_config = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(VideoMaker.WorkPath + "config.txt"))
            {
                var kvp = line.Split('=');
                s_config.Add(kvp[0], kvp[1]);
            }
        }
    }
}
