using System.Reflection;

namespace TaskTracker.Api;

public static class AppVersion
{
    // Read once from the assembly, which gets it from <Version> in the .csproj.
    public static string Current { get; } =
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";
}
