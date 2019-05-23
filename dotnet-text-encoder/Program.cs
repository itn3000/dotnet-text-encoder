using System;
using System.IO;
using System.Text;
using CommandLine;
using CommandLine.Text;
using System.Linq;

namespace dotnet_text_encoder
{
    class Options
    {
        [Option('f', "from", HelpText = "input file encoding(default: UTF-8)")]
        public string FromEncoding { get; set; }
        [Option('t', "to", HelpText = "output file encoding(default: UTF-8)")]
        public string ToEncoding { get; set; }
        [Option('i', "input", HelpText = "input file path(default: standard input)")]
        public string InputFile { get; set; }
        [Option('o', "output", HelpText = "output file path(default: standard output)")]
        public string OutputFile { get; set; }
        [Option('n', "no-preamble", HelpText = "disable output preamble(=BOM) if exists")]
        public bool NoPreamble { get; set; }
        [Usage]
        public static Example[] Usage => new Example[]
        {
            new Example("basic usage", new UnParserSettings(), new Options()
            {
                InputFile = "[input file]",
                OutputFile = "[output file]",
                FromEncoding = "[from encoding]",
                ToEncoding = "[to encoding]"
            }),
            new Example("read standard input", new UnParserSettings(), new Options()
            {
                OutputFile = "[output file]",
                FromEncoding = "[from encoding]",
                ToEncoding = "[to encoding]"
            }),
            new Example("disable preamble(BOM)", new UnParserSettings(), new Options()
            {
                InputFile = "[input file]",
                OutputFile = "[output file]",
                FromEncoding = "[from encoding]",
                ToEncoding = "utf-8",
                NoPreamble = true
            }),
        };
    }
    class Program
    {
        static Stream GetInputStream(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Console.OpenStandardInput();
            }
            else
            {
                return File.OpenRead(filePath);
            }
        }
        static Stream GetOutputStream(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Console.OpenStandardOutput();
            }
            else
            {
                return File.Create(filePath);
            }
        }
        static Encoding GetEncodingFromString(string str)
        {
            if(string.IsNullOrEmpty(str))
            {
                return Encoding.UTF8;
            }
            else if(int.TryParse(str, out var cp))
            {
                return Encoding.GetEncoding(cp);
            }
            else
            {
                return Encoding.GetEncoding(str);
            }
        }
        static void Main(string[] args)
        {
            var res = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opt =>
                {
                    try
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        var fromenc = GetEncodingFromString(opt.FromEncoding);
                        var toenc = GetEncodingFromString(opt.ToEncoding);
                        using (var instm = GetInputStream(opt.InputFile))
                        using (var outstm = GetOutputStream(opt.OutputFile))
                        {
                            TextConverter.ConvertStream(instm, fromenc, outstm, toenc, opt.NoPreamble);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"converting error:{e}");
                        Environment.ExitCode = 2;
                    }
                })
                ;
            if (res.Tag == ParserResultType.NotParsed)
            {
                var notParsed = (NotParsed<Options>)res;
                if (!notParsed.Errors.Any(x => x.Tag == ErrorType.HelpRequestedError) &&
                    !notParsed.Errors.Any(x => x.Tag == ErrorType.VersionRequestedError))
                {
                    Environment.ExitCode = 1;
                }
                // HelpText.AutoBuild(res, null, null)
                //     .AddPreOptionsLine(HelpText.RenderParsingErrorsText(res, er => er.)
            }
        }
    }
}
