namespace CSharpRpp
{
    public class Program
    {
        private static void Main()
        {
            const string code = @"

class Foo[A] extends Function2[Int, A, Int]
{
    def apply(x: Int, y: A) : Int = x + 12
}

";

            RppCompiler compiler = new RppCompiler();
            compiler.CompileAndSave(code);
        }
    }
}