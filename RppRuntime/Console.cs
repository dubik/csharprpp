using System;

class Console
{
    public static Console _instance = new Console();

    public void println(string obj)
    {
        System.Console.WriteLine(obj);
    }

    public void printFormat(string format, params object[] args)
    {
        System.Console.WriteLine(format, args);
    }
}