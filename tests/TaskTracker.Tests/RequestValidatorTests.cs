using TaskTracker.Api.Models;
using TaskTracker.Api.Validation;

namespace TaskTracker.Tests;

// Unit tests: no HTTP and no database, only the validation logic.
public class RequestValidatorTests
{
    [Fact]
    public void ValidCreateRequest_PassesValidation()
    {
        var request = new CreateTaskRequest { Title = "Valid", Description = "ok" };

        var isValid = RequestValidator.TryValidate(request, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankTitle_FailsWithCamelCaseKey(string? title)
    {
        var request = new CreateTaskRequest { Title = title };

        var isValid = RequestValidator.TryValidate(request, out var errors);

        Assert.False(isValid);
        Assert.Contains("title", errors.Keys);
    }

    [Fact]
    public void TitleOfExactly100Characters_IsAccepted()
    {
        var request = new CreateTaskRequest { Title = new string('x', 100) };

        Assert.True(RequestValidator.TryValidate(request, out _));
    }

    [Fact]
    public void TitleOf101Characters_IsRejected()
    {
        var request = new CreateTaskRequest { Title = new string('x', 101) };

        Assert.False(RequestValidator.TryValidate(request, out var errors));
        Assert.Contains("title", errors.Keys);
    }

    [Fact]
    public void DescriptionOf501Characters_IsRejected()
    {
        var request = new CreateTaskRequest { Title = "ok", Description = new string('x', 501) };

        Assert.False(RequestValidator.TryValidate(request, out var errors));
        Assert.Contains("description", errors.Keys);
    }

    [Fact]
    public void UpdateRequestWithoutStatus_Fails()
    {
        var request = new UpdateTaskRequest { Title = "ok" };

        Assert.False(RequestValidator.TryValidate(request, out var errors));
        Assert.Contains("status", errors.Keys);
    }

    [Fact]
    public void UpdateRequestWithAllFields_Passes()
    {
        var request = new UpdateTaskRequest { Title = "ok", Status = TaskItemStatus.Done };

        Assert.True(RequestValidator.TryValidate(request, out _));
    }
}
