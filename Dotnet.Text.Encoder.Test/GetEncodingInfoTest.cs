using dotnet_text_encoder;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Dotnet.Text.Encoder
{
    public class GetEncodingInfoTest
    {
        [Fact]
        public void ByCodePage()
        {
            var cplist = new string[]
            {
                "0-100",
                "200-300",
                "301"
            };
            var results = new EncodingInfoGetter(new DummyConsole())
            {
                CodeRanges = cplist
            }.GetTestResults().ToArray();
            Assert.Equal(203, results.Length);
            for (int i = 0; i <= 100; i++)
            {
                Assert.Contains(results, x => x.CodePage == i);
            }
            for (int i = 200; i <= 301; i++)
            {
                Assert.Contains(results, x => x.CodePage == i);
            }
        }
        [Fact]
        public void InvalidCodePage()
        {
            var cplist = new string[]
            {
                "100-0",
            };
            var results = new EncodingInfoGetter(new DummyConsole())
            {
                CodeRanges = cplist
            }.GetTestResults().ToArray();
            Assert.Empty(results);
        }
        [Fact]
        public void ByName()
        {
            var names = new string[] { "utf-8" };
            var results = new EncodingInfoGetter(new DummyConsole())
            {
                Names = names
            }.GetTestResults().ToArray();
            Assert.Single(results);
            Assert.True(results[0].Found);
            Assert.Equal(names[0], results[0].Name);
        }
        [Fact]
        public void InvalidName()
        {
            var names = new string[] { "aaaaaaaaaaaa" };
            var results = new EncodingInfoGetter(new DummyConsole())
            {
                Names = names
            }.GetTestResults().ToArray();
            Assert.Single(results);
            Assert.False(results[0].Found);
            Assert.Equal(names[0], results[0].Name);
        }
    }
}