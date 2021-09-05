using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Chell.IO
{
    public class LINQPadConsoleProvider : IConsoleProvider
    {
        private readonly Pipe _pipe;

        public Encoding InputEncoding => Console.InputEncoding;
        public Encoding OutputEncoding => Console.OutputEncoding;
        public bool IsInputRedirected => Console.IsInputRedirected;
        public bool IsOutputRedirected => Console.IsOutputRedirected;
        public bool IsErrorRedirected => Console.IsErrorRedirected;

        public TextWriter Out { get; }
        public TextWriter Error { get; }

        public LINQPadConsoleProvider()
        {
            _pipe = new Pipe();
            Out = new PipeTextWriter(_pipe.Writer, OutputEncoding);
            Error = new PipeTextWriter(_pipe.Writer, OutputEncoding);

            var reader = new StreamReader(_pipe.Reader.AsStream());
            _ = Task.Run(async () =>
            {
                Memory<char> buffer = new char[1024];
                while (true)
                {
                    var read = await reader.ReadAsync(buffer, default);
                    if (read != 0)
                    {
                        Console.Out.Write(buffer.Span.Slice(0, read));
                    }
                }
            });
        }

        public Stream OpenStandardInput()
            => Console.OpenStandardInput();

        public Stream OpenStandardOutput()
            => _pipe.Writer.AsStream(leaveOpen: true);

        public Stream OpenStandardError()
            => _pipe.Writer.AsStream(leaveOpen: true);

        private class PipeTextWriter : TextWriter
        {
            private readonly PipeWriter _writer;
            public override Encoding Encoding { get; }

            public PipeTextWriter(PipeWriter writer, Encoding encoding)
            {
                _writer = writer;
                Encoding = encoding;
            }

            public override void Write(char value)
            {
                Span<byte> buffer = stackalloc byte[4];
                Span<char> c = stackalloc char[1];
                c[0] = value;

                var written = Encoding.GetBytes(c, buffer);
                _writer.Write(buffer.Slice(0, written));
                _ = _writer.FlushAsync();
            }
        }
    }
}
