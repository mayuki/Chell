using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chell.Internal
{
    internal class StreamPipe
    {
        private readonly Pipe _pipe;
        private readonly Stream _baseStream;
        private readonly Task _copyTask;
        private readonly Task _readerTask;
        private readonly CancellationTokenSource _cancellationTokenSourceCopyStreamToPipe;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object _syncLock = new object();
        private readonly List<object> _destinations = new List<object>();
        private readonly TaskCompletionSource<bool> _destinationsReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _shutdown;

        public StreamPipe(Stream baseStream)
        {
            _pipe = new Pipe(new PipeOptions());
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSourceCopyStreamToPipe = new CancellationTokenSource();

            _copyTask = CopyStreamToPipeAsync(_cancellationTokenSourceCopyStreamToPipe.Token);
            _readerTask = RunReadLoopAsync(_cancellationTokenSource.Token);
        }

        private async Task CopyStreamToPipeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _baseStream.CopyToAsync(_pipe.Writer, cancellationToken).ConfigureAwait(false);
                await _pipe.Writer.CompleteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _pipe.Writer.CompleteAsync(ex).ConfigureAwait(false);
            }
        }

        public async Task CompleteAsync()
        {
            _shutdown = true;
            _cancellationTokenSource.CancelAfter(1000);
            _cancellationTokenSourceCopyStreamToPipe.CancelAfter(1000);

            try
            {
                await _copyTask.ConfigureAwait(false);
                await _readerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Ready()
        {
            _destinationsReady.TrySetResult(true);
        }

        public StreamPipe Connect(Stream stream)
        {
            lock (_syncLock)
            {
                _destinations.Add(stream ?? throw new ArgumentNullException(nameof(stream)));
            }
            return this;
        }
        public StreamPipe Connect(PipeWriter writer)
        {
            lock (_syncLock)
            {
                _destinations.Add(writer ?? throw new ArgumentNullException(nameof(writer)));
            }
            return this;
        }

        public StreamPipe Disconnect(Stream stream)
        {
            lock (_syncLock)
            {
                _destinations.Remove(stream ?? throw new ArgumentNullException(nameof(stream)));
            }
            return this;
        }

        public StreamPipe Disconnect(PipeWriter writer)
        {
            lock (_syncLock)
            {
                _destinations.Remove(writer ?? throw new ArgumentNullException(nameof(writer)));
            }
            return this;
        }

        private async Task RunReadLoopAsync(CancellationToken cancellationToken)
        {
            var cancellationTokenTask = new TaskCompletionSource<bool>();
            await using var cancellationTokenRegistration = cancellationToken.Register(() => cancellationTokenTask.TrySetCanceled(cancellationToken));

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Wait for the destination to be available.
                object[] dests;
                lock (_syncLock)
                {
                    dests = _destinations.ToArray();
                }
                while (dests.Length == 0)
                {
                    if (_shutdown) return;
                    cancellationToken.ThrowIfCancellationRequested();

                    // Wait for signal to start sending to the destinations.
                    // This will wait only once after the process starts.
                    await Task.WhenAny(cancellationTokenTask.Task, _destinationsReady.Task).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    lock (_syncLock)
                    {
                        dests = _destinations.ToArray();
                    }

                    if (dests.Length == 0)
                    {
                        await Task.Yield();
                    }
                }

                // Reads from the pipe reader.
                var result = await _pipe.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                // Get the current destination again.
                // If there is no destination after reading, do not advance the pipe, and go to the top of the loop.
                lock (_syncLock)
                {
                    var destsCurrent = _destinations.ToArray();
                    if (!dests.SequenceEqual(destsCurrent))
                    {
                        dests = destsCurrent;
                        if (dests.Length == 0)
                        {
                            _pipe.Reader.AdvanceTo(result.Buffer.Start);
                            continue;
                        }
                    }
                }

                // Writes to the destinations.
                await Task.WhenAll(dests.Select(async x =>
                {
                    var writeTask = x switch
                    {
                        Stream stream => WriteAsync(stream, result, cancellationToken),
                        PipeWriter writer => WriteAsync(writer, result, cancellationToken),
                        _ => throw new NotSupportedException()
                    };

                    // NOTE: The destination may be closed first.
                    //       When the destination is closed, the task throws an IOException (Broken pipe).
                    try
                    {
                        await writeTask.ConfigureAwait(false);
                    }
                    catch (IOException)
                    {
                    }
                })).ConfigureAwait(false);

                _pipe.Reader.AdvanceTo(result.Buffer.End);

                if (result.IsCanceled || result.IsCompleted)
                {
                    return;
                }
            }
        }

        private static async ValueTask WriteAsync(Stream stream, ReadResult result, CancellationToken cancellationToken)
        {
            if (!result.Buffer.IsEmpty)
            {
                if (result.Buffer.IsSingleSegment)
                {
                    await stream.WriteAsync(result.Buffer.First, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    foreach (var segment in result.Buffer)
                    {
                        await stream.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
                    }
                }
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            }

            if (result.IsCompleted || result.IsCanceled)
            {
                stream.Close();
            }
        }

        private static async ValueTask WriteAsync(PipeWriter writer, ReadResult result, CancellationToken cancellationToken)
        {
            if (!result.Buffer.IsEmpty)
            {
                if (result.Buffer.IsSingleSegment)
                {
                    await writer.WriteAsync(result.Buffer.First, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    foreach (var segment in result.Buffer)
                    {
                        await writer.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
                    }
                }
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            }

            if (result.IsCompleted || result.IsCanceled)
            {
                await writer.CompleteAsync().ConfigureAwait(false);
            }
        }
    }
}
