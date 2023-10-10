using System;
using System.CodeDom;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Framework
{
    internal class TextGetter
    {
        const string s_baseUrl = "https://www.bibiqu.com/";
        static HttpClient s_cliet = new HttpClient();
        static Random s_random = new Random();

        /// <summary>
        /// 获取一个随机的书本id
        /// </summary>
        /// <returns></returns>
        private static string RandomBookId => $"{s_random.Next(100)}_{s_random.Next(20000)}";

        static TextGetter()
        {
            s_cliet.BaseAddress = new Uri(s_baseUrl);
            s_cliet.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36 Edg/110.0.1587.63");
        }

        /// <summary>
        /// 爬取指定章节
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task<string> GetText(string url)
        {
            var html = await s_cliet.GetStringAsync(url);
            var content = Regex.Match(html, @"(?<=<div class=""content"" id=""content"">)[\s\S]+?(?=</div>)").Value;
            content = content.Replace("<p>", "").Replace("</p>", "\n");
            return content;
        }

        /// <summary>
        /// 随机爬取小说
        /// </summary>
        /// <param name="minSize"></param>
        /// <returns></returns>
        public static async Task<(string bookName, string text)> GetText(int minSize = 3000,int maxSize=6000)
        {
            //获取html文件
            var html = await s_cliet.GetStringAsync(RandomBookId);

            //获取书名
            var bookName = Regex.Match(html, @"(?<=<div class=""top"">[\s\r\n]*<h1>).*?(?=</h1>)").Value;

            //获取目录
            var list = Regex.Matches(html, @"(?<=<ul class=""section-list fix"">)[\s\S]*?(?=</div>)")[1].Value;
            var hrefs = Regex.Matches(list, @"(?<=<a href="").+?(?="">)");

            //获取合适长度的文章
            var builder = new StringBuilder();
            foreach (Match href in hrefs)
            {
                builder.AppendLine(await GetText(href.Value));
                if (builder.Length > minSize)
                    break;
            }
            if (builder.Length > maxSize)
                return (bookName,builder.ToString(0,maxSize));    
            return (bookName, builder.ToString());
        }
    }
}
