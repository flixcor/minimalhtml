// v2 negative fixture (repurposed from v1): only UNMARKED interpolated
// literals remain inert — marker-annotated interpolated cases are positive
// under v2 FR-006 and live in tests/fixtures/positive/interpolated-*.cs.
// This file exists so the v1 marker-parser test "returns [] for
// negative/interpolated.cs" still has a valid unmarked corpus to assert
// against (parser MUST NOT claim any HtmlRegion from an unmarked $"...").

using System;

public class InterpolatedFixtures
{
    public string Name = "world";

    // Unmarked $"..." — no marker, no HtmlRegion.
    public string Case1 = $"<p>hello {Name}</p>";

    // Unmarked $@"..." — no marker, no HtmlRegion.
    public string Case2 = $@"<p>hello {Name}</p>";

    // Unmarked $"""...""" — no marker, no HtmlRegion.
    public string Case3 = $"""<p>hello {Name}</p>""";
}
