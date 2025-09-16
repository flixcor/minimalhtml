using Microsoft.CodeAnalysis;

namespace MinimalForms.ModelGenerator
{
    public class DefaultClasses
    {
        internal static void Register(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "ZodNet.ZodAttribute.g.cs", """
                namespace ZodNet;
                [AttributeUsage(AttributeTargets.Field)]
                public class ZodAttribute: Attribute{}
                """
                            ));

            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "ZodNet.ZodExtensions.g.cs", """
                #nullable enable
                namespace ZodNet.Extensions;
                using System.Buffers;
                internal static class ZodExtensions
                {
                    public static bool TryCopyAndMove(this string part, ref Span<char> name, ref int charsWritten) => part.AsSpan().TryCopyAndMove(ref name, ref charsWritten);

                    public static bool TryCopyAndMove<T>(this ReadOnlySpan<T> source, ref Span<T> target, ref int charsWritten)
                    {
                        if (!source.TryCopyTo(target)) return false;
                        charsWritten += source.Length;
                        target = target[source.Length..];
                        return true;
                    }

                    public static bool TryFormatAndMove<T>(this T source, ref Span<char> target, ref int charsWritten) where T : ISpanFormattable
                    {
                        if (!source.TryFormat(target, out var written, default, default)) return false;
                        charsWritten += written;
                        target = target[written..];
                        return true;
                    }

                    public static bool TryFormatAndMove<T>(this T source, ref Span<byte> target, ref int charsWritten) where T : IUtf8SpanFormattable
                    {
                        if (!source.TryFormat(target, out var written, default, default)) return false;
                        charsWritten += written;
                        target = target[written..];
                        return true;
                    }
                        
                    public static bool Grow<T>(ref Span<T> chars, ref T[]? borrowed)
                    {
                        const int MaxLength = 4096;
                        if (chars.Length >= MaxLength)
                        {
                            return false;
                        }
                        if(borrowed != null)
                        {
                            ArrayPool<T>.Shared.Return(borrowed);
                        }
                        borrowed = ArrayPool<T>.Shared.Rent(chars.Length * 2);
                        chars = borrowed;
                        return true;
                    }
                }
                #nullable disable
                """
                ));
        }
    }
}
