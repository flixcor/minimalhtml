// v6 negative fixture: dotted calls whose method name is NOT "Html" (case-sensitive)
// MUST NOT produce HTML regions.

public class DottedReceiverNegativeFixtures
{
    private readonly object _obj = new object();

    // Case 1: wrong method name in dotted position — must NOT trigger.
    public string Case1() => _obj.Render("<p>x</p>");

    // Case 2: lowercase method name — case-sensitive; must NOT trigger.
    public string Case2() => _obj.html("<p>x</p>");
}
