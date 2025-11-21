using System;
using System.IO;
using System.Text;
using System.Linq;
// using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ConsoleAppFramework;

namespace dotnet_text_encoder
{
    public enum Newline
    {
        None,
        Cr,
        Crlf,
        Lf,
    }
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var app = ConsoleApp.Create();
            app.Add<Commands>();
            app.Run(args);
        }
    }
}
