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

        private static void Declare(RppVar node, ILGenerator body, bool initialize)
        {
            Type varType = node.Type.Value.NativeType;

            if (IsCapturedVar(node))
            {
                Type elementType = varType;
                varType = GetRefType(varType);
                ConstructorInfo constructor = initialize ? varType.GetConstructor(new[] {elementType}) : varType.GetConstructor(Type.EmptyTypes);
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

        public static void Load(RppVar node, ILGenerator body, Dictionary<LocalBuilder, FieldBuilder> capturedVars)
        {
            if (IsCapturedVar(node))
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

                body.Emit(OpCodes.Ldfld, GetRefElemField(GetRefType(node.Type.Value.NativeType)));
            }
            else
            {
                body.Emit(OpCodes.Ldloc, node.Builder);
            }
        }

        public static void Store(RppVar node, ILGenerator body, Dictionary<LocalBuilder, FieldBuilder> capturedVars)
        {
            if (IsCapturedVar(node))
            {
                // TODO this is not very optimal, we save what's on stack to local var, loading this.refField and then
                // loading temp var and storing then to this.refField.elem
                // This is needed because clr want's to have this, field, value -> on stack
                Type varType = node.Type.Value.NativeType;
                LocalBuilder tempVar = body.DeclareLocal(varType);
                body.Emit(OpCodes.Stloc, tempVar);

                if (capturedVars != null)
                {
                    FieldBuilder field = capturedVars[node.Builder];
                    body.Emit(OpCodes.Ldarg_0);
                    body.Emit(OpCodes.Ldfld, field);
                }
                else
                {
                    // When capturedVars is null it means we are not in a closure, so have to load local var
                    body.Emit(OpCodes.Ldloc, node.Builder);
                }

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
            if (!varType.IsPrimitive)
            {
                throw new ArgumentException("expected only primitive types, non primitive shouldn't need to be passed as references", nameof(varType));
            }

            return typeof (Ref<>).MakeGenericType(varType);
        }

        private static FieldInfo GetRefElemField(Type refType)
        {
            return refType.GetField("elem");
        }

        private static bool IsCapturedVar(RppVar node)
        {
            return node.IsCaptured && node.Type.Value.IsPrimitive;
        }
    }
}