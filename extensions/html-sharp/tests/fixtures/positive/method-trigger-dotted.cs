// v6 dotted-receiver fixture: every canonical form of receiver.Html("...") MUST
// produce an HTML region. The parser word-boundary check (!isIdentChar(prevCode))
// passes for '.' and ')' which precede Html in all dotted call forms.

using System;

public class DottedReceiverFixtures
{
    private readonly HtmlBuilder _builder = new HtmlBuilder();

    // Case 1: simple identifier receiver.
    public string Case1() => _builder.Html("<p>hello</p>");

    // Case 2: 'this' keyword receiver.
    public string Case2() => this._builder.Html("<span>world</span>");

    // Case 3: static class name receiver.
    public string Case3() => HtmlBuilder.Html("<h1>title</h1>");

    // Case 4: method-call-result receiver (fluent / factory pattern).
    public string Case4() => GetBuilder().Html("<div>content</div>");

    // Case 5: multi-level member-access chain.
    public string Case5() => _builder.Inner.Html("<em>nested</em>");

    // Case 6: interpolated string — hole must be excluded from HTML region.
    public string Case6(string name) => _builder.Html($"<p>{name}</p>");

    private HtmlBuilder GetBuilder() => new HtmlBuilder();
}

public class HtmlBuilder
{
    public HtmlBuilder Inner { get; } = null!;
    public static string Html(string s) => s;
    string Html(string s) => s;
}
