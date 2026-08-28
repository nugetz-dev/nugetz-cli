using Nugetz.Cli.Services;

namespace Nugetz.Cli.Tests;

public sealed class UpgradePlannerTests
{
    [Theory]
    [InlineData("1.9.0", "2.0.0", true)]
    [InlineData("2.0.0", "2.1.0", false)]
    [InlineData("2.0.0-preview.1", "3.0.0", true)]
    public void ClassifiesMajorUpdates(string current, string target, bool expected)
    {
        Assert.Equal(expected, UpgradePlanner.IsMajorUpdate(current, target));
    }
}
