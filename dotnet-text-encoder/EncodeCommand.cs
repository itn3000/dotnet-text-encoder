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
    /// 
    /// </summary>
    /// <param name="from">-f, input file encoding(default: UTF-8)</param>
    /// <param name="to">-t, output file encoding(default: UTF-8)</param>
    /// <param name="input">-i, input file path(default: standard input)</param>
    /// <param name="output">-o, output file path(default: standard output)</param>
    /// <param name="baseDirectory">-b|--base-directory, search base directory(default: current directory)</param>
    /// <param name="ignoreCase">-i, search file with case insensitive</param>
    /// <param name="preamble">-p, enable output preamble(=BOM) if exists</param>
    /// <param name="eol">-e, converting end of line(cr,crlf,lf,none: default=none)</param>
    /// <param name="dryRun">do not convert file</param>
    /// <param name="exclude">-x, file exclude pattern, you can use globbing</param>
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
    /// <summary>
    /// convert multiple files encoding
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
    public int Overwrite([Argument] string[] targets, string? from = null, string? to = null,
        string? baseDirectory = null, bool ignoreCase = false,
        bool preamble = false,
        string? eol = null,
        bool dryRun = false,
        string[]? exclude = null)
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