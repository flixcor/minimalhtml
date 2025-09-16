using MinimalForms.ModelGenerator.Utility;

namespace MinimalForms.ModelGenerator;

internal readonly record struct ConfigDeclaration
{
    public ConfigDeclaration(string @namespace, string @class, string accessibility, EquatableArray<Pair> pairs)
    {
        Namespace = @namespace;
        Class = @class;
        Accessibility = accessibility;
        Pairs = pairs;
    }

    public string Namespace { get; }
    public string Class { get; }
    public string Accessibility { get; }
    public EquatableArray<Pair> Pairs { get; }
}

internal readonly record struct Pair
{
    public Pair(string name, EquatableArray<string> arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    public string Name { get; }
    public EquatableArray<string> Arguments { get; }
}

internal readonly record struct GeneratedProp
{
    public GeneratedProp(string name, string type, Pair[] configurations, GeneratedProp[] children)
    {
        Type = type;
        Name = name;
        var withoutQuotes = name.AsSpan().Trim('"');
        Configurations = configurations;
        Children = children;
        var pascal = GetSafeName(withoutQuotes);
        SafeName = pascal;
        CamelCase = Casing.PascalToCamel(pascal.AsSpan());
    }



    public string Name { get; }
    public string Type { get; }
    public string SafeName { get; }
    public string CamelCase { get; }
    public Pair[] Configurations { get; }
    public GeneratedProp[] Children { get; }

    public override string ToString()
    {
        return Type + "(" + SafeName + ")[" + string.Join(",", Children) + "]";
    }

    private static string GetSafeName(ReadOnlySpan<char> withoutQuotes)
    {
        withoutQuotes = withoutQuotes.Trim('"');
        Span<char> safeName = stackalloc char[withoutQuotes.Length];
        var upper = true;
        var j = 0;
        for (var i = 0; i < withoutQuotes.Length; i++)
        {
            var ch = withoutQuotes[i];
            if (ch == '[' || ch == ']' || ch == '.')
            {
                continue;
            }
            if (char.IsWhiteSpace(ch))
            {
                upper = true;
                continue;
            }
            if (upper)
            {
                ch = char.ToUpperInvariant(ch);
                upper = false;
            }
            safeName[j++] = ch;
        }
        return safeName.Slice(0, j).ToString();
    }
}
