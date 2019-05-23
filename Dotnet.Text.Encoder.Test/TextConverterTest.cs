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
    }
}
