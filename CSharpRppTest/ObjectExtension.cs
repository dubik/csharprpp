namespace CSharpRppTest
{
    internal static class ObjectExtension
    {
        public static object GetFieldValue(this object o, string name)
        {
            return o.GetType().GetField(name).GetValue(o);
        }
    }
}
