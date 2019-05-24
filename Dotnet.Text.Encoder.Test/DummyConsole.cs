using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace Dotnet.Text.Encoder
{
    class DummyConsole : IConsole
    {
        public TextWriter Out => Console.Out;

        public TextWriter Error => Console.Error;

        public TextReader In => Console.In;

        public bool IsInputRedirected => true;

        public bool IsOutputRedirected => true;

        public bool IsErrorRedirected => true;

        public ConsoleColor ForegroundColor { 
            get => ConsoleColor.Gray;
            set 
            {
            }
        }
        public ConsoleColor BackgroundColor { get => ConsoleColor.Black; set {} }

        public event ConsoleCancelEventHandler CancelKeyPress;

        public void ResetColor()
        {
        }
    }

}