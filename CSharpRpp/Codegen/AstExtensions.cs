namespace CSharpRpp.Codegen
{
    static class AstExtensions
    {
        public static string GetNativeName(this RppClass clazz)
        {
            return clazz.Kind == ClassKind.Object ? RppScope.GetObjectName(clazz.Name) : clazz.Name;
        }
    }
}
