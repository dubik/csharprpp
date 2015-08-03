using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

                // Constructor may call each other, so we should create stub first, and the generate code
                clazz.Constructors.ForEach(c => CreateConstructorStubs(typeBuilder, c));
                clazz.Constructors.ForEach(CreateConstructorBody);
            }
        }


        private static void CreateConstructorStubs(TypeBuilder type, IRppFunc constructor)
        {
            var constructorParams = constructor.Params;
            Type[] paramTypes = ParamTypes(constructorParams);
            ConstructorBuilder constructorBuilder = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, paramTypes);
            constructor.ConstructorInfo = constructorBuilder;

            DefineParams(constructorParams, constructorBuilder);
            AssignConstructorParamIndex(constructor);
        }

        private static void DefineParams(IEnumerable<IRppParam> constructorParams, ConstructorBuilder constructorBuilder)
        {
            int index = 1;
            foreach (var param in constructorParams)
            {
                constructorBuilder.DefineParameter(index++, ParameterAttributes.None, RppClass.StringConstructorArgName(param.Name));
            }
        }

        private static void CreateConstructorBody(IRppFunc constructor)
        {
            ConstructorBuilder builder = constructor.ConstructorInfo as ConstructorBuilder;
            Debug.Assert(builder != null, "builder != null");
            var body = builder.GetILGenerator();
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
    }
}