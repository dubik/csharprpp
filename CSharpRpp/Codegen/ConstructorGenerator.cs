using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace CSharpRpp.Codegen
{
    internal class ConstructorGenerator
    {
        public static void GenerateFields(IEnumerable<KeyValuePair<RppClass, TypeBuilder>>  classes)
        {
            foreach (var pair in classes)
            {
                RppClass clazz = pair.Key;
                TypeBuilder typeBuilder = pair.Value;
                //FieldBuilder builder = typeBuilder.DefineField("", typeof (int), FieldAttributes.Public);
            }
        }

        public static void GenerateConstructors(IEnumerable<KeyValuePair<RppClass, TypeBuilder>> classes)
        {
            foreach (var pair in classes)
            {
                RppClass clazz = pair.Key;
                TypeBuilder typeBuilder = pair.Value;
                ConstructorBuilder builder = GenerateConstructor(typeBuilder);
                ILGenerator body = builder.GetILGenerator();
                ClrCodegen codegen = new ClrCodegen(body);
                clazz.Constructor.Expr.Accept(codegen);
                body.Emit(OpCodes.Ret);
            }
        }

        private static ConstructorBuilder GenerateConstructor(TypeBuilder typeBuilder)
        {
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            return constructorBuilder;
        }
    }
}