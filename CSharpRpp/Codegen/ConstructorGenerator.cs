using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace CSharpRpp.Codegen
{
    class ConstructorGenerator
    {
        public static void GenerateFields(IEnumerable<KeyValuePair<RppClass, TypeBuilder>> classes)
        {
            foreach (var pair in classes)
            {
                RppClass clazz = pair.Key;
                TypeBuilder typeBuilder = pair.Value;
                foreach (var field in clazz.Fields)
                {
                    FieldBuilder builder = typeBuilder.DefineField(field.Name, field.Type.Runtime, FieldAttributes.Public);
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
                var fieldsType = clazz.ClassParams.Select(f => f.Type.Runtime);
                ConstructorBuilder builder = GenerateConstructor(typeBuilder, fieldsType);
                clazz.Constructor.ConstructorBuilder = builder;
                AssignConstructorParamIndex(clazz.Constructor);

                ILGenerator body = builder.GetILGenerator();
                ClrCodegen codegen = new ClrCodegen(body);
                clazz.Constructor.Expr.Accept(codegen);
                body.Emit(OpCodes.Ret);

                clazz.Constructors.ForEach(c => CreateConstructor(typeBuilder, c));
            }
        }

        private static void CreateConstructor(TypeBuilder type, IRppFunc constructor)
        {
            Type[] paramTypes = ParamTypes(constructor.Params);
            ConstructorBuilder constructorBuilder = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, paramTypes);
            var body = constructorBuilder.GetILGenerator();
            ClrCodegen codegen = new ClrCodegen(body);
            constructor.Expr.Accept(codegen);
            body.Emit(OpCodes.Ret);
        }

        private static Type[] ParamTypes([NotNull] IEnumerable<IRppParam> paramList)
        {
            return paramList.Select(param => param.Type.Runtime).ToArray();
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