using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class ChineseSentenceTokenizer
{
    private static readonly char[] s_punctuations = new char[]
    {
        '。', '？', '！', '，', '；', '：', '“', '”', '‘', '’', '（', '）', '【', '】', '『', '』',
        '—', '…', '-', '《', '》', '〈', '〉', '｛', '｝', '「', '」', '＜', '＞', '〔', '〕',
        '.', '?', '!', ',', ';', ':', '\'', '\"', '(', ')', '[', ']', '{', '}', '<', '>',
        '-', '|', '/', '\\', '@', '#', '$', '%', '^', '&', '*', '+', '=', '~', '`', '_',
        '、', '～',' ','\r','\n'
    };

    public static IEnumerable<string> Tokenize(string text)
    {
        string s="";
        foreach (var i in s_punctuations)
            s += i;
        var matches = Regex.Split(text, s);
        return matches;
    }
    public static IEnumerable<string> Tokenize1(string text)
    {
        StringBuilder sentence = new StringBuilder();
        foreach (var value in text)
        {
            if (s_punctuations.Contains(value))
            {
                if (sentence.Length > 0)
                {
                    yield return sentence.ToString().Trim();
                    sentence.Clear();
                }
            }
            else
            {
                sentence.Append(value);
            }
        }
        if (sentence.Length > 0)
        {
            yield return sentence.ToString().Trim();
        }

    }
}
