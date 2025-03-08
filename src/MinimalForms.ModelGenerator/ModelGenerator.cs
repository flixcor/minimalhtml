using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace ZodNet.ModelGenerator
{
    [Generator]
    public class ModelGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            DefaultClasses.Register(context);

            var modelProvider = ModelProvider.Register(context);

            context.RegisterSourceOutput(modelProvider, static (ctx, value) =>
            {
                var props = GetProps(new(value.Pairs.ToList())).ToList();
                var syntax = $$"""
                // generated at {{DateTime.Now}}
                /*{{string.Join("\n", props)}}*/
                #nullable enable
                using ZodNet.Extensions;
                using Microsoft.Extensions.Primitives;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Collections;
                using System.Buffers;
                using System;
                using System.Text;
                using System.Text.Encodings.Web;
                using System.Text.Json;
                using System.Text.Json.Serialization;
                using MinimalForms;
                using ByteValues = MinimalForms.Values<System.ReadOnlyMemory<byte>>;
                namespace {{value.Namespace}};
                {{value.Accessibility}} partial class {{value.Class}}JsonConverter: JsonConverter<{{value.Class}}>
                {
                    public override {{value.Class}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                    {
                        throw new NotImplementedException();
                    }

                    public override void Write(Utf8JsonWriter writer, {{value.Class}} value, JsonSerializerOptions options)
                    {
                        writer.WriteStartObject();{{string.Join("", props.Select(p => $"\n        value.{p.SafeName}.Write(writer);"))}}
                        writer.WriteEndObject();
                    }
                }
                {{value.Accessibility}} partial class {{value.Class}}
                {{{string.Join("", props.Select(p => $$"""

                    {{GetPropDeclaration(p, [])}}
                """))}}

                    public static async ValueTask<{{value.Class}}?> BindAsync(HttpContext context)
                    {
                        if(!context.Request.HasFormContentType) return null;
                        var form = await context.Request.BodyReader.GetFormDictionary(context.Request.ContentType, context.Request.ContentLength, context.RequestAborted);
                        context.Response.RegisterForDispose(form);
                        return Bind(form);
                    }

                    public static ValueTask<{{value.Class}}?> BindAsync(HttpContext context, System.Reflection.ParameterInfo parameter) => BindAsync(context);

                    public static {{value.Class}} Bind(FormDictionary form)
                    {
                        return new {{value.Class}}
                        {
                            {{string.Join("""
                            
                            
                """, props.Select(p => GetInitializer(p)))}}
                        };
                    }
                    {{string.Join("\n\n", props.Select(p => GetPropStruct(value, p, [])))}}
                }
                #nullable disable
                """;
                ctx.AddSource(string.Join(".", value.Namespace, value.Class, "props", "generated"), syntax);
            });
        }

        private static string GetPropStruct(ConfigDeclaration value, GeneratedProp p, string[] namePrefixes)
        {
            var recurringGroupLevel = namePrefixes.Length;

            if (p.Type == "OfObjects")
            {
                return GetRecurringGroup(value, p, namePrefixes);

            }

            if (p.Type == "Object")
            {
                return GetGroup(value, p, namePrefixes);
            }

            var type = Type(p);

            var ordinalType = recurringGroupLevel switch
            {
                1 => "int",
                > 1 => $"({string.Join(",", Enumerable.Range(0, recurringGroupLevel).Select(x => "int"))})",
                _ => ""
            };

            var valuesType = p.Type.Contains("File") ? "Values<MinimalForms.FormFile>" : "ByteValues";

            return $$"""
                
            {{value.Accessibility}} readonly struct {{p.SafeName}}Type
            {
                private readonly bool _isForm;
                private readonly {{valuesType}} _values;
                {{(p.Type.Contains("File") ? "" : $"private readonly {type} _value;")}}
                {{(recurringGroupLevel > 0 ? $"private readonly {ordinalType} _ordinal;" : "")}}

                public {{p.SafeName}}Type({{(recurringGroupLevel > 0 ? $"{ordinalType} ordinal, " : "")}}{{valuesType}} values)
                {{{(recurringGroupLevel > 0 ? """

                    _ordinal = ordinal;
            """ : "")}}
                    _values = values;
                    _isForm = true;
                }
            {{(p.Type.Contains("File") ? "" : $$"""

                private {{p.SafeName}}Type({{type}} value)
                {
                    _value = value;
                }
            """)}}
            
            {{(recurringGroupLevel > 0 ? $$"""

                public {{p.SafeName}}Type({{p.SafeName}}Type value, {{ordinalType}} ordinal)
                {
                    _ordinal = ordinal;
                    _value = value._value;
                    _isForm = value._isForm;
                    _values = value._values;
                }

            """ : "")}}
                {{(p.Type.Contains("File") ? "" : $"public static implicit operator {p.SafeName}Type({type} value) => new(value);")}}

                {{(p.Type == "Number" ? $$"""
                public static implicit operator int({{p.SafeName}}Type number) => number._isForm ? int.Parse(number._values[0].Span) : (int)number._value!.Value;
                public static implicit operator decimal({{p.SafeName}}Type number) => number._isForm ? decimal.Parse(number._values[0].Span) : number._value!.Value;
                public static implicit operator double({{p.SafeName}}Type number) => number._isForm ? double.Parse(number._values[0].Span) : (double)number._value!.Value;
                public static implicit operator int?({{p.SafeName}}Type number) => number._isForm ? number._values.Count > 0 && int.TryParse(number._values[0].Span, out var x) ? x : null : (int?)number._value;
                public static implicit operator decimal?({{p.SafeName}}Type number) => number._isForm ? number._values.Count > 0 && decimal.TryParse(number._values[0].Span, out var x) ? x : null : number._value;
                public static implicit operator double?({{p.SafeName}}Type number) => number._isForm ? number._values.Count > 0 && double.TryParse(number._values[0].Span, out var x) ? x : null : (double?)number._value;
                public void Write(Utf8JsonWriter writer)
                {
                    writer.WritePropertyName("{{p.CamelCase}}"u8);
                    if (_values.Count > 0)
                    {
                        var span = _values[0].Span;
                        if(!span.IsEmpty)
                        {
                            writer.WriteRawValue(span);
                            return;
                        }
                    }
                    if (_value.HasValue)
                    {
                        writer.WriteNumberValue(_value.GetValueOrDefault());
                    }
                    else 
                    {
                        writer.WriteNullValue();
                    }
                }
            """ : p.Type == "Bool" ? $$"""
                public static implicit operator bool({{p.SafeName}}Type v) => v._isForm ? v._values.Count > 0 && (v._values[0].Span.SequenceEqual("on"u8) || v._values[0].Span.SequenceEqual("true"u8) || v._values[0].Span.SequenceEqual("True"u8)) : v._value;
                public void Write(Utf8JsonWriter writer)
                {
                    writer.WritePropertyName("{{p.CamelCase}}"u8);
                    bool? value = this;
                    if (value.HasValue)
                    {
                        writer.WriteBooleanValue(value.GetValueOrDefault());
                    }
                    else 
                    {
                        writer.WriteNullValue();
                    }
                }
            """ : p.Type == "String" ? $$"""
                public void Write(Utf8JsonWriter writer)
                {
                    writer.WritePropertyName("{{p.CamelCase}}"u8);
                    if (_values.Count > 0)
                    {
                        writer.WriteStringValue(_values[0].Span);
                    }
                    else
                    {
                        writer.WriteStringValue(_value);
                    }
                }
                public static implicit operator string({{p.SafeName}}Type v) => v._isForm ? Encoding.UTF8.GetString(v._values[0].Span) : v._value ?? "";
            """ : p.Type == "OfStrings" ? $$"""
                public static implicit operator string[]({{p.SafeName}}Type v) => v._isForm ? v._values : v._value ?? [];
                public bool Contains(string value) => _isForm ? _values.Contains(Encoding.UTF8.GetBytes()) : _value != null && _value.Contains(value); 
                public void Write(Utf8JsonWriter writer)
                {
                    writer.WritePropertyName("{{p.CamelCase}}"u8);
                    writer.WriteStartArray();

                    if (_isForm)
                    {
                        foreach (var rawValue in _values)
                        {
                            writer.WriteStringValue(rawValue.Span);
                        }
                    }
                    else 
                    {
                        foreach (var value in _value)
                        {
                            writer.WriteStringValue(value);
                        }
                    }
                    writer.WriteEndArray();
                }
            """ : p.Type == "OfNumbers" ? $$"""
                public static implicit operator decimal[]({{p.SafeName}}Type v) => v._isForm ? v._values.Select(x=> x.IsEmpty ? null : (decimal?)decimal.Parse(x.Span)).Where(x=> x != null).Select(x=> x.GetValueOrDefault()).ToArray() : v._value ?? [];
                public bool Contains(decimal value) => _isForm ? _values.Any(v => decimal.TryParse(v.Span, out var y) && y == value) : _value != null && _value.Contains(value); 
                public void Write(Utf8JsonWriter writer)
                {
                    writer.WritePropertyName("{{p.CamelCase}}"u8);
                    writer.WriteStartArray();
            
                    if (_isForm)
                    {
                        foreach (var rawValue in _values)
                        {
                            var span = rawValue.Span;
                            if(!span.IsEmpty)
                            {
                                writer.WriteRawValue(span);
                            }
                        }
                    }
                    else 
                    {
                        foreach (var value in _value)
                        {
                            writer.WriteNumberValue(value);
                        }
                    }
                    writer.WriteEndArray();
                }
            """ : p.Type == "File" ? $$"""
                public bool HasFile => _values.Count > 0;
                public MinimalForms.FormFile File => _values[0];
                public void Write(Utf8JsonWriter writer)
                {
                    writer.WritePropertyName("{{p.CamelCase}}"u8);
                    if(!HasFile)
                    {
                        writer.WriteNullValue();
                        return;
                    }
                    var file = File;
                    writer.WriteStartObject();
                    writer.WriteString("fileName"u8, file.FileName.Span);
                    writer.WriteString("contentType"u8, file.ContentType.Span);
                    writer.WriteNumber("length"u8, file.Length);
                    writer.WriteEndObject();
                }
            """ : p.Type == "OfFiles" ? $$"""
                public Values<MinimalForms.FormFile> Files => _values;
                public void Write(Utf8JsonWriter writer)
                {
                    writer.WritePropertyName("{{p.CamelCase}}"u8);
                    writer.WriteStartArray();
                    foreach(var file in _values)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("fileName"u8, file.FileName.Span);
                        writer.WriteString("contentType"u8, file.ContentType.Span);
                        writer.WriteNumber("length"u8, file.Length);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }
            """ : "")}}

                public static bool TryWriteName(Span<char> name, {{(recurringGroupLevel > 0 ? $"{ordinalType} ordinal, " : "")}}out int charsWritten)
                {
                    charsWritten = 0;
                    {{(recurringGroupLevel > 0 ? $"""
                    return {string.Join("", namePrefixes.Select((n, i) => $"""
                    "{n}[".TryCopyAndMove(ref name, ref charsWritten) &&
                                ordinal{(recurringGroupLevel > 1 ? $".Item{i + 1}" : "")}.TryFormatAndMove(ref name, ref charsWritten) &&
                    """))}
                                 "].{p.SafeName}".TryCopyAndMove(ref name, ref charsWritten);
                    """ : $"""return "{p.SafeName}".TryCopyAndMove(ref name, ref charsWritten);""")}}
                }

                public static bool TryWriteName(Span<byte> name, {{(recurringGroupLevel > 0 ? $"{ordinalType} ordinal, " : "")}}out int charsWritten)
                {
                    charsWritten = 0;
                    {{(recurringGroupLevel > 0 ? $"""
                    return {string.Join("", namePrefixes.Select((n, i) => $"""
                    "{n}["u8.TryCopyAndMove(ref name, ref charsWritten) &&
                                ordinal{(recurringGroupLevel > 1 ? $".Item{i + 1}" : "")}.TryFormatAndMove(ref name, ref charsWritten) &&
                    """))}
                                "].{p.SafeName}"u8.TryCopyAndMove(ref name, ref charsWritten);
                    """ : $"""return "{p.SafeName}"u8.TryCopyAndMove(ref name, ref charsWritten);""")}}
                }

                public NameStruct Name => new({{(recurringGroupLevel> 0 ? "_ordinal" : "")}});
                {{(p.Type == "String" || p.Type == "Number" ? "public ValueStruct Value => new(this);" : "")}}

                public readonly record struct NameStruct({{(recurringGroupLevel> 0 ? $"{ordinalType} Ordinal" : "")}}) : IUtf8SpanFormattable
                {
                    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryWriteName(utf8Destination{{(recurringGroupLevel > 0 ? ", Ordinal" : "")}}, out bytesWritten);
                }

                {{(p.Type != "String" && p.Type != "Number" ? "" : $$"""
                public readonly record struct ValueStruct(in {{p.SafeName}}Type Item) : IUtf8SpanFormattable
                {
                    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
                    {
                        if(Item._values.Count > 0) return HtmlEncoder.Default.EncodeUtf8(Item._values[0].Span, utf8Destination, out _, out bytesWritten) == OperationStatus.Done;
                        {{(p.Type == "String" 
                            ? "if(Item._value != null) return MinimalHtml.Helpers.TryWriteHtmlEscaped(Item._value, utf8Destination, out bytesWritten);"
                            : "if(Item._value.HasValue) return Item._value.GetValueOrDefault().TryFormat(utf8Destination, out bytesWritten, format, provider);")}}
                        bytesWritten = 0;
                        return true;
                    }
                }
            """)}}
            }
            """;
        }

        private static string GetRecurringGroup(ConfigDeclaration value, GeneratedProp p, string[] namePrefixes)
        {
            var recurringGroupLevel = namePrefixes.Length;
            string[] nextLevel = [.. namePrefixes, p.SafeName];
            var prefix = string.Join(".", nextLevel.Select(n => n + "Type"));
            var ordinalType = recurringGroupLevel switch
            {
                1 => "int",
                > 1 => string.Join(",", Enumerable.Range(0, recurringGroupLevel).Select(x => "int")),
                _ => ""
            };

            var ordinalDeclaration = ordinalType.Length > 0 ? $"\n    private readonly {ordinalType} _ordinal;" : "";

            var tupInitializer = recurringGroupLevel switch
            {
                1 => "(_ordinal, _index)",
                > 1 => $"({string.Join(",", Enumerable.Range(1, recurringGroupLevel).Select(x => "_ordinal.Item" + x))}, _index)",
                _ => "_index"
            };

            return $$"""
            [System.Runtime.CompilerServices.CollectionBuilder(typeof({{p.SafeName}}Enumerable), nameof(Create))]
            public readonly struct {{p.SafeName}}Enumerable: IEnumerable<{{p.SafeName}}Type>
            {{{ordinalDeclaration}}
                private readonly FormDictionary _lookup = FormDictionary.Empty;
                private readonly IEnumerable<{{p.SafeName}}Type>? _enumerable;

                public {{p.SafeName}}Enumerable({{(ordinalType.Length > 0 ? $"{ordinalType} ordinal, " : "")}}in FormDictionary lookup)
                {
                    {{(ordinalType.Length > 0 ? "_ordinal = ordinal;" : "")}}
                    _lookup = lookup;
                }

                public {{p.SafeName}}Enumerable(IEnumerable<{{p.SafeName}}Type> enumerable)
                {
                    _enumerable = enumerable;
                }

                public static {{p.SafeName}}Enumerable Create(ReadOnlySpan<{{p.SafeName}}Type> span) => new(span.ToArray());

            {{(ordinalType.Length > 0 ? $$"""
                public {{p.SafeName}}Enumerable(in {{p.SafeName}}Enumerable me, {{ordinalType}} ordinal)
                {
                    _enumerable = me._enumerable;
                    _ordinal = ordinal;
                    _lookup = me._lookup;
                }
            """ : "")}}

                public static implicit operator {{p.SafeName}}Enumerable(List<{{p.SafeName}}Type> e) => new(e);
                public static implicit operator {{p.SafeName}}Enumerable({{p.SafeName}}Type[] e) => new(e);
                public static implicit operator {{p.SafeName}}Enumerable(ImmutableArray<{{p.SafeName}}Type> e) => new(e);

                public Enumerator GetEnumerator() => _enumerable == null ? new({{(ordinalType.Length > 0 ? "_ordinal, " : "")}}_lookup) : new(_enumerable.GetEnumerator());

                IEnumerator<{{p.SafeName}}Type> IEnumerable<{{p.SafeName}}Type>.GetEnumerator() => GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                public void Write(Utf8JsonWriter writer)
                {
                    writer.WritePropertyName("{{p.CamelCase}}"u8);
                    writer.WriteStartArray();
                    foreach (var item in this)
                    {
                        writer.WriteStartObject();{{string.Join("", p.Children.Select(z => $"\n            item.{z.SafeName}.Write(writer);"))}}
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }

                public struct Enumerator : IEnumerator<{{p.SafeName}}Type>
                {
                    public Enumerator({{(ordinalType.Length > 0 ? $"{ordinalType} ordinal, " : "")}}in FormDictionary form)
                    {
                        {{(ordinalType.Length > 0 ? "_ordinal = ordinal;" : "")}}
                        _form = form;
                    }

                    public Enumerator(IEnumerator<{{p.SafeName}}Type> enumerator)
                    {
                        _enumerator = enumerator;
                    }

                    private int _index;{{ordinalDeclaration}}
                    private readonly FormDictionary? _form = FormDictionary.Empty;
                    private readonly IEnumerator<{{p.SafeName}}Type>? _enumerator;

                    public {{p.SafeName}}Type Current { get; private set; }

                    public bool MoveNext()
                    {
                        var tup = {{tupInitializer}};
                        if (_enumerator != null)
                        {
                            var result = _enumerator.MoveNext();
                            if(!result) return false;
                            Current = new()
                            {{{string.Join("", p.Children.Select(c => $"""
                            
                                {c.SafeName} = new(_enumerator.Current.{c.SafeName}, tup),
            """))}}
                            };
                            _index++;
                            return true;
                        }

                        if (_form == null || _form.Count == 0) return false;

                        Span<byte> chars = stackalloc byte[128];
                        byte[]? borrowed = null;
                        int written = 0;
                        
                        try
                        {{{string.Join("", p.Children.Select((c) => c.Type != "OfObjects" ? $$"""

                            while(!{{prefix}}.{{c.SafeName}}Type.TryWriteName(chars, tup, out written) && ZodExtensions.Grow(ref chars, ref borrowed)){}
                            var chars{{c.SafeName}} = chars.Slice(0, written);
                            var prop{{c.SafeName}}Found = _form.TryGet{{(c.Type.Contains("File") ? "File" : "Value")}}(chars{{c.SafeName}}, out var prop{{c.SafeName}});
            """ : $"""

                            var prop{c.SafeName} = new {prefix}.{c.SafeName}Enumerable(tup, _form);
                            var prop{c.SafeName}Found = prop{c.SafeName}.GetEnumerator().MoveNext();
            """))}}
            
                            if({{string.Join(" && ", p.Children.Select(c=> $"!prop{c.SafeName}Found"))}}) return false;
            
                            Current = new()
                            {{{string.Join("", p.Children.Select(c => c.Type != "OfObjects" ? $"""
                            
                                {c.SafeName} = new(tup, prop{c.SafeName}),
            """: $"""

                                {c.SafeName} = prop{c.SafeName}
            """))}}
                            };
            
                            _index++;
                            return true;
                        }
                        finally
                        {
                            if (borrowed != null)
                            {
                                ArrayPool<byte>.Shared.Return(borrowed);
                            }
                        }
                    }

                    readonly object IEnumerator.Current => Current;
                    readonly void IDisposable.Dispose() => _enumerator?.Dispose();
                    public void Reset()
                    {
                        _index = 0;
                        _enumerator?.Reset();
                    }
                }
            }
            {{GetGroup(value, p, nextLevel)}}
            """;
        }

        private static string GetGroup(ConfigDeclaration value, GeneratedProp p, string[] namePrefixes)
        {
            return $$"""
            {{value.Accessibility}} partial struct {{p.SafeName}}Type
            {{{string.Join("", p.Children.Select(p => $$"""

                    {{GetPropDeclaration(p, namePrefixes)}} 
                """))}}
            public void Write(Utf8JsonWriter writer)
            {
                writer.WritePropertyName("{{p.CamelCase}}"u8);
                writer.WriteStartObject();{{string.Join("", p.Children.Select(p => $"\n    {p.SafeName}.Write(writer);"))}}
                writer.WriteEndObject();
            }
            {{string.Join("", p.Children.Select(p => GetPropStruct(value, p, namePrefixes)))}}
            }
            """;
        }

        private static string GetPropDeclaration(GeneratedProp p, string[] namePrefixes)
        {
            if (p.Type == "OfObjects") return $$"""public {{p.SafeName}}Enumerable {{p.SafeName}} { get; init; }""";
            return $$"""public {{p.SafeName}}Type {{p.SafeName}} { get; init; }""";
        }

        private static string GetInitializer(GeneratedProp p, IEnumerable<string>? prefix = null)
        {
            prefix ??= [];
            var nextPrefix = prefix.Concat([p.SafeName]);
            if (p.Type == "Object") return $$"""
                {{p.SafeName}} = new()
                {
                    {{string.Join("\n", p.Children.Select(c => GetInitializer(c, nextPrefix)))}}
                },
                """;
            if (p.Type == "OfObjects") return $"""{p.SafeName} = new(form),""";
            var key = string.Join(".", nextPrefix);
            if (p.Type.Contains("File")) return $"""{p.SafeName} = new(form.TryGetFile("{key}"u8, out var values{p.SafeName}) ? values{p.SafeName} : default),""";
            return $"""{p.SafeName} = new(form["{key}"u8]),""";
        }

        private static string Type(GeneratedProp prop) => prop.Type switch
        {
            "Number" => "decimal?",
            "Bool" => "bool",
            "OfBools" => "bool[]?",
            "OfStrings" => "string[]?",
            "OfNumbers" => "decimal[]?",
            _ => "string?"
        };



        private static IEnumerable<GeneratedProp> GetProps(Queue<Pair> pairs, int level = 0)
        {
            string? name = null;
            string? type = null;
            GeneratedProp[]? children = null;
            var configurations = new List<Pair>();
            var first = true;
            var wasArray = false;

            while (pairs.Count > 0)
            {
                var pair = pairs.Dequeue();

                if (pair.Name == "Build")
                {
                    if (first)
                    {
                        yield break;
                    }
                    if (name is { } && type is { })
                    {
                        yield return new GeneratedProp(name, type, configurations.ToArray(), children ?? []);
                    }
                    configurations.Clear();
                    first = true;
                    children = null;
                    continue;
                }
                if (wasArray)
                {
                    type = pair.Name;
                    wasArray = false;
                }
                if (first)
                {
                    first = false;
                    wasArray = pair.Name == "Array";
                    if (pair.Arguments.Count > 0)
                    {
                        name = pair.Arguments.AsSpan()[0];
                        type = pair.Name;
                    }
                }

                if (pair.Name.AsSpan().Contains("object".AsSpan(), StringComparison.OrdinalIgnoreCase) && name != null && type != null)
                {
                    children = GetProps(pairs).ToArray();
                    yield return new GeneratedProp(name, type, configurations.ToArray(), children ?? []);
                    first = true;
                    configurations.Clear();
                    children = null;
                    wasArray = false;
                }
            }
        }
    }
}
