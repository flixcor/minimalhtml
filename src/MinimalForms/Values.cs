using System.Collections;

namespace MinimalForms;

public readonly struct Values<T> : IReadOnlyList<T> where T : struct
{
    private readonly (T, T, T) _tuple;
    private readonly List<T>? _list;
    private readonly bool _isOne;
    private readonly bool _isTwo;
    private readonly bool _isThree;

    public static readonly Values<T> Empty = default;

    public int Count => _isOne ? 1 : _isTwo ? 2 : _isThree ? 3 : _list?.Count ?? 0;

    public T this[int index] => index switch
    {
        0 when _isOne || _isTwo || _isThree => _tuple.Item1,
        1 when _isTwo || _isThree => _tuple.Item2,
        2 when _isThree => _tuple.Item3,
        _ when _list?.Count > index => _list[index],
        _ => throw new IndexOutOfRangeException(nameof(index) + index),
    };

    public Enumerator GetEnumerator() => new(this);

    private Values(T one)
    {
        _tuple = (one, default, default);
        _isOne = true;
    }

    private Values((T, T) two)
    {
        _tuple = (two.Item1, two.Item2, default);
        _isTwo = true;
    }

    private Values((T, T, T) three)
    {
        _tuple = three;
        _isThree = true;
    }

    private Values(List<T> values)
    {
        _list = values;
    }

    public Values<T> Add(T value)
    {
        if (_isOne) return new((_tuple.Item1!, value));
        if (_isTwo) return new((_tuple.Item1!, _tuple.Item2!, value));
        if (_isThree) return new(new List<T>(10) { _tuple.Item1!, _tuple.Item2!, _tuple.Item3!, value });
        if (_list != null)
        {
            _list.Add(value);
            return this;
        }
        return new(value);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(Values<T> item) : IEnumerator<T>
    {
        private int _index;
        public T Current { get; private set; }

        readonly T IEnumerator<T>.Current => Current;

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (item.Count <= _index)
            {
                return false;
            }

            Current = item[_index];
            _index++;
            return true;
        }

        readonly void IDisposable.Dispose()
        {
        }

        bool IEnumerator.MoveNext() => MoveNext();

        void IEnumerator.Reset()
        {
            _index = 0;
        }
    }
}
