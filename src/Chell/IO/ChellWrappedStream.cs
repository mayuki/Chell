using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chell.IO
{
    public partial class ChellWritableStream : ChellWrappedStream
    {
        private readonly StreamWriter _writer;

        public ChellWritableStream(Stream baseStream, Encoding encoding)
            : base(baseStream)
        {
            _writer = new StreamWriter(baseStream, encoding);
            _writer.AutoFlush = true;
        }

        public void Write(byte[] value) => BaseStream.Write(value);
        public new void Write(ReadOnlySpan<byte> value) => BaseStream.Write(value);
        public ValueTask WriteAsync(byte[] value, CancellationToken cancellationToken = default) => BaseStream.WriteAsync(value, cancellationToken);
        public new ValueTask WriteAsync(ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default) => BaseStream.WriteAsync(value, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            _writer.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _writer.DisposeAsync();
            await base.DisposeAsync();
        }
    }

    public partial class ChellReadableStream : ChellWrappedStream
    {
        private readonly StreamReader _reader;

        public ChellReadableStream(Stream baseStream, Encoding encoding)
            : base(baseStream)
        {
            _reader = new StreamReader(baseStream, encoding);
        }

        public async Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var readLen = await BaseStream.ReadAsync(bufferWriter.GetMemory(1024 * 32), cancellationToken);
                if (readLen == 0)
                {
                    return bufferWriter.WrittenMemory.ToArray();
                }
                bufferWriter.Advance(readLen);
            }
        }

        public byte[] ReadAllBytes()
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            while (true)
            {
                var readLen = BaseStream.Read(bufferWriter.GetSpan(1024 * 32));
                if (readLen == 0)
                {
                    return bufferWriter.WrittenMemory.ToArray();
                }
                bufferWriter.Advance(readLen);
            }
        }

        public async Task<string> ReadToEndAsync()
        {
            return await _reader.ReadToEndAsync();
        }

        public string ReadToEnd()
        {
            return _reader.ReadToEnd();
        }

        public IEnumerable<string> ReadAllLines()
        {
            while (!_reader.EndOfStream)
            {
                var line = _reader.ReadLine();
                if (line is null) yield break;

                yield return line;
            }
        }

        public async IAsyncEnumerable<string> ReadAllLinesAsync()
        {
            while (!_reader.EndOfStream)
            {
                var line = await _reader.ReadLineAsync();
                if (line is null) yield break;

                yield return line;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _reader.Dispose();
            base.Dispose(disposing);
        }
    }

    public abstract class ChellWrappedStream : Stream
    {
        private readonly Stream _baseStream;

        protected Stream BaseStream => _baseStream;

        protected ChellWrappedStream(Stream baseStream)
        {
            _baseStream = baseStream;
        }

        #region Stream Implementation
        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            return _baseStream.Read(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return _baseStream.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _baseStream.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return _baseStream.WriteAsync(buffer, cancellationToken);
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }
        #endregion
    }
}
