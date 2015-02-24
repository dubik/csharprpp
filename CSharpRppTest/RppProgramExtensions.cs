using System.Linq;
using CSharpRpp;

namespace CSharpRppTest
{
    static class RppProgramExtensions
    {
        public static T First<T>(this RppProgram program, string name) where T : class, IRppNamedNode
        {
            AstNodeMatcher<T> astNodeMatcher = new AstNodeMatcher<T>(name);
            program.Accept(astNodeMatcher);
            return astNodeMatcher.Matches.First();
        }
    }
}