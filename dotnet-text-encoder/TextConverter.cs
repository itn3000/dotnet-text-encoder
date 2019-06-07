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
        static readonly byte[] Cr = new byte[] { CrValue };
        static readonly byte[] Lf = new byte[] { LfValue };
        static readonly byte[] CrLf = new byte[] { CrValue, LfValue };
        const byte CrValue = 0x0d;
        const byte LfValue = 0x0a;
        public static void ConvertStream(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding, bool noPreamble, Newline nl = Newline.None)
        {
            using (var sr = new StreamReader(input, inputEncoding))
            {
                var buf = new char[4096];
                bool first = true;
                var wbuf = ArrayPool<byte>.Shared.Rent(4096);
                bool prevcr = false;
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
                    prevcr = WriteBytesToStream(output, wbuf.AsMemory(0, wlen), nl, prevcr);
                }
                if(prevcr)
                {
                    // treat last cr
                    WriteNewline(output, nl, Cr.AsSpan());
                }
            }
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
        static bool WriteBytesToStream(Stream stm, ReadOnlyMemory<byte> mem, Newline nlid, bool prevcr)
        {
            var rsp = mem.Span;
            for (int i = 0; i < rsp.Length; i++)
            {
                if (rsp[i] == CrValue)
                {
                    // CR has come, but cannot decide whether CRLR or CR + [another char]
                    if (i != 0)
                    {
                        stm.Write(rsp.Slice(0, i));
                    }
                    rsp = rsp.Slice(i + 1);
                    i = -1;
                    prevcr = true;
                }
                else if (rsp[i] == LfValue)
                {
                    if (prevcr)
                    {
                        if (i != 0)
                        {
                            stm.Write(rsp.Slice(0, i));
                        }
                        // CRLF has come
                        WriteNewline(stm, nlid, CrLf.AsSpan());
                        rsp = rsp.Slice(i + 1);
                        i = -1;
                        prevcr = false;
                    }
                    else
                    {
                        // single LF has come
                        if (i != 0)
                        {
                            stm.Write(rsp.Slice(0, i));
                        }
                        WriteNewline(stm, nlid, Lf.AsSpan());
                        rsp = rsp.Slice(i + 1);
                        i = -1;
                    }
                }
                else
                {
                    if (prevcr)
                    {
                        // single CR has come
                        WriteNewline(stm, nlid, Cr.AsSpan());
                        prevcr = false;
                        i--;
                    }
                }
            }
            // write remaining data
            if (!rsp.IsEmpty)
            {
                stm.Write(rsp);
            }
            return prevcr;
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
                        bool prevcr = false;
                        bool first = true;
                        while (true)
                        {
                            var readResult = await pipe.Reader.ReadAsync().ConfigureAwait(false);
                            if (!readResult.Buffer.IsEmpty)
                            {
                                if (first && !noPreamble)
                                {
                                    var pre = outputEncoding.GetPreamble();
                                    if (pre != null && pre.Length != 0)
                                    {
                                        output.Write(pre, 0, pre.Length);
                                    }
                                }
                                first = false;
                                foreach (var rbuf in readResult.Buffer)
                                {
                                    prevcr = WriteBytesToStream(output, rbuf, nl, prevcr);
                                }
                            }
                            if (readResult.Buffer.IsEmpty && readResult.IsCompleted)
                            {
                                break;
                            }
                        }
                    })
                );
            }
        }
    }
}