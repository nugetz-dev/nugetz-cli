namespace Nugetz.Cli.Services;

public sealed class ProjectDiscoveryService
{
    private static readonly string[] PriorityPrefixes = ["src", "apps", "services", "tests"];

    public List<string> FindProjects(string root)
    {
        var projects = Directory
            .EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrVendored(path, root))
            .Select(p => Path.GetRelativePath(root, p))
            .ToList();

        projects.Sort((a, b) =>
        {
            var aPriority = GetPriority(a);
            var bPriority = GetPriority(b);
            if (aPriority != bPriority)
                return aPriority.CompareTo(bPriority);
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        });

        return projects;
    }

    private static bool IsGeneratedOrVendored(string path, string root)
    {
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        return relative.Split('/').Any(segment => segment is "bin" or "obj" or ".git" or "node_modules");
    }

    private static int GetPriority(string path)
    {
        var normalized = path.Replace('\\', '/');
        for (var i = 0; i < PriorityPrefixes.Length; i++)
        {
            if (normalized.StartsWith(PriorityPrefixes[i] + "/", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return PriorityPrefixes.Length;
    }
}
