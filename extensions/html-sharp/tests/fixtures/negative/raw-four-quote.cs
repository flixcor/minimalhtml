// US2 negative fixture: raw literals with N>3 opening quotes are unsupported
// in v1 per research.md R3. Even with a marker, these MUST remain flat C#
// string coloring — parseHtmlRegions() MUST return [] for this file.

using System;

public class RawFourQuoteFixtures
{
    // Case 1: 4-quote raw with marker — stays flat.
    public string Case1 = /*lang=html*/""""<p id="x">hi</p>"""";

    // Case 2: 5-quote raw with marker and body containing """ — stays flat.
    public string Case2 = /*lang=html*/"""""<p data-q="""triple""">body</p>""""";
}
