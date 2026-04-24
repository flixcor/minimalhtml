// SYNTAX TEST "source.cs" "holes inside HTML attribute values carry meta.interpolation.cs"
// Covers the html-interp-holes.injection grammar which re-applies hole patterns
// inside string.quoted.double.html and string.quoted.single.html scopes that
// are owned by text.html.basic (without the injection, holes are swallowed as
// plain HTML attribute string content).

class AttrHoles
{
    // N=1: hole in single-quoted HTML attribute value ($"..." interp-regular).
    string b = /*lang=html*/$"<a href='{url}'>text</a>";
//                                      ^ meta.interpolation.cs

    // N=1: hole in double-quoted HTML attribute value ($"""...""" raw).
    string c = /*lang=html*/$"""<a href="{url}">text</a>""";
//                                       ^ meta.interpolation.cs

    // N=2: hole in double-quoted HTML attribute value ($$"""...""" raw).
    string d = /*lang=html*/$$"""<a href="{{url}}">text</a>""";
//                                        ^ meta.interpolation.cs

    // N=3: hole in double-quoted HTML attribute value ($$$"""...""" raw).
    string e = /*lang=html*/$$$"""<a href="{{{url}}}">text</a>""";
//                                         ^ meta.interpolation.cs

    // N=1: hole in HTML tag attribute position (not inside a quoted value).
    // The C# string arg inside the hole must not be tokenised as illegal HTML.
    string f = /*lang=html*/$"""<div {SomeMethod("data-attr")}></div>""";
//                                   ^ meta.interpolation.cs
//                                                 ^ source.cs
}
