using System;
using System.IO;
using System.Text;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace dotnet_text_encoder
{
    public enum Newline
    {
        None,
        Cr,
        Crlf,
        Lf,
    }
    [Command(ExtendedHelpText = @"
changing text encoding
Examples:
input utf-8,output shift_jis(cp932) by name:
  dotnet tenc -f utf-8 -t shift_jis -i utf8.txt -o sjis.txt
input utf-8,output shift_jis(cp932) by code page
  dotnet tenc -f 65001 -t 932 -i utf8.txt -o sjis.txt
input utf-8,output utf-8 without BOM(BOM added by default)
  dotnet tenc -f utf-8 -t shift_jis -i utf8.txt -o sjis.txt -n
")]
    [Subcommand(typeof(EncodingInfoGetter))]
    [VersionOption("dotnet-tenc 0.2.1")]
    class Options
    {
        [Option("-f|--from", "input file encoding(default: UTF-8)", CommandOptionType.SingleValue)]
        public string FromEncoding { get; set; }
        [Option("-t|--to", "output file encoding(default: UTF-8)", CommandOptionType.SingleValue)]
        public string ToEncoding { get; set; }
        [Option("-i|--input", "input file path(default: standard input)", CommandOptionType.SingleValue)]
        public string InputFile { get; set; }
        [Option("-o|--output", "output file path(default: standard output)", CommandOptionType.SingleValue)]
        public string OutputFile { get; set; }
        [Option("-n|--no-preamble", "disable output preamble(=BOM) if exists", CommandOptionType.NoValue)]
        public bool NoPreamble { get; set; }
        [Option("-e|--eol", "converting end of line(cr,crlf,lf)", CommandOptionType.SingleValue)]
        public Newline Newline { get; set; }
        public int OnExecute()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var fromenc = TextConverter.GetEncodingFromString(FromEncoding);
                var toenc = TextConverter.GetEncodingFromString(ToEncoding);
                using (var instm = GetInputStream(InputFile))
                using (var outstm = GetOutputStream(OutputFile))
                {
                    TextConverter.ConvertStream(instm, fromenc, outstm, toenc, NoPreamble, Newline);
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"converting error:{e}");
                return 2;
            }
        }
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
        // [Usage]
        // public static Example[] Usage => new Example[]
        // {
        //     new Example("basic usage", new UnParserSettings(), new Options()
        //     {
        //         InputFile = "[input file]",
        //         OutputFile = "[output file]",
        //         FromEncoding = "[from encoding]",
        //         ToEncoding = "[to encoding]"
        //     }),
        //     new Example("read standard input", new UnParserSettings(), new Options()
        //     {
        //         OutputFile = "[output file]",
        //         FromEncoding = "[from encoding]",
        //         ToEncoding = "[to encoding]"
        //     }),
        //     new Example("disable preamble(BOM)", new UnParserSettings(), new Options()
        //     {
        //         InputFile = "[input file]",
        //         OutputFile = "[output file]",
        //         FromEncoding = "[from encoding]",
        //         ToEncoding = "utf-8",
        //         NoPreamble = true
        //     }),
        // };
    }
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var ret = CommandLineApplication.Execute<Options>(args);
            Environment.ExitCode = ret;

            // var res = Parser.Default.ParseArguments<Options, EncodingTester>(args)
            //     .WithParsed<Options>(opt =>
            //     {
            //         try
            //         {
            //             Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //             var fromenc = TextConverter.GetEncodingFromString(opt.FromEncoding);
            //             var toenc = TextConverter.GetEncodingFromString(opt.ToEncoding);
            //             using (var instm = GetInputStream(opt.InputFile))
            //             using (var outstm = GetOutputStream(opt.OutputFile))
            //             {
            //                 TextConverter.ConvertStream(instm, fromenc, outstm, toenc, opt.NoPreamble);
            //             }
            //         }
            //         catch (Exception e)
            //         {
            //             Console.Error.WriteLine($"converting error:{e}");
            //             Environment.ExitCode = 2;
            //         }
            //     })
            //     .WithParsed<EncodingTester>(tester =>
            //     {
            //         Console.WriteLine($"Name,CodePage,Found,DisplayName");
            //         foreach (var result in tester.GetTestResults())
            //         {
            //             Console.WriteLine($"{result.Name},{result.CodePage},{result.Found},{result.DisplayName}");
            //         }
            //     })
            //     ;
            // if (res.Tag == ParserResultType.NotParsed)
            // {
            //     var notParsed = (NotParsed<object>)res;
            //     if (!notParsed.Errors.Any(x => x.Tag == ErrorType.HelpRequestedError) &&
            //         !notParsed.Errors.Any(x => x.Tag == ErrorType.VersionRequestedError))
            //     {
            //         Environment.ExitCode = 1;
            //     }
            //     // HelpText.AutoBuild(res, null, null)
            //     //     .AddPreOptionsLine(HelpText.RenderParsingErrorsText(res, er => er.)
            // }
        }
    }
}
