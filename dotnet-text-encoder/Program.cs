﻿using System;
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
    [VersionOption("dotnet-tenc 0.3.0")]
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
        [Option("-e|--eol", "converting end of line(cr,crlf,lf,none: default=none)", CommandOptionType.SingleValue)]
        public Newline Newline { get; set; }
        public int OnExecute()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var fromenc = TextConverter.GetEncodingFromString(FromEncoding);
                var toenc = TextConverter.GetEncodingFromString(ToEncoding);
                using (var instm = GetInputStream(InputFile))
                using (var inbufstm = new BufferedStream(instm))
                using (var outstm = GetOutputStream(OutputFile))
                using (var outbufstm = new BufferedStream(outstm))
                {
                    TextConverter.ConvertStream(inbufstm, fromenc, outbufstm, toenc, NoPreamble, Newline);
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
    }
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var ret = CommandLineApplication.Execute<Options>(args);
            Environment.ExitCode = ret;
        }
    }
}
