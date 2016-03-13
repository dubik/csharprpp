public class RppConsole
{
    public static RppConsole _instance = new RppConsole();

    public void println(object obj)
    {
        System.Console.WriteLine(obj);
    }

    public void printFormat(string format, params object[] args)
    {
        System.Console.WriteLine(format, args);
    }
}