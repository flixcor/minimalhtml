namespace MinimalHtml.SourceGenerator
{
    internal readonly record struct CacheMethod
    {
        public readonly string MethodName;
        public readonly string ClassName;
        public readonly string Namespace;

        public CacheMethod(string methodName, string className, string @namespace)
        {
            MethodName = methodName;
            ClassName = className;
            Namespace = @namespace;
        }
    }
}
