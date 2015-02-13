using System;

namespace RppRuntime
{
    // ReSharper disable InconsistentNaming
    public class Runtime
    {
        public int _field;
        public static void println(string line)
        {
            if (line != null)
            {
                Console.WriteLine(line);
            }
            else
            {
                Console.WriteLine("Moika");
            }
        }
    }

    public class Foor
    {
        public int _field;

        public int calculate(int k)
        {
            return _field + k;
        }
    }

    // ReSharper restore InconsistentNaming
}