using System;

public class ValueRef<T> where T : struct
{
    public Func<T> Getter;
    public Action<T> Setter;

    public T Value { get => Getter(); set => Setter(value); }

    public ValueRef(Func<T> InGetter, Action<T> InSetter)
    {
        Getter = InGetter;
        Setter = InSetter;
    }
}

public class ConstValueRef<T> where T : struct
{
    public Func<T> Getter;

    public T Value => Getter();

    public ConstValueRef(Func<T> InGetter)
    {
        Getter = InGetter;
    }
}
