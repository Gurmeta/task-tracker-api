using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace TaskTracker.Tests;

// Starts the real API in memory for each test, with its own temporary SQLite file,
// so tests never share data and never touch a developer's real database.
public class TaskTrackerFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tasktracker-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // SQLite keeps pooled connections open, which would lock the file on Windows.
            SqliteConnection.ClearAllPools();
            foreach (var file in new[] { _dbPath, _dbPath + "-shm", _dbPath + "-wal" })
            {
                try { File.Delete(file); } catch (IOException) { /* best-effort cleanup of a temp file */ }
            }
        }
    }
}
