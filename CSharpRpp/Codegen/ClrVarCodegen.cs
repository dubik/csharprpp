using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CSharpRpp.Codegen
{
    internal class ClrVarCodegen
    {
        public static void Declare(RppVar node, ILGenerator body)
        {
            Declare(node, body, false);
        }

        public static void DeclareAndInitialize(RppVar node, ILGenerator body)
        {
            Declare(node, body, true);
        }

        /// <summary>
        /// If var is primitive, then instance of <c>Ref&lt;primitiveType&gt;</c> is created. If
        /// var is class, then <c>Ref&lt;object&gt;</c> is created because <c>Reflection.Emit</c> doesn't let
        /// me create instance of generic type with type builder as parameters (or actually it let's me create
        /// but I can't get constructors). So we have to cast everytime we load value from <c>elem</c>.
        /// </summary>
        private static void Declare(RppVar node, ILGenerator body, bool initialize)
        {
            Type varType = node.Type.Value.NativeType;

            if (IsWrappedVar(node))
            {
                Type elementType = varType;
                varType = GetRefType(varType);
                ConstructorInfo constructor = initialize ? GetSpecificConstructor(varType, elementType) : varType.GetConstructor(Type.EmptyTypes);
                Debug.Assert(constructor != null, "constructor != null");

                body.Emit(OpCodes.Newobj, constructor);

                initialize = true;
            }

            node.Builder = body.DeclareLocal(varType);
            node.Builder.SetLocalSymInfo(node.Name);

            if (initialize)
            {
                body.Emit(OpCodes.Stloc, node.Builder);
            }
        }

        private static ConstructorInfo GetSpecificConstructor(Type varType, Type elementType)
        {
            return varType.GetConstructor(new[] {GetRefElementType(elementType)});
        }

        /// <summary>
        /// Returns <c>object</c> for those elements which are classes, otherwise it's the same as input <c>elementType</c>.
        /// </summary>
        private static Type GetRefElementType(Type elementType)
        {
            Type refElementType = elementType;
            if (elementType.IsClass)
            {
                refElementType = typeof (object);
            }

            return refElementType;
        }

        public static void Load(RppVar node, ILGenerator body, Dictionary<LocalBuilder, FieldBuilder> capturedVars)
        {
            if (node.IsCaptured)
            {
                LoadRef(node, body, capturedVars);
                LoadValFromRef(node, body);
            }
            else
            {
                body.Emit(OpCodes.Ldloc, node.Builder);
            }
        }

        private static void LoadValFromRef(RppVar node, ILGenerator body)
        {
            Type varType = node.Type.Value.NativeType;

            body.Emit(OpCodes.Ldfld, GetRefElemField(GetRefType(varType)));
            if (varType.IsClass)
            {
                body.Emit(OpCodes.Castclass, varType);
            }
        }

        private static void LoadRef(RppVar node, ILGenerator body, IReadOnlyDictionary<LocalBuilder, FieldBuilder> capturedVars)
        {
            if (capturedVars != null)
            {
                FieldBuilder field = capturedVars[node.Builder];
                body.Emit(OpCodes.Ldarg_0);
                body.Emit(OpCodes.Ldfld, field);
            }
            else
            {
                // When we access captured variable not from a closure, we need to load it almost as if we would do it from a closure
                body.Emit(OpCodes.Ldloc, node.Builder);
            }
        }

        public static void Store(RppVar node, ILGenerator body, Dictionary<LocalBuilder, FieldBuilder> capturedVars)
        {
            if (node.IsCaptured)
            {
                // TODO this is not very optimal, we save what's on stack to local var, loading this.refField and then
                // loading temp var and storing then to this.refField.elem
                // This is needed because clr want's to have this, field, value -> on stack
                Type varType = node.Type.Value.NativeType;
                LocalBuilder tempVar = body.DeclareLocal(varType);
                body.Emit(OpCodes.Stloc, tempVar);

                LoadRef(node, body, capturedVars);

                body.Emit(OpCodes.Ldloc, tempVar);

                body.Emit(OpCodes.Stfld, GetRefElemField(GetRefType(varType)));
            }
            else
            {
                body.Emit(OpCodes.Stloc, node.Builder);
            }
        }

        public static Type GetRefType(Type varType)
        {
            return typeof (Ref<>).MakeGenericType(GetRefElementType(varType));
        }

        private static FieldInfo GetRefElemField(Type refType)
        {
            return refType.GetField("elem");
        }

        private static bool IsWrappedVar(RppVar node)
        {
            return node.IsCaptured;
        }
    }
}