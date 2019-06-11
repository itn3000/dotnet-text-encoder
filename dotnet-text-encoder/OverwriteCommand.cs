using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace dotnet_text_encoder
{
    [Command("overwrite", "ow", Description = "convert files with overwrite mode", ExtendedHelpText = @"
Examples:
all files that have '.txt' extension are targeted.
  dotnet tenc ow -f utf-8 -t utf-8 -e lf **/*.txt
all files that have '.txt' extension under the 'targetdir' are targeted.
  dotnet tenc ow -f utf-8 -t utf-8 -e lf **/*.txt -b targetdir
all files that have '.txt' extension are targeted,excluding under the 'sub' directory
  dotnet tenc ow -f utf-8 -t utf-8 -e lf **/*.txt -x sub/**/*
")]
    [HelpOption]
    class OverwriteCommand
    {
        [Argument(0, "Targets", "target files, you can use globbing(*.txt, **/*.cs)")]
        public string[] Targets { get; set; }
        [Option("-f|--from", "input file encoding(default: UTF-8)", CommandOptionType.SingleValue)]
        public string FromEncoding { get; set; }
        [Option("-t|--to", "output file encoding(default: UTF-8)", CommandOptionType.SingleValue)]
        public string ToEncoding { get; set; }
        [Option("-b|--base", "search base directory(default: current directory)", CommandOptionType.SingleValue)]
        public string BaseDirectory { get; set; }
        [Option("-i|--ignore-case", "search file with case insensitive", CommandOptionType.NoValue)]
        public bool IgnoreCase { get; set; }
        [Option("-n|--no-preamble", "disable output preamble(=BOM) if exists", CommandOptionType.NoValue)]
        public bool NoPreamble { get; set; }
        [Option("-e|--eol", "converting end of line(cr,crlf,lf,none: default=none)", CommandOptionType.SingleValue)]
        public Newline Newline { get; set; }
        [Option("-x|--exclude", "file exclude pattern, you can use globbing", CommandOptionType.MultipleValue)]
        public string[] Exclude { get; set; }
        public OverwriteCommand(ILoggerFactory loggerFactory, IConsole console)
        {
            _Logger = loggerFactory.CreateLogger<OverwriteCommand>();
            _Console = console;
        }
        public OverwriteCommand() : this(NullLoggerFactory.Instance, McMaster.Extensions.CommandLineUtils.PhysicalConsole.Singleton)
        {
        }
        IConsole _Console;
        ILogger _Logger;
        public int OnExecute()
        {
            try
            {
                if (Targets == null || Targets.Length == 0)
                {
                    _Console.Error.WriteLine("one or more files must be specified");
                    return 1;
                }
                var basedir = !string.IsNullOrEmpty(BaseDirectory) ? BaseDirectory : Directory.GetCurrentDirectory();
                var fromenc = TextConverter.GetEncodingFromString(FromEncoding);
                var toenc = TextConverter.GetEncodingFromString(ToEncoding);
                var matcher = new Matcher(StringComparison.CurrentCultureIgnoreCase);
                matcher.AddIncludePatterns(Targets);
                if(Exclude != null && Exclude.Length != 0)
                {
                    matcher.AddExcludePatterns(Exclude);
                }
                var baseDirInfo = new DirectoryInfoWrapper(new DirectoryInfo(basedir));
                var result = matcher.Execute(baseDirInfo);
                if (!result.HasMatches)
                {
                    _Console.Error.WriteLine("no file was matched");
                    return 3;
                }
                _Logger.LogDebug("converting {0} to {1}, newline = {2}, no preamble = {3}", fromenc.WebName, toenc.WebName, Newline, NoPreamble);
                foreach (var fpath in result.Files.Select(x => x.Path))
                {
                    DoEncoding(Path.Combine(baseDirInfo.FullName, fpath), fromenc, toenc);
                }
                return 0;
            }
            catch (Exception e)
            {
                _Console.Error.WriteLine("converting file error:{0}", e);
                return 2;
            }
        }
        void DoEncoding(string targetFilePath, Encoding fromenc, Encoding toenc)
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
                    _Logger.LogDebug("processing {0}", targetFilePath);
                    TextConverter.ConvertStream(inbufstm, fromenc, outbufstm, toenc, NoPreamble, Newline);
                }
                File.Move(targetFilePath, bakFilePath);
                File.Move(tmpFilePath, targetFilePath);
                File.Delete(bakFilePath);
            }
            catch (Exception e)
            {
                _Logger.LogWarning(e, "converting text error({0})", targetFilePath);
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
                    _Logger.LogWarning(e, "failed to delete tmpfile({0})", tmpFilePath);
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
                    _Logger.LogWarning(e, "failed to delete tmpfile({0}.tmp)", tmpFilePath);
                }
            }
        }
    }

}