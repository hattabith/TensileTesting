using System.Collections.Concurrent;
using System.IO;

namespace TensileTestingApp.Models
{
    public class CSVLogger : IAsyncDisposable
    {
        private StreamWriter? _writer;
        private readonly ConcurrentQueue<TensileTestData> _buffer = new();
        private CancellationTokenSource? _flushCts;
        private Task? _flushTask;

        public async Task StartLogginAsync(string filePath)
        {
            await StopLoggingAsync();

            _writer = new StreamWriter(filePath, append: true);
            await _writer.WriteLineAsync("Timestamp,Force,Length");

            _flushCts = new CancellationTokenSource();
            _flushTask = Task.Run(() => FlushBufferAsync(_flushCts.Token));
        }

        public void Enqueue(TensileTestData data)
        {
            _buffer.Enqueue(data);
        }

        public async Task StopLoggingAsync()
        {
            if (_flushCts is not null)
            {
                _flushCts.Cancel();

                if (_flushTask is not null)
                {
                    try
                    {
                        await _flushTask;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                _flushCts.Dispose();
                _flushCts = null;
                _flushTask = null;
            }

            if (_writer is not null)
            {
                await _writer.FlushAsync();
                _writer.Dispose();
                _writer = null;
            }
        }

        private async Task FlushBufferAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_writer is not null && _buffer.TryDequeue(out var data))
                {
                    await _writer.WriteLineAsync($"{data.Timestamp},{data.Force},{data.Length}");
                    continue;
                }

                await Task.Delay(100, cancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopLoggingAsync();
        }
    }
}
