using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleAppFramework;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace dotnet_text_encoder
{
    public class OverwriteCommand
    {
        /// <summary>
        /// convert files with overwrite mode
        /// Examples:
        /// all files that have '.txt' extension are targeted.
        ///   dotnet tenc ow -f utf-8 -t utf-8 -e lf **/*.txt
        /// all files that have '.txt' extension under the 'targetdir' are targeted.
        ///   dotnet tenc ow -f utf-8 -t utf-8 -e lf **/*.txt -b targetdir
        /// all files that have '.txt' extension are targeted,excluding under the 'sub' directory
        ///   dotnet tenc ow -f utf-8 -t utf-8 -e lf **/*.txt -x sub/**/*
        /// </summary>
        /// <param name="targets">target files, you can use globbing(*.txt, **/*.cs)</param>
        /// <param name="from">-f, input file encoding(default: UTF-8)</param>
        /// <param name="to">-t, output file encoding(default: UTF-8)</param>
        /// <param name="baseDirectory">-b|--base, search base directory(default: current directory)</param>
        /// <param name="ignoreCase">-i, search file with case insensitive</param>
        /// <param name="preamble">-p, enable output preamble(=BOM) if exists</param>
        /// <param name="eol">-e, converting end of line(cr,crlf,lf,none: default=none)</param>
        /// <param name="dryRun">do not convert file</param>
        /// <param name="exclude">-x, file exclude pattern, you can use globbing</param>
        /// <returns>0 if success</returns>
        [Command("ow")]
        public int Overwrite(string? from = null, string? to = null,
            string? baseDirectory = null, bool ignoreCase = false,
            bool preamble = false,
            string? eol = null,
            bool dryRun = false,
            string[]? exclude = null, [Argument]params string[]? targets)
        {
            try
            {
                if (targets == null || targets.Length == 0)
                {
                    Console.Error.WriteLine("one or more files must be specified");
                    return 1;
                }
                var basedir = !string.IsNullOrEmpty(baseDirectory) ? baseDirectory : Directory.GetCurrentDirectory();
                var fromenc = TextConverter.GetEncodingFromString(from);
                var toenc = TextConverter.GetEncodingFromString(to);
                var matcher = new Matcher(StringComparison.CurrentCultureIgnoreCase);
                matcher.AddIncludePatterns(targets);
                if (exclude != null && exclude.Length != 0)
                {
                    matcher.AddExcludePatterns(exclude);
                }
                var baseDirInfo = new DirectoryInfoWrapper(new DirectoryInfo(basedir));
                var result = matcher.Execute(baseDirInfo);
                if (!result.HasMatches)
                {
                    Console.Error.WriteLine("no file was matched");
                    return 3;
                }
                // _Logger.LogDebug("converting {0} to {1}, newline = {2}, no preamble = {3}", fromenc.WebName, toenc.WebName, Newline, NoPreamble);
                foreach (var fpath in result.Files.Select(x => x.Path))
                {
                    if (dryRun)
                    {
                        Console.WriteLine($"replacing file(dryrun): {fpath}");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"replacing file: {fpath}");
                    }
                    DoEncoding(Path.Combine(baseDirInfo.FullName, fpath), fromenc, toenc, preamble, ConvertNewline(eol));
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("converting file error:{0}", e);
                return 2;
            }
        }
        void DoEncoding(string targetFilePath, Encoding fromenc, Encoding toenc, bool preamble, Newline nl)
        {
            var tmpFilePath = $"{targetFilePath}.tmp";
            var bakFilePath = $"{targetFilePath}.bak";
            try
            {
                using (var instm = File.OpenRead(targetFilePath))
                using (var inbufstm = new BufferedStream(instm))
                using (var outstm = File.Create(tmpFilePath))
                using (var outbufstm = new BufferedStream(outstm))
                {
                    TextConverter.ConvertStream(inbufstm, fromenc, outbufstm, toenc, !preamble, nl);
                }
                File.Move(targetFilePath, bakFilePath);
                File.Move(tmpFilePath, targetFilePath);
                File.Delete(bakFilePath);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"converting text error({targetFilePath}): {e}");
            }
            finally
            {
                try
                {
                    if (File.Exists(tmpFilePath))
                    {
                        File.Delete(tmpFilePath);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"failed to delete tmpfile({tmpFilePath}): {e}");
                }
                try
                {
                    if (File.Exists(bakFilePath))
                    {
                        if (File.Exists(targetFilePath))
                        {
                            File.Delete(bakFilePath);
                        }
                        else
                        {
                            File.Move(bakFilePath, targetFilePath);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("failed to delete tmpfile({0}.tmp): {1}", tmpFilePath, e);
                }
            }
        }
        Newline ConvertNewline(string? nlString)
        {
            return TextConverter.ParseNewline(nlString ?? "");
        }
    }

}