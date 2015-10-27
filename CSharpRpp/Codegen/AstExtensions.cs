namespace CSharpRpp.Codegen
{
    static class AstExtensions
    {
        public static string GetNativeName(this RppClass clazz)
        {
            return clazz.Kind == ClassKind.Object ? Symbols.SymbolTable.GetObjectName(clazz.Name) : clazz.Name;
        }
    }
}