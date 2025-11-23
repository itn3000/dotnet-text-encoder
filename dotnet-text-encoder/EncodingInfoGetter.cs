using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ConsoleAppFramework;

namespace dotnet_text_encoder
{
    class EncodingTestResult
    {
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public int CodePage { get; set; }
        public bool Found { get; set; }
        public byte[]? Preamble { get; set; }
    }
    class EncodingInfoGetter
    {
        /// <summary>
        /// getting encoding info and output by CSV format
        /// 
        /// Examples:
        /// try get info by name:
        ///     dotnet tenc getinfo -n shift_jis
        /// try get info by codepage number(single)
        ///     dotnet tenc getinfo -c 932
        /// try get info by codepage number(range)
        ///     dotnet tenc getinfo -c 0-1000
        /// </summary>
        /// <param name="name">-n, encoding names</param>
        /// <param name="codepage">-c, code page range(number separated by '-', number is must be 0 - 65535)</param>
        /// <param name="showFault">-s, show fault result</param>
        /// <returns></returns>
        [Command("getinfo")]
        public int GetEncodingInfo(string[]? name = null, string[]? codepage = null, bool showFault = false)
        {
            Console.WriteLine($"Name,CodePage,Found,DisplayName,Preamble(hex)");
            foreach (var result in GetTestResults(name, codepage))
            {
                if (!showFault && !result.Found)
                {
                    continue;
                }
                var preamble = result.Preamble != null ? string.Join("", result.Preamble.Select(x => x.ToString("x2"))) : "";
                Console.WriteLine($"{result.Name},{result.CodePage},{result.Found},{result.DisplayName},{preamble}");
            }
            return 0;
        }
        public IEnumerable<EncodingTestResult> GetTestResults(string[]? names, string[]? codeRanges)
        {
            if (names != null)
            {
                foreach (var name in names)
                {
                    yield return DoTestByName(name);
                }
            }
            if (codeRanges != null)
            {
                foreach (var rangeString in codeRanges)
                {
                    if (string.IsNullOrEmpty(rangeString))
                    {
                        continue;
                    }
                    var range = rangeString.Split('-', 2);
                    if (range.Length == 1)
                    {
                        if (int.TryParse(range[0].Trim(), out var cp))
                        {
                            yield return DoTestByCodePage(cp);
                        }
                    }
                    else
                    {
                        if (ushort.TryParse(range[0].Trim(), out var cpstart) && ushort.TryParse(range[1].Trim(), out var cpend))
                        {
                            if (cpstart > cpend)
                            {
                                continue;
                            }
                            foreach (var cp in Enumerable.Range(cpstart, cpend - cpstart + 1))
                            {
                                yield return DoTestByCodePage(cp);
                            }
                        }
                    }
                }
            }
        }
        EncodingTestResult DoTestByName(string name)
        {
            try
            {
                var enc = Encoding.GetEncoding(name);
                return new EncodingTestResult()
                {
                    Name = name,
                    CodePage = enc.CodePage,
                    Found = true,
                    DisplayName = enc.EncodingName,
                    Preamble = enc.GetPreamble()
                };
            }
            catch (Exception)
            {
                return new EncodingTestResult()
                {
                    Name = name,
                    Found = false
                };
            }
        }
        EncodingTestResult DoTestByCodePage(int cp)
        {
            try
            {
                var enc = Encoding.GetEncoding(cp);
                return new EncodingTestResult()
                {
                    Name = enc.WebName,
                    CodePage = cp,
                    Found = true,
                    DisplayName = enc.EncodingName,
                    Preamble = enc.GetPreamble()
                };
            }
            catch
            {
                return new EncodingTestResult()
                {
                    CodePage = cp,
                    Found = false
                };
            }
        }
    }
}