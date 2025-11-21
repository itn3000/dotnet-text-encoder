using System;
using System.IO;
using System.Text;
using ConsoleAppFramework;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System.Linq;

namespace dotnet_text_encoder;

public class EncodeCommand
{
    /// <summary>
    /// Encode text from stream(file, stdin)
    /// Examples:
    /// input utf-8,output shift_jis(cp932) by name:
    ///   dotnet tenc -f utf-8 -t shift_jis -i utf8.txt -o sjis.txt
    /// input utf-8,output shift_jis(cp932) by name:
    ///   dotnet tenc -f utf-8 -t shift_jis -i utf8.txt -o sjis.txt
    /// input utf-8,output utf-8 without BOM(BOM added by default)
    ///   dotnet tenc -f utf-8 -t shift_jis -i utf8.txt -o sjis.txt -n
    /// input utf-8,output utf-8 convert eol to LF(and no BOM)
    ///   dotnet tenc -f utf-8 -t utf-8 -e lf -i utf8.txt -o utf8-lf.txt -n
    /// </summary>
    /// <param name="from">-f, input file encoding(default: UTF-8)</param>
    /// <param name="to">-t, output file encoding(default: UTF-8)</param>
    /// <param name="input">-i, input file path(default: standard input)</param>
    /// <param name="output">-o, output file path(default: standard output)</param>
    /// <param name="preamble">-p, enable output preamble(=BOM) if exists</param>
    /// <param name="eol">-e, converting end of line(cr,crlf,lf,none: default=none)</param>
    [Command("")]
    public int EncodeText(string? from = null, string? to = null,
        string? input = null, string? output = null,
        bool preamble = false,
        string? eol = null
        )
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var fromenc = TextConverter.GetEncodingFromString(from);
            var toenc = TextConverter.GetEncodingFromString(to);
            var nl = ConvertNewline(eol);
            using (var instm = GetInputStream(input))
            using (var inbufstm = new BufferedStream(instm))
            using (var outstm = GetOutputStream(output))
            using (var outbufstm = new BufferedStream(outstm))
            {
                TextConverter.ConvertStream(inbufstm, fromenc, outbufstm, toenc, !preamble, nl);
            }
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"converting error:{e}");
            return 2;
        }
    }
    Newline ConvertNewline(string? nlString)
    {
        return TextConverter.ParseNewline(nlString ?? "");
    }
    static Stream GetInputStream(string? filePath)
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
    static Stream GetOutputStream(string? filePath)
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