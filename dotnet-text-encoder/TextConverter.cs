using System;
using System.Text;
using System.IO;
using System.Buffers;
using System.Threading.Tasks;

namespace dotnet_text_encoder
{
    static class TextConverter
    {
        public static Encoding GetEncodingFromString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return Encoding.UTF8;
            }
            else if (int.TryParse(str, out var cp))
            {
                return Encoding.GetEncoding(cp);
            }
            else
            {
                return Encoding.GetEncoding(str);
            }
        }
        static readonly byte[] Cr = new byte[] { CrValue };
        static readonly byte[] Lf = new byte[] { LfValue };
        static readonly byte[] CrLf = new byte[] { CrValue, LfValue };
        const byte CrValue = 0x0d;
        const byte LfValue = 0x0a;
        public static void ConvertStream(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding, bool noPreamble, Newline nl = Newline.None)
        {
            using (var sr = new StreamReader(input, inputEncoding))
            {
                var buf = ArrayPool<char>.Shared.Rent(4096);
                bool first = true;
                var wbuf = ArrayPool<byte>.Shared.Rent(4096);
                bool prevcr = false;
                try
                {
                    while (true)
                    {
                        var charread = sr.Read(buf, 0, buf.Length);
                        if (charread == 0)
                        {
                            break;
                        }
                        if (first && !noPreamble)
                        {
                            var pre = outputEncoding.GetPreamble();
                            if (pre != null && pre.Length != 0)
                            {
                                output.Write(pre, 0, pre.Length);
                            }
                        }
                        first = false;
                        prevcr = WriteBytesToStream(output, buf.AsSpan(0, charread), nl, prevcr, outputEncoding, ref wbuf);
                    }
                    if (prevcr)
                    {
                        // treat last cr
                        WriteNewline(output, nl, Cr.AsSpan());
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(buf);
                    ArrayPool<byte>.Shared.Return(wbuf);
                }
            }
        }
        static bool WriteBytesToStream(Stream output, ReadOnlySpan<char> input, Newline nl, bool prevcr, Encoding outputEncoding, ref byte[] wbuf)
        {
            int off = 0;
            Span<byte> wspan = wbuf.AsSpan();
            for (int i = 0; i < input.Length; i++)
            {
                if (prevcr)
                {
                    if (input[i] == LfValue)
                    {
                        // CRLF has come
                        WriteNewline(output, nl, CrLf.AsSpan());
                        off = i + 1;
                        prevcr = false;
                        continue;
                    }
                    else
                    {
                        // single CR has come
                        WriteNewline(output, nl, Cr.AsSpan());
                        prevcr = false;
                    }
                }
                if (input[i] == CrValue)
                {
                    // CR has come, but cannot decide to CRLF or single CR,
                    var sp = input.Slice(off, i - off);
                    wspan = EncodeAndWriteStream(sp, output, wspan, nl, outputEncoding, ref wbuf);
                    off = i + 1;
                    prevcr = true;
                }
                else if (input[i] == LfValue)
                {
                    // single LF has come
                    var sp = input.Slice(off, i - off);
                    wspan = EncodeAndWriteStream(sp, output, wspan, nl, outputEncoding, ref wbuf);
                    WriteNewline(output, nl, Lf.AsSpan());
                    off = i + 1;
                }
            }
            // write remaining buffer
            if (off < input.Length)
            {
                var sp = input.Slice(off);
                EncodeAndWriteStream(sp, output, wspan, nl, outputEncoding, ref wbuf);
            }
            return prevcr;
        }
        static Span<byte> EncodeAndWriteStream(ReadOnlySpan<char> sp, Stream output, Span<byte> wspan, Newline nl, Encoding outputEncoding, ref byte[] wbuf)
        {
            var wlen = outputEncoding.GetByteCount(sp);
            if (wlen > wspan.Length)
            {
                wspan = Span<byte>.Empty;
                ArrayPool<byte>.Shared.Return(wbuf);
                wbuf = ArrayPool<byte>.Shared.Rent(wlen);
                wspan = wbuf.AsSpan();
            }
            outputEncoding.GetBytes(sp, wspan);
            var tmp = wspan.Slice(0, wlen);
            output.Write(tmp);
            return wspan;
        }
        static void WriteNewline(Stream stm, Newline nl, ReadOnlySpan<byte> defaultValue)
        {
            switch (nl)
            {
                case Newline.Cr:
                    stm.Write(Cr.AsSpan());
                    break;
                case Newline.Lf:
                    stm.Write(Lf.AsSpan());
                    break;
                case Newline.Crlf:
                    stm.Write(CrLf.AsSpan());
                    break;
                default:
                    stm.Write(defaultValue);
                    break;
            }
        }
    }
}