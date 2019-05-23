using System.Text;
using System.IO;
using System.Buffers;

namespace dotnet_text_encoder
{
    static class TextConverter
    {
        public static void ConvertStream(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding, bool noPreamble)
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
                    var wlen = outputEncoding.GetByteCount(buf, 0, charread);
                    if(wlen > wbuf.Length)
                    {
                        ArrayPool<byte>.Shared.Return(wbuf);
                        wbuf = ArrayPool<byte>.Shared.Rent(wlen);
                    }
                    outputEncoding.GetBytes(buf, 0, charread, wbuf, 0);
                    output.Write(wbuf, 0, wlen);
                }
            }
        }
    }
}