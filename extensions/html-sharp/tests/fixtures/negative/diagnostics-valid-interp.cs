// Fixture: negative/diagnostics-valid-interp.cs
// Valid interpolated HTML — zero diagnostics expected.
// Holes {cls} and {text} are blanked before validation; must not cause false positives.
var x = $"<div class='{nameof(x)}'><p>{nameof(x)}</p></div>";
