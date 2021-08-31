using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chell.Internal
{
    internal class OutputSink : IDisposable, IAsyncDisposable
    {
        private readonly Encoding _encoding;
        private readonly Pipe _outputPipe;
        private readonly Pipe _errorPipe;
        private readonly MemoryStream _outputBuffer;
        private readonly MemoryStream _errorBuffer;
        private readonly MemoryStream _combinedBuffer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _readWriteTaskOutput;
        private readonly Task _readWriteTaskError;

        internal PipeWriter OutputWriter => _outputPipe.Writer;
        internal PipeWriter ErrorWriter => _errorPipe.Writer;

        public ReadOnlyMemory<byte> OutputBinary => new ReadOnlyMemory<byte>(_outputBuffer.GetBuffer(), 0, (int)_outputBuffer.Length);
        public ReadOnlyMemory<byte> ErrorBinary => new ReadOnlyMemory<byte>(_errorBuffer.GetBuffer(), 0, (int)_errorBuffer.Length);
        public ReadOnlyMemory<byte> CombinedBinary => new ReadOnlyMemory<byte>(_combinedBuffer.GetBuffer(), 0, (int)_combinedBuffer.Length);
        public string Output => (_outputBuffer is { Length: > 0} s) ? _encoding.GetString(OutputBinary.Span) : string.Empty;
        public string Error => (_errorBuffer is { Length: > 0 } s) ? _encoding.GetString(ErrorBinary.Span) : string.Empty;
        public string Combined => (_combinedBuffer is { Length: > 0 } s) ? _encoding.GetString(CombinedBinary.Span) : string.Empty;

        public OutputSink(Encoding encoding)
        {
            _encoding = encoding;
            _outputPipe = new Pipe();
            _errorPipe = new Pipe();
            _cancellationTokenSource = new CancellationTokenSource();

            _outputBuffer = new MemoryStream();
            _errorBuffer = new MemoryStream();
            _combinedBuffer = new MemoryStream();

            _readWriteTaskOutput = RunReadWriteLoopAsync(_outputPipe.Reader, _outputBuffer, _cancellationTokenSource.Token);
            _readWriteTaskError = RunReadWriteLoopAsync(_errorPipe.Reader, _errorBuffer, _cancellationTokenSource.Token);
        }

        public async Task CompleteAsync()
        {
            await _outputPipe.Writer.CompleteAsync().ConfigureAwait(false);
            await _errorPipe.Writer.CompleteAsync().ConfigureAwait(false);

            try
            {
                _cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                await _readWriteTaskOutput.ConfigureAwait(false);
                await _readWriteTaskError.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task RunReadWriteLoopAsync(PipeReader reader, Stream dest, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                if (result.Buffer.IsSingleSegment)
                {
                    dest.Write(result.Buffer.FirstSpan);
                    _combinedBuffer.Write(result.Buffer.FirstSpan);
                }
                else
                {
                    foreach (var segment in result.Buffer)
                    {
                        dest.Write(segment.Span);
                        _combinedBuffer.Write(segment.Span);
                    }
                }

                reader.AdvanceTo(result.Buffer.End);

                if (result.IsCanceled || result.IsCompleted)
                {
                    return;
                }
            }
        }

        public void Dispose()
        {
            try
            {
                CompleteAsync().Wait();
            }
            catch { }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await CompleteAsync().ConfigureAwait(false);
            }
            catch { }
        }
    }
}