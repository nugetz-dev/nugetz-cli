namespace Nugetz.Cli.Commands;

public static class PackPublishHandler
{
    public static async Task<int> RunAsync(string[] rawArgs)
    {
        var packResult = await PackHandler.RunAsync(rawArgs);
        if (packResult != 0)
            return packResult;
        return await PublishHandler.RunAsync([]);
    }
}
