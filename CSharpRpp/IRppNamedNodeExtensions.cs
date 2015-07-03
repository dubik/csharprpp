namespace CSharpRpp
{
    static class RppNamedNodeExtensions
    {
        public static bool IsObject(this IRppNamedNode node)
        {
            RppClass clazz = node as RppClass;
            return clazz != null && clazz.Kind == ClassKind.Object;
        }

        public static bool IsFunction(this IRppNamedNode node)
        {
            return node is RppFunc;
        }
    }
}