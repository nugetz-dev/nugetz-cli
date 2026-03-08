namespace Nugetz.Cli.Commands;

public sealed class Args
{
    private readonly string[] _args;
    private readonly Dictionary<string, string?> _options = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _positional = [];

    public Args(string[] args)
    {
        _args = args;
        Parse();
    }

    private void Parse()
    {
        for (var i = 0; i < _args.Length; i++)
        {
            var arg = _args[i];

            if (arg.StartsWith("--") || (arg.StartsWith('-') && arg.Length == 2))
            {
                // Check if next arg is a value (not another flag)
                if (i + 1 < _args.Length && !_args[i + 1].StartsWith('-'))
                {
                    _options[arg] = _args[i + 1];
                    i++;
                }
                else
                {
                    _flags.Add(arg);
                }
            }
            else
            {
                _positional.Add(arg);
            }
        }
    }

    public string? Positional(int index) =>
        index < _positional.Count ? _positional[index] : null;

    public string? Option(params string[] names)
    {
        foreach (var name in names)
            if (_options.TryGetValue(name, out var value))
                return value;
        return null;
    }

    public bool Flag(params string[] names)
    {
        foreach (var name in names)
            if (_flags.Contains(name))
                return true;
        return false;
    }

    public int OptionInt(int defaultValue, params string[] names)
    {
        var val = Option(names);
        return val is not null && int.TryParse(val, out var n) ? n : defaultValue;
    }
}
