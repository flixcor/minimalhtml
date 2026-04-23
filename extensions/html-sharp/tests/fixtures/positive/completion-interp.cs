// Completion fixtures: interpolation hole exclusion
string a = /*lang=html*/ $"<p>{name}</p>";
string b = Html($"<a href=\"{url}\">link</a>");
