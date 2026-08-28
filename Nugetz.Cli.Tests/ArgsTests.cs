using Nugetz.Cli.Commands;

namespace Nugetz.Cli.Tests;

public sealed class ArgsTests
{
    [Fact]
    public void ParsesPositionalsOptionsAndFlags()
    {
        var args = new Args(["Serilog", "--version", "4.0.0", "--yes", "-p", "src/App.csproj"]);

        Assert.Equal("Serilog", args.Positional(0));
        Assert.Equal("4.0.0", args.Option("--version", "-v"));
        Assert.Equal("src/App.csproj", args.Option("--project", "-p"));
        Assert.True(args.Flag("--yes", "-y"));
    }

    [Fact]
    public void ReturnsDefaultForInvalidIntegerOption()
    {
        var args = new Args(["query", "--limit", "not-a-number"]);

        Assert.Equal(10, args.OptionInt(10, "--limit"));
    }
}
