// ReSharper disable InconsistentNaming

public interface Function0<out TResult>
{
    TResult apply();
}

public interface Function1<in T1, out TResult>
{
    TResult apply(T1 arg1);
}

public interface Function2<in T1, in T2, out TResult>
{
    TResult apply(T1 arg1, T2 arg2);
}

public interface Function3<in T1, in T2, in T3, out TResult>
{
    TResult apply(T1 arg1, T2 arg2, T3 arg3);
}

public interface Function4<in T1, in T2, in T3, in T4, out TResult>
{
    TResult apply(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
}

public interface Function5<in T1, in T2, in T3, in T4, in T5, out TResult>
{
    TResult apply(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
}

// ReSharper restore InconsistentNaming