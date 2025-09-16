namespace MinimalForms.ModelGenerator.Utility
{
    public static class Casing
    {
        public static string PascalToCamel(ReadOnlySpan<char> input)
        {
            Span<char> output = stackalloc char[input.Length];
            output[0] = char.ToLower(input[0]);
            input.Slice(1).CopyTo(output.Slice(1));
            return output.ToString();
        }
    }
}
