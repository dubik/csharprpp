using System;
using System.Reflection;

namespace CSharpRppTest
{
    internal static class ObjectExtension
    {
        public static object GetPropertyValue(this object o, string name)
        {
            PropertyInfo propertyInfo = o.GetType().GetProperty(name);
            if (propertyInfo == null)
            {
                throw new Exception($"Property {name} is missing");
            }

            return propertyInfo.GetValue(o);
        }
    }
}