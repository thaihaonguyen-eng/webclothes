using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var win1252 = Encoding.GetEncoding(1252);
        var utf8 = Encoding.UTF8;

        string rootPath = @"c:\Users\Admin\source\repos\webclothes\webclothes";
        var extensions = new[] { ".cs", ".cshtml" };
        var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();

        // Vietnamese accented characters
        string vietnameseChars = "áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđ" +
                                 "ÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴĐ";

        var mojibakeDict = new Dictionary<string, string>();

        foreach (char c in vietnameseChars)
        {
            string correctStr = c.ToString();
            byte[] utf8Bytes = utf8.GetBytes(correctStr);
            string mojibakeStr = win1252.GetString(utf8Bytes);
            
            // Only add if it actually looks different and the mojibake representation has multiple characters
            // Because UTF-8 for Vietnamese characters is 2 or 3 bytes, win1252 will interpret them as 2 or 3 distinct characters
            if (mojibakeStr != correctStr)
            {
                if (!mojibakeDict.ContainsKey(mojibakeStr))
                {
                    mojibakeDict[mojibakeStr] = correctStr;
                }
            }
        }

        // We want to sort the mojibake dictionary by length descending so we replace longer sequences first
        var sortedPairs = mojibakeDict.OrderByDescending(kv => kv.Key.Length).ToList();

        int filesChanged = 0;

        foreach (var file in files)
        {
            // Skip generated files or obj/bin
            if (file.Contains(@"\obj\") || file.Contains(@"\bin\")) continue;

            string content = File.ReadAllText(file, Encoding.UTF8);
            string originalContent = content;

            foreach (var pair in sortedPairs)
            {
                if (content.Contains(pair.Key))
                {
                    content = content.Replace(pair.Key, pair.Value);
                }
            }

            if (content != originalContent)
            {
                File.WriteAllText(file, content, Encoding.UTF8);
                Console.WriteLine($"Fixed: {file}");
                filesChanged++;
            }
        }

        Console.WriteLine($"Done. Fixed {filesChanged} files.");
    }
}
