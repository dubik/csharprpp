namespace CSharpRpp
{
    static class IRppNamedNodeExtensions
    {
        public static bool IsObject(this IRppNamedNode node)
        {
            RppClass clazz = node as RppClass;
            return clazz != null && clazz.Kind == ClassKind.Object;
        }
    }
}