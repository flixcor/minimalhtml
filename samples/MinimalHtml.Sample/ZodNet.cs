namespace ZodNet
{
    public interface IZodRoot
    {
        IZodString<IZodRoot> String(string name);
        IZodNumber<IZodRoot> Number(string name);
        IZodArray<IZodRoot> Array(string name);
        IZodObject<IZodRoot> Object(string name);
        IZodBool<IZodRoot> Bool(string name);
        IZodFile<IZodRoot> File(string name);
        IZodRoot Build();
    }

    public interface IZodString<T>
    {
        IZodString<T> MinLength(int min);
        IZodString<T> MaxLength(int max);
        IZodString<T> Nullable();
        IZodString<T> Empty();
        T Build();
    }

    public interface IZodNumber<T>
    {
        IZodNumber<T> Min(decimal min);
        IZodNumber<T> Max(decimal max);
        IZodNumber<T> Nullable();
        T Build();
    }

    public interface IZodBool<T>
    {
        IZodBool<T> Nullable();
        T Build();
    }

    public interface IZodFile<T>
    {
        T Build();
    }

    public interface IZodArray<T>
    {
        IZodString<T> OfStrings();
        IZodNumber<T> OfNumbers();
        IZodObject<T> OfObjects();
        IZodFile<T> OfFiles();
    }

    public interface IZodObject<T>
    {
        IZodString<IZodObject<T>> String(string name);
        IZodNumber<IZodObject<T>> Number(string name);
        IZodArray<IZodObject<T>> Array(string name);
        IZodObject<IZodObject<T>> Object(string name);
        IZodBool<IZodObject<T>> Bool(string name);
        IZodFile<IZodObject<T>> File(string name);
        T Build();
    }
}
