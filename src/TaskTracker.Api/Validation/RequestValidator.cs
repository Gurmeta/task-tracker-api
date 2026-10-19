using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Api.Validation;

public static class RequestValidator
{
    // Runs the DataAnnotations attributes ([Required], [StringLength]) on a request object
    // and returns the errors grouped by property name, ready for Results.ValidationProblem.
    public static bool TryValidate<T>(T request, out Dictionary<string, string[]> errors)
        where T : notnull
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        errors = results
            .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : [""])
                .Select(member => (Member: ToCamelCase(member), Message: r.ErrorMessage ?? "Invalid value.")))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());

        return errors.Count == 0;
    }

    private static string ToCamelCase(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
