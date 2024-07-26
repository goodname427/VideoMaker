using System;
using System.Collections.Generic;
using System.IO;
using System.Speech.Synthesis;

namespace Framework
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            //GetAllVoice();
            Start();
            //Console.WriteLine(TextGetter.GetText().Result);
            Console.WriteLine("视频生成完毕");
            Console.ReadLine();
        }

        public static void Start()
        {
            //读取配置, 初始化视频制作器
            var config = new Config(VideoMaker.WorkPath + "config.txt");
            var videoMaker = new VideoMaker()
            {
                VideoExtension = config["VideoExtension"],
                BGMExtension = config["BGMExtension"],
                Voice = config["Voice"]
            };

            //设置参数
            string text = null;
            string fileName = null;
            switch (config.GetInt32("TextSourceMode"))
            {
                case 0:
                    (fileName, text) = TextGetter.GetText(config.GetInt32("MinSize"),config.GetInt32("MaxSize")).Result;
                    Console.WriteLine(fileName);
                    break;
                case 1:
                    text = File.ReadAllText(VideoMaker.WorkPath + config["TextFilename"]);
                    fileName = config["OutputFilename"];
                    break;
            }

            //合成视频
            videoMaker.MakeVideo(VideoMaker.WorkPath + config["VideoPath"], text, fileName, VideoMaker.WorkPath + config["BGMPath"]);

        }

        public static void GetAllVoice()
        {
            var sy = new SpeechSynthesizer();
            foreach (var voice in sy.GetInstalledVoices())
                Console.WriteLine($"{voice.VoiceInfo.Name} {voice.Enabled}");
        }
    }
}
