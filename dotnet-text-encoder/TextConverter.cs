using System;
using System.Text;
using System.IO;
using System.Buffers;
using System.Threading.Tasks;
using System.IO.Pipelines;

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
        static readonly byte[] Cr = new byte[] { 0x0a };
        static readonly byte[] Lf = new byte[] { 0x0d };
        static readonly byte[] CrLf = new byte[] { 0x0a, 0x0d };
        public static void ConvertStream(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding, bool noPreamble, Newline nl = Newline.None)
        {
            using (var sr = new StreamReader(input, inputEncoding))
            {
                var buf = new char[4096];
                bool first = true;
                var wbuf = ArrayPool<byte>.Shared.Rent(4096);
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
                    var wlen = outputEncoding.GetByteCount(buf, 0, charread);
                    if (wlen > wbuf.Length)
                    {
                        ArrayPool<byte>.Shared.Return(wbuf);
                        wbuf = ArrayPool<byte>.Shared.Rent(wlen);
                    }
                    outputEncoding.GetBytes(buf, 0, charread, wbuf, 0);
                    var wsp = wbuf.AsSpan(wlen);
                    while (true)
                    {
                        var nloff = FindNewlineOffset(wsp, nl);
                        if (nloff < 0)
                        {
                            output.Write(wsp);
                            break;
                        }
                        output.Write(wsp.Slice(0, nloff));
                        switch (nl)
                        {
                            case Newline.Cr:
                                output.Write(Cr.AsSpan());
                                break;
                            case Newline.Lf:
                                output.Write(Lf.AsSpan());
                                break;
                            case Newline.Crlf:
                                output.Write(CrLf.AsSpan());
                                break;
                            default:
                                break;
                        }
                    }
                    output.Write(wbuf, 0, wlen);
                }
            }
        }
        static void WriteNewline(Stream stm, Newline nl)
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
                    break;
            }
        }
        static int FindNewlineOffset(ReadOnlySpan<byte> sp, Newline nl)
        {
            Span<byte> crlf = stackalloc byte[2];
            crlf[0] = 0x0a;
            crlf[1] = 0x0d;
            switch (nl)
            {
                case Newline.Crlf:
                    return sp.IndexOfAny((byte)0x0a, (byte)0x0d);
                case Newline.Cr:
                case Newline.Lf:
                    var idx = sp.IndexOf(crlf);
                    if (idx == -1)
                    {
                        return sp.IndexOf((byte)0x0d);
                    }
                    else
                    {
                        return idx;
                    }
                default:
                    return -1;
            }
        }
        public static async Task ConvertStreamAsync(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding, bool noPreamble, Newline nl = Newline.None)
        {
            var pipe = new Pipe();
            using (var sr = new StreamReader(input, inputEncoding))
            {
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        var buf = new char[4096];
                        byte[] wbuf = ArrayPool<byte>.Shared.Rent(8192);
                        Action<byte[], int, PipeWriter> wfunc = (data, length, pw) =>
                        {
                            var sp = pw.GetSpan(length);
                            data.AsSpan(0, length).CopyTo(sp);
                            pw.Advance(length);
                        };
                        while (true)
                        {
                            var charread = sr.Read(buf, 0, buf.Length);
                            if (charread == 0)
                            {
                                break;
                            }
                            var wlen = outputEncoding.GetByteCount(buf, 0, charread);
                            if (wlen > wbuf.Length)
                            {
                                ArrayPool<byte>.Shared.Return(wbuf);
                                wbuf = ArrayPool<byte>.Shared.Rent(wlen);
                            }
                            wlen = outputEncoding.GetBytes(buf, 0, charread, wbuf, 0);
                            wfunc(wbuf, wlen, pipe.Writer);
                            await pipe.Writer.FlushAsync().ConfigureAwait(false);
                        }
                        pipe.Writer.Complete();
                    }),
                    Task.Run(async () =>
                    {
                        Action<Stream, Memory<byte>, Newline, bool> wfunc = (stm, mem, nlid, prevcr) =>
                        {
                            var rsp = mem.Span;
                            for (int i = 0; i < rsp.Length - 1; i++)
                            {
                                if(rsp[i] == 0x0a)
                                {
                                    if(rsp[i + 1] == 0x0d)
                                    {
                                        if(nlid == Newline.Cr)
                                        {
                                            stm.Write(Cr.AsSpan());
                                        }
                                        else if(nlid == Newline.Lf)
                                        {
                                            stm.Write(Lf.AsSpan());
                                        }
                                        i++;
                                    }
                                }
                                else if(rsp[i] == 0x0d)
                                {
                                    
                                }
                            }
                        };
                        while (true)
                        {
                            var readResult = await pipe.Reader.ReadAsync().ConfigureAwait(false);
                            if (!readResult.Buffer.IsEmpty)
                            {
                                foreach (var rbuf in readResult.Buffer)
                                {
                                    if (nl == Newline.None)
                                    {
                                        output.Write(rbuf.Span);
                                    }
                                    else
                                    {
                                        int noff = 0;
                                    }
                                }
                            }
                        }
                    })
                );
            }
        }
    }
}