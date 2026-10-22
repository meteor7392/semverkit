using SemVerKit;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 0;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "parse":
                    RequireArgumentCount(args, 2);
                    Console.WriteLine(SemanticVersion.Parse(args[1]));
                    return 0;

                case "compare":
                    RequireArgumentCount(args, 3);
                    var left = SemanticVersion.Parse(args[1]);
                    var right = SemanticVersion.Parse(args[2]);
                    var comparison = left.CompareTo(right);
                    Console.WriteLine(comparison < 0 ? -1 : comparison > 0 ? 1 : 0);
                    return 0;

                case "satisfies":
                    RequireArgumentCount(args, 3);
                    var version = SemanticVersion.Parse(args[1]);
                    var satisfied = VersionRange.IsSatisfiedBy(args[2], version);
                    Console.WriteLine(satisfied);
                    return satisfied ? 0 : 1;

                case "bump":
                    RequireArgumentCount(args, 3);
                    var source = SemanticVersion.Parse(args[1]);
                    var bumped = args[2].ToLowerInvariant() switch
                    {
                        "major" => new SemanticVersion(source.Major + 1, 0, 0),
                        "minor" => new SemanticVersion(source.Major, source.Minor + 1, 0),
                        "patch" => new SemanticVersion(source.Major, source.Minor, source.Patch + 1),
                        _ => throw new ArgumentException("Bump level must be major, minor, or patch.")
                    };
                    Console.WriteLine(bumped);
                    return 0;

                default:
                    PrintUsage();
                    return 1;
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
    }

    private static void RequireArgumentCount(string[] args, int expected)
    {
        if (args.Length != expected)
        {
            throw new ArgumentException($"Expected {expected - 1} argument(s) after '{args[0]}'.");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  semverkit parse <version>");
        Console.WriteLine("  semverkit compare <version> <version>");
        Console.WriteLine("  semverkit satisfies <version> <range>");
        Console.WriteLine("  semverkit bump <version> <major|minor|patch>");
    }
}
