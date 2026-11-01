using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Api.Data;
using TaskTracker.Api.Models;
using TaskTracker.Api.Services;

namespace TaskTracker.Tests;

// Tests the store directly against an in-memory SQLite database.
// The connection must stay open: an in-memory SQLite database disappears when it closes.
public class EfTaskStoreTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly AppDbContext _db;
    private readonly EfTaskStore _store;

    public EfTaskStoreTests()
    {
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.Migrate(); // also proves the migrations apply cleanly to an empty database
        _store = new EfTaskStore(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Add_AssignsIdAndUtcCreatedAt()
    {
        var before = DateTime.UtcNow;

        var task = await _store.AddAsync(new TaskItem { Title = "new" }, CancellationToken.None);

        Assert.True(task.Id > 0);
        Assert.Equal(DateTimeKind.Utc, task.CreatedAt.Kind);
        Assert.True(task.CreatedAt >= before);
    }

    [Fact]
    public async Task Update_MissingTask_ReturnsNull()
    {
        var result = await _store.UpdateAsync(
            999, "x", null, TaskItemStatus.Done, null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_MissingTask_ReturnsFalse()
    {
        Assert.False(await _store.DeleteAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_ExistingTask_ReturnsTrueAndRemovesIt()
    {
        var task = await _store.AddAsync(new TaskItem { Title = "temp" }, CancellationToken.None);

        Assert.True(await _store.DeleteAsync(task.Id, CancellationToken.None));
        Assert.Null(await _store.GetByIdAsync(task.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetAll_FiltersByStatusAndOrdersById()
    {
        await _store.AddAsync(new TaskItem { Title = "a", Status = TaskItemStatus.Done }, CancellationToken.None);
        await _store.AddAsync(new TaskItem { Title = "b", Status = TaskItemStatus.Todo }, CancellationToken.None);
        await _store.AddAsync(new TaskItem { Title = "c", Status = TaskItemStatus.Done }, CancellationToken.None);

        var done = await _store.GetAllAsync(new TaskQuery(Status: TaskItemStatus.Done), CancellationToken.None);
        var all = await _store.GetAllAsync(new TaskQuery(), CancellationToken.None);

        Assert.Equal(["a", "c"], done.Select(t => t.Title));
        Assert.Equal(["a", "b", "c"], all.Select(t => t.Title));
    }

    [Fact]
    public async Task GetAll_AppliesLimitAndOffsetInIdOrder()
    {
        foreach (var title in new[] { "a", "b", "c", "d", "e" })
        {
            await _store.AddAsync(new TaskItem { Title = title }, CancellationToken.None);
        }

        var page = await _store.GetAllAsync(new TaskQuery(Limit: 2, Offset: 1), CancellationToken.None);
        var pastTheEnd = await _store.GetAllAsync(new TaskQuery(Limit: 2, Offset: 10), CancellationToken.None);

        Assert.Equal(["b", "c"], page.Select(t => t.Title));
        Assert.Empty(pastTheEnd);
    }

    [Fact]
    public async Task Count_CountsAllMatchingTasksRegardlessOfPaging()
    {
        await _store.AddAsync(new TaskItem { Title = "a", Status = TaskItemStatus.Done }, CancellationToken.None);
        await _store.AddAsync(new TaskItem { Title = "b", Status = TaskItemStatus.Todo }, CancellationToken.None);
        await _store.AddAsync(new TaskItem { Title = "c", Status = TaskItemStatus.Done }, CancellationToken.None);

        Assert.Equal(3, await _store.CountAsync(new TaskQuery(), CancellationToken.None));
        Assert.Equal(2, await _store.CountAsync(new TaskQuery(Status: TaskItemStatus.Done), CancellationToken.None));
    }

    [Fact]
    public async Task StatusIsStoredAsText()
    {
        var task = await _store.AddAsync(
            new TaskItem { Title = "x", Status = TaskItemStatus.InProgress }, CancellationToken.None);

        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT Status FROM Tasks WHERE Id = $id";
        command.Parameters.AddWithValue("$id", task.Id);

        Assert.Equal("InProgress", (string?)await command.ExecuteScalarAsync());
    }
}
