using System;

// ReSharper disable InconsistentNaming

public class Runtime
{
    public static void println(string line)
    {
        Console.WriteLine(line);
    }

    public static void printFormat(string format, params object[] args)
    {
        Console.WriteLine(format, args);
    }
}

// ReSharper restore InconsistentNaming