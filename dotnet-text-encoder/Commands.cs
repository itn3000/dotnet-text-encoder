using ConsoleAppFramework;
namespace dotnet_text_encoder;

class Commands
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
        return new EncodeCommand().EncodeText(from, to, input, output, preamble, eol);
    }
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
        string[]? exclude = null, [Argument]params string[] targets)
    {
        return new OverwriteCommand().Overwrite(from, to, baseDirectory, ignoreCase, preamble, eol, dryRun, exclude, targets);
    }
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
        return new EncodingInfoGetter().GetEncodingInfo(name, codepage, showFault);
    }
}