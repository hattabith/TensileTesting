using System.IO;
using TensileTestingApp.Models;

namespace TensileTesting.Tests
{
    public class CSVLoggerTests : IAsyncDisposable
    {
        private readonly string _tempFile = Path.GetTempFileName();

        // ── StartLogginAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task StartLogginAsync_CreatesFileWithCsvHeader()
        {
            await using var logger = new CSVLogger();
            await logger.StartLogginAsync(_tempFile);
            await logger.StopLoggingAsync();

            string content = await File.ReadAllTextAsync(_tempFile);
            Assert.Contains("Timestamp,Force,Length", content);
        }

        [Fact]
        public async Task StartLogginAsync_CalledTwice_StopsFirstSessionBeforeStartingNew()
        {
            await using var logger = new CSVLogger();
            await logger.StartLogginAsync(_tempFile);
            var ex = await Record.ExceptionAsync(() => logger.StartLogginAsync(_tempFile));
            await logger.StopLoggingAsync();

            Assert.Null(ex);
        }

        // ── Enqueue / flush ───────────────────────────────────────────────────

        [Fact]
        public async Task Enqueue_SingleItem_IsWrittenToFileAfterStop()
        {
            await using var logger = new CSVLogger();
            await logger.StartLogginAsync(_tempFile);

            var data = new TensileTestData
            {
                Timestamp = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc),
                Force = 50.0,
                Length = 25.0
            };
            logger.Enqueue(data);

            // Give the background flush loop time to process the item
            await Task.Delay(300);
            await logger.StopLoggingAsync();

            string content = await File.ReadAllTextAsync(_tempFile);
            Assert.Contains("50", content);
            Assert.Contains("25", content);
        }

        [Fact]
        public async Task Enqueue_MultipleItems_AllWrittenAfterStop()
        {
            await using var logger = new CSVLogger();
            await logger.StartLogginAsync(_tempFile);

            for (int i = 1; i <= 5; i++)
            {
                logger.Enqueue(new TensileTestData
                {
                    Timestamp = DateTime.UtcNow,
                    Force = i * 10.0,
                    Length = i * 1.0
                });
            }

            await Task.Delay(600);
            await logger.StopLoggingAsync();

            string[] lines = await File.ReadAllLinesAsync(_tempFile);
            // Header + 5 data lines
            Assert.True(lines.Length >= 6);
        }

        // ── StopLoggingAsync idempotency ──────────────────────────────────────

        [Fact]
        public async Task StopLoggingAsync_CalledBeforeStart_DoesNotThrow()
        {
            await using var logger = new CSVLogger();
            var ex = await Record.ExceptionAsync(() => logger.StopLoggingAsync());
            Assert.Null(ex);
        }

        [Fact]
        public async Task StopLoggingAsync_CalledTwice_DoesNotThrow()
        {
            await using var logger = new CSVLogger();
            await logger.StartLogginAsync(_tempFile);
            await logger.StopLoggingAsync();
            var ex = await Record.ExceptionAsync(() => logger.StopLoggingAsync());
            Assert.Null(ex);
        }

        // ── DisposeAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task DisposeAsync_StopsLoggingGracefully()
        {
            var logger = new CSVLogger();
            await logger.StartLogginAsync(_tempFile);
            logger.Enqueue(new TensileTestData { Timestamp = DateTime.UtcNow, Force = 1.0, Length = 0.5 });
            await Task.Delay(300);
            await logger.DisposeAsync();

            // File should exist and contain the header at minimum
            Assert.True(File.Exists(_tempFile));
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        public ValueTask DisposeAsync()
        {
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);
            return ValueTask.CompletedTask;
        }
    }
}
