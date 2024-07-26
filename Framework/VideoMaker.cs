using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using static System.Net.Mime.MediaTypeNames;

namespace Framework
{
    internal class VideoMaker
    {
        /// <summary>
        /// 工作文件夹
        /// </summary>
        public static string WorkPath { get; private set; } =
#if DEBUG
                @"E:\CGL\Projects\2023DY\";
#else
                Directory.GetCurrentDirectory();
#endif

        /// <summary>
        /// 临时音频文件位置
        /// </summary>
        private string OutputAudio => TempPath + "out_audio.wav";
        /// <summary>
        /// 临时源音频文件位置
        /// </summary>
        private string SourceAudioNoUse => TempPath + "src_audio.wav";
        /// <summary>
        /// 临时源音频文件位置
        /// </summary>
        private string SourceAudio => TryGetSourceFile(SourceAudioNoUse, OutputAudio);
        
        /// <summary>
        /// 临时输出视频文件位置
        /// </summary>
        private string OutputVideo => TempPath + $"out{VideoExtension}";
        /// <summary>
        /// 临时源视频文件位置
        /// </summary>
        private string SourceVideoNoUse => TempPath + $"src{VideoExtension}";
        /// <summary>
        /// 临时源视频文件位置，并且调整输出文件为源文件
        /// </summary>
        private string SourceVideo => TryGetSourceFile(SourceVideoNoUse, OutputVideo);
        
        /// <summary>
        /// 临时文本文件位置
        /// </summary>
        private string Txt => TempPath + "out_txt.txt";

        /// <summary>
        /// 临时文件夹
        /// </summary>
        public string TempPath { get; set; } = Path.GetTempPath() + "Temp\\";
        /// <summary>
        /// 输出文件夹
        /// </summary>
        public string OutputPath { get; set; } = WorkPath + "out\\";
        /// <summary>
        /// 视频文件扩展名
        /// </summary>
        public string VideoExtension { get; set; } = ".mkv";
        /// <summary>
        /// 音频文件扩展名
        /// </summary>
        public string BGMExtension { get; set; } = ".mp3";
        /// <summary>
        /// 使用的语音库
        /// </summary>
        public string Voice { get; set; } = "Microsoft Huihui Desktop";

        /// <summary>
        /// 尝试获取源文件，存在输出文件时将输出文件复制到源文件位置
        /// </summary>
        /// <param name="sourceNoUse"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        private string TryGetSourceFile(string sourceNoUse, string output)
        {
            if (File.Exists(output))
            {
                if (File.Exists(sourceNoUse))
                    File.Delete(sourceNoUse);
                File.Move(output, sourceNoUse);
                File.Delete(output);
            }
            return sourceNoUse;
        }


        /// <summary>
        /// 获取视频或者音频时长
        /// </summary>
        /// <param name="filename">视频或音频文件名</param>
        /// <returns></returns>
        private double GetMediaDuration(string filename)
        {
            var result = Command.ExecuteWithResult($"ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 -i \"{filename}\"");
            return double.Parse(Regex.Match(result, @"(\d+)(\.\d+?)?(?=\r\n)").Value);
        }
        /// <summary>
        /// 获取视频尺寸
        /// </summary>
        /// <param name="filename">视频文件名</param>
        private void GetVideoSize(string filename, out int width, out int height)
        {
            var result = Command.ExecuteWithResult($"ffprobe -v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 {filename}");
            var match = Regex.Match(result, @"(\d+)x(\d+)");
            (width, height) = (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }

        /// <summary>
        /// 生成音频和字幕
        /// </summary>
        /// <param name="text"></param>
        private TextInfo[] CreateAudioAndSubtitles(string text)
        {
            //分割文本
            var segments = ChineseSentenceTokenizer.Tokenize1(text);
            //获取字幕以及处理文本
            int i = 0;
            var list = new List<TextInfo>();
            PromptBuilder builder = new PromptBuilder();
            builder.AppendBookmark(" ");
            foreach (var segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                    continue;

                builder.AppendText(segment.Trim() + ",");
                builder.AppendBookmark(" ");
                list.Add(new TextInfo(segment)
                {
                    //FontSize = 10
                });
            }

            //获取音频以及字幕位置
            i = 0;
            var texts = list.ToArray();
            using (var synthesizer = new SpeechSynthesizer())
            {
                synthesizer.Rate = 4;
                synthesizer.SelectVoice(Voice);
                //synthesizer.SelectVoiceByHints(VoiceGender.Female);
                synthesizer.SetOutputToWaveFile(OutputAudio);
                synthesizer.BookmarkReached += (o, e) =>
                    {
                        if (i > 0)
                        {
                            texts[i - 1].ToTime = e.AudioPosition;
                        }
                        if (i < texts.Length)
                        {
                            texts[i].FromTime = e.AudioPosition;
                        }
                        i++;
                    };
                synthesizer.Speak(builder);
            }

            return texts;
        }
        /// <summary>
        /// 调整字幕换行
        /// </summary>
        /// <param name="texts"></param>
        /// <returns></returns>
        private TextInfo[] BreakSubtitle(TextInfo[] texts)
        {
            //自适应屏幕
            GetVideoSize(SourceVideo, out var width, out _);
            var max = width / texts.First().FontSize;
            return (
                    from t in texts
                    from st in t.Split(max)
                    select st
                    ).ToArray();
        }
        /// <summary>
        /// 合成指定时长的视频或音频
        /// </summary>
        /// <param name="lastTime">视频时长</param>
        /// <param name="mediaPath">视频路径</param>
        private void CreateMatchDurationMedia(string mediaPath, TimeSpan lastTime, string mediaExtension, Func<string> outputGetter, Func<string> sourceGetter)
        {
            var durations = new Dictionary<int, double>();
            var files = Directory.GetFiles(mediaPath).Where((file) => Path.GetExtension(file) == mediaExtension).ToArray();
            //获取视频时长
            for (int i = 0; i < files.Length; i++)
                durations[i] = GetMediaDuration(files[i]);

            //合成视频
            double totalDuration = 0;
            var r = new Random();
            var sb = new StringBuilder();

            //随机拼接视频
            do
            {
                var index = r.Next(files.Length);
                var file = files[index];
                totalDuration += durations[index];
                sb.AppendLine($"file '{file}'");
            }
            while (totalDuration < lastTime.TotalSeconds);

            //合成
            File.WriteAllText(Txt, sb.ToString());
            Command.Execute($"ffmpeg -f concat -safe 0 -i {Txt} -c copy -y {outputGetter()}");
            Command.Execute($"ffmpeg -i {sourceGetter()} -vcodec copy -acodec copy -ss 00 -to {lastTime} -y {outputGetter()}");
        }

        /// <summary>
        /// 合成视频和字幕
        /// </summary>
        /// <param name="texts"></param>
        private void CombineSubtitlesAndVedio(TextInfo[] texts)
        {
            //合成命令
            var sb = new StringBuilder();
            int i = 0;
            foreach (var text in texts)
            {
                sb.Append($"{(i == 0 ? "[0:v]" : $"[text{i - 1}]")}");
                sb.Append(text.ToString());
                sb.AppendLine($"{(i == texts.Length - 1 ? "[out];" : $"[text{i}];")}");
                i++;
            }
            sb.AppendLine("[0:v][out]overlay=(main_w-overlay_w)/2:(main_h-overlay_h)/2");

            File.WriteAllText(Txt, sb.ToString());

            //执行命令
            var cmd = $"ffmpeg -i {SourceVideo} -filter_complex_script {Txt} -y {OutputVideo}";
            Command.Execute(cmd);
        }
        /// <summary>
        /// 合成视频和音频
        /// </summary>
        /// <param name="lastTime"></param>
        private void CombineAudioAndVedio(double lastTime)
        {
            //调整视频速度
            var speed = GetMediaDuration(SourceAudio) / lastTime;
            Command.Execute($"ffmpeg -i {SourceVideo} -filter:v  \"setpts={speed}*PTS\"  {OutputVideo}");
            //合成视频和音频
            Command.Execute($"ffmpeg -i {SourceVideo} -i {SourceAudio} -c:v copy -c:a aac -strict experimental -map 0:v:0 -map 1:a:0 -y {OutputVideo}");
        }

        /// <summary>
        /// 合成指定视频
        /// </summary>
        /// <param name="videoPath">视频文件夹路径</param>
        /// <param name="text"></param>
        public void MakeVideo(string videoPath, string text, string videoFilename = "out", string bgmPath = "")
        {
            //创建临时文件夹
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);

            try
            {
                //合成视频

                // 生成音频和字幕
                Console.WriteLine("生成音频和字幕");
                var texts = CreateAudioAndSubtitles(text);
                // 记录视频持续时间
                var lastTime = texts.Last().ToTime;

                // 生成匹配时长的视频
                Console.WriteLine("生成匹配时长的视频");
                CreateMatchDurationMedia(videoPath, lastTime, VideoExtension, () => OutputVideo, () => SourceVideo);

                // 字幕断行
                Console.WriteLine("字幕断行");
                texts = BreakSubtitle(texts);
                // 合成字幕和视频
                Console.WriteLine("合成字幕和视频");
                CombineSubtitlesAndVedio(texts);

                // 合成音频和视频
                Console.WriteLine("合成音频和视频");
                CombineAudioAndVedio(lastTime.TotalSeconds);

                // 合成BGM, 如果输入的话
                //if (!string.IsNullOrWhiteSpace(bgmPath))
                //{
                //    CreateMatchDurationMedia(bgmPath, lastTime, BGMExtension, () => OutputAudio, () => SourceAudio);
                //    CombineAudio(lastTime.TotalSeconds);
                //}


                //输出视频文件
                if (File.Exists(OutputVideo))
                    File.Copy(OutputVideo, OutputPath + videoFilename + VideoExtension, true);
            }
            finally
            {
                //删除临时文件夹
                Directory.Delete(TempPath, true);
            }
        }

        private struct TextInfo
        {
            /// <summary>
            /// 描边颜色
            /// </summary>
            public string ShaderColor { get; set; }
            /// <summary>
            /// 字体颜色
            /// </summary>
            public string FontColor { get; set; }
            /// <summary>
            /// 字体大小
            /// </summary>
            public int FontSize { get; set; }
            /// <summary>
            /// 字体文件
            /// </summary>
            public string FontFile { get; set; }
            /// <summary>
            /// 文本
            /// </summary>
            public string Text { get; set; }
            /// <summary>
            /// 起始时间
            /// </summary>
            public TimeSpan FromTime { get; set; }
            /// <summary>
            /// 结束时间
            /// </summary>
            public TimeSpan ToTime { get; set; }
            /// <summary>
            /// 文字位置X
            /// </summary>
            public string X { get; set; }
            /// <summary>
            /// 文字位置Y
            /// </summary>
            public string Y { get; set; }

            public TextInfo(string text)
            {
                Text = text;
                FromTime = TimeSpan.Zero;
                ToTime = TimeSpan.Zero;
                FontColor = "white";
                ShaderColor = "black";
                FontSize = 50;
                FontFile = "C:/Windows/Fonts/msyh.ttc";
                X = "(w-tw)/2";
                Y = "(h-th)/2";
            }

            public override string ToString()
            {
                return "drawtext=" +
                $"fontcolor={FontColor}:" +
                $"fontsize={FontSize}:" +
                $"fontfile=\\'{FontFile}\\':" +
                $"shadowcolor={ShaderColor}:shadowx=2:shadowy=2:" +
                $"text='{Text}':" +
                $"x={X}:" +
                $"y={Y}:" +
                $"enable='between(t\\,{FromTime.TotalSeconds}\\,{ToTime.TotalSeconds})'"
                    ;
            }

            public IEnumerable<TextInfo> Split(int max)
            {
                var matches = Regex.Matches(Text, $@".{{0,{max}}}");
                int count = matches.Count;
                int i = 0;
                foreach (Match match in matches)
                {
                    var text = this;
                    text.Text = match.Value;
                    text.Y = $"(h-{count}*th)/2+{i}*th";
                    i++;
                    yield return text;
                }
            }
        }
    }
}
