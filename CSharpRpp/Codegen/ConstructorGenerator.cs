using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace CSharpRpp.Codegen
{
    internal class ConstructorGenerator
    {
        public static void GenerateFields(IEnumerable<KeyValuePair<RppClass, TypeBuilder>> classes)
        {
            foreach (var pair in classes)
            {
                RppClass clazz = pair.Key;
                TypeBuilder typeBuilder = pair.Value;
                foreach (var field in clazz.Fields)
                {
                    FieldBuilder builder = typeBuilder.DefineField(field.Name, field.RuntimeType, FieldAttributes.Public);
                    field.Builder = builder;
                }
            }
        }

        public static void GenerateConstructors(IEnumerable<KeyValuePair<RppClass, TypeBuilder>> classes)
        {
            foreach (var pair in classes)
            {
                RppClass clazz = pair.Key;
                TypeBuilder typeBuilder = pair.Value;
                var fieldsType = clazz.Fields.Select(f => f.RuntimeType);
                ConstructorBuilder builder = GenerateConstructor(typeBuilder, fieldsType);
                clazz.Constructor.ConstructorBuilder = builder;
                AssignConstructorParamIndex(clazz.Constructor);

                ILGenerator body = builder.GetILGenerator();
                ClrCodegen codegen = new ClrCodegen(body);
                clazz.Constructor.Expr.Accept(codegen);
                body.Emit(OpCodes.Ret);
            }
        }

        private static void AssignConstructorParamIndex(IRppFunc constructor)
        {
            int index = 1;
            foreach (var param in constructor.Params)
            {
                param.Index = index++;
            }
        }

        private static ConstructorBuilder GenerateConstructor(TypeBuilder typeBuilder, IEnumerable<Type> fieldsType)
        {
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, fieldsType.ToArray());
            return constructorBuilder;
        }
    }
}