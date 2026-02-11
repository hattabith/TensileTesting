using System.Collections.Concurrent;
using System.IO;

namespace TensileTestingApp.Models
{
    public class CSVLogger
    {
        private StreamWriter _writer;
        private ConcurrentQueue<TensileTestData> _buffer = new();

        public async Task StartLogginAsync(string filePath)
        {
            _writer = new StreamWriter(filePath, append: true);
            await _writer.WriteLineAsync("Timestamp,Force,Length");
            _ = Task.Run(FlushBufferAsync);
        }
        private async Task FlushBufferAsync()
        {
            while (true)
            {
                if (_buffer.TryDequeue(out var data))
                {
                    await _writer.WriteLineAsync($"{data.Timestamp},{data.Force},{data.Length}");
                }
                await Task.Delay(100); // пауза
            }
        }
    }
}
