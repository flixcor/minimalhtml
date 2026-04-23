// Fixture: positive/diagnostics-unclosed.cs
// Used by html-validator unit tests and e2e diagnostics tests.
//
// Region 0: <p> is unclosed (endTagStart === undefined); <div> is closed.
var r0 = /*lang=html*/ "<div><p>Hello</div>";
// Region 1: top-level <section> is unclosed.
var r1 = /*lang=html*/ "<section>World";
// Region 2: valid — <span> is properly closed; zero diagnostics expected.
var r2 = /*lang=html*/ "<span>OK</span>";
