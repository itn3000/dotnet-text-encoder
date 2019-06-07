using System;
using System.Text;
using System.IO;
using Xunit;
using System.Linq;
using dotnet_text_encoder;

namespace Dotnet.Text.Encoder.Test
{
    public class TextConverterTest
    {
        public TextConverterTest()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        [Theory]
        [InlineData("utf-8", "shift_jis")]
        [InlineData("shift_jis", "utf-8")]
        [InlineData("932", "65001")]
        public void LongString(string srcEncoding, string destEncoding)
        {
            var srcstr = new string(Enumerable.Range(0, 10240).Select(x => 'あ').ToArray());
            var srcenc = TextConverter.GetEncodingFromString(srcEncoding);
            var destenc = TextConverter.GetEncodingFromString(destEncoding);
            using(var instm = new MemoryStream(srcenc.GetBytes(srcstr)))
            using(var outstm = new MemoryStream())
            {
                dotnet_text_encoder.TextConverter.ConvertStream(instm, srcenc, outstm, destenc, true);
                var deststr = destenc.GetString(outstm.ToArray());
                Assert.Equal(srcstr, deststr);
            }
        }
        [Fact]
        public void NoPreamble()
        {
            var srcstr = new string(Enumerable.Range(0, 10000).Select(x => 'あ').ToArray());
            var srcenc = Encoding.UTF8;
            var destenc = Encoding.UTF8;
            using(var instm = new MemoryStream(srcenc.GetBytes(srcstr)))
            using(var outstm = new MemoryStream())
            {
                dotnet_text_encoder.TextConverter.ConvertStream(instm, srcenc, outstm, destenc, true);
                var destbytes = outstm.ToArray();
                var deststr = destenc.GetString(destbytes);
                Assert.Equal(srcstr, deststr);
                Assert.NotEqual(0xef, destbytes[0]);
                Assert.NotEqual(0xbb, destbytes[1]);
                Assert.NotEqual(0xbf, destbytes[2]);
            }
        }
        [Fact]
        public void Preamble()
        {
            var srcstr = new string(Enumerable.Range(0, 10000).Select(x => 'あ').ToArray());
            var srcenc = Encoding.UTF8;
            var destenc = Encoding.UTF8;
            using(var instm = new MemoryStream(srcenc.GetBytes(srcstr)))
            using(var outstm = new MemoryStream())
            {
                dotnet_text_encoder.TextConverter.ConvertStream(instm, srcenc, outstm, destenc, false);
                var destbytes = outstm.ToArray();
                var deststr = destenc.GetString(destbytes);
                Assert.Equal(0xef, destbytes[0]);
                Assert.Equal(0xbb, destbytes[1]);
                Assert.Equal(0xbf, destbytes[2]);
                // remove bom before comparing
                Assert.Equal(srcstr, new string(deststr.AsSpan(1)));
            }
        }
        [Theory]
        [InlineData("a\r\nb\rc\n", "a\r\nb\rc\n", Newline.None)]
        [InlineData("a\r\nb\rc\n", "a\nb\nc\n", Newline.Lf)]
        [InlineData("a\r\nb\rc\n", "a\r\nb\r\nc\r\n", Newline.Crlf)]
        [InlineData("a\r\nb\rc\n", "a\rb\rc\r", Newline.Cr)]
        [InlineData("a\r\nb\nc\r", "a\r\nb\nc\r", Newline.None)]
        [InlineData("a\r\nb\nc\r", "a\nb\nc\n", Newline.Lf)]
        [InlineData("a\r\nb\nc\r", "a\r\nb\r\nc\r\n", Newline.Crlf)]
        [InlineData("a\r\nb\nc\r", "a\rb\rc\r", Newline.Cr)]
        [InlineData("a\r\nb\nc\r\n", "a\r\nb\nc\r\n", Newline.None)]
        [InlineData("a\r\nb\nc\r\n", "a\nb\nc\n", Newline.Lf)]
        [InlineData("a\r\nb\nc\r\n", "a\r\nb\r\nc\r\n", Newline.Crlf)]
        [InlineData("a\r\nb\nc\r\n", "a\rb\rc\r", Newline.Cr)]
        [InlineData("a\r\nb\nc", "a\r\nb\nc", Newline.None)]
        [InlineData("a\r\nb\nc", "a\nb\nc", Newline.Lf)]
        [InlineData("a\r\nb\nc", "a\r\nb\r\nc", Newline.Crlf)]
        [InlineData("a\r\nb\nc", "a\rb\rc", Newline.Cr)]
        [InlineData("\r\n", "\r\n", Newline.None)]
        [InlineData("\r\n", "\n", Newline.Lf)]
        [InlineData("\r\n", "\r\n", Newline.Crlf)]
        [InlineData("\r\n", "\r", Newline.Cr)]
        [InlineData("\n", "\n", Newline.None)]
        [InlineData("\n", "\n", Newline.Lf)]
        [InlineData("\n", "\r\n", Newline.Crlf)]
        [InlineData("\n", "\r", Newline.Cr)]
        [InlineData("\r", "\r", Newline.None)]
        [InlineData("\r", "\n", Newline.Lf)]
        [InlineData("\r", "\r\n", Newline.Crlf)]
        [InlineData("\r", "\r", Newline.Cr)]
        public void CrLfConvert(string srcstr, string expectedstr, Newline nl)
        {
            var srcenc = Encoding.UTF8;
            var destenc = Encoding.UTF8;
            using(var instm = new MemoryStream(srcenc.GetBytes(srcstr)))
            using(var outstm = new MemoryStream())
            {
                dotnet_text_encoder.TextConverter.ConvertStream(instm, srcenc, outstm, destenc, true, nl);
                var actual = destenc.GetString(outstm.ToArray());
                Assert.Equal(expectedstr, actual);
            }
        }
        [Theory]
        [InlineData(Newline.Cr, "\n", "\r")]
        [InlineData(Newline.Crlf, "\n", "\r\n")]
        [InlineData(Newline.Lf, "\n", "\n")]
        [InlineData(Newline.None, "\n", "\n")]
        [InlineData(Newline.Cr, "\r", "\r")]
        [InlineData(Newline.Crlf, "\r", "\r\n")]
        [InlineData(Newline.Lf, "\r", "\n")]
        [InlineData(Newline.None, "\r", "\r")]
        [InlineData(Newline.Cr, "\r\n", "\r")]
        [InlineData(Newline.Crlf, "\r\n", "\r\n")]
        [InlineData(Newline.Lf, "\r\n", "\n")]
        [InlineData(Newline.None, "\r\n", "\r\n")]
        public void LongStringCrLf(Newline nl, string originalnl, string expectednl)
        {
            var len = 1024;
            var linenum = 10;
            var rnd = new Random();
            var srcstr = new string(Enumerable.Range(0, linenum).SelectMany((idx) =>
                Enumerable.Range(0, len).Select(x => (char)rnd.Next((int)'a', (int)'z')).Concat(originalnl)).ToArray())
                ;
            var expected = srcstr.Replace(originalnl, expectednl);
            var srcenc = Encoding.UTF8;
            var destenc = Encoding.UTF8;
            using(var instm = new MemoryStream(srcenc.GetBytes(srcstr)))
            using(var outstm = new MemoryStream())
            {
                dotnet_text_encoder.TextConverter.ConvertStream(instm, srcenc, outstm, destenc, true, nl);
                var actual = destenc.GetString(outstm.ToArray());
                Assert.Equal(expected, actual);
            }
        }
    }
}
