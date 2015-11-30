using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CSharpRpp.TypeSystem;

namespace CSharpRpp.Codegen
{
    internal class ClrCodegenUtils
    {
        private static readonly Dictionary<short, OpCode> loadLocal = new Dictionary<short, OpCode>()
        {
            {0, OpCodes.Ldloc_0},
            {1, OpCodes.Ldloc_1},
            {2, OpCodes.Ldloc_2},
            {3, OpCodes.Ldloc_3}
        };

        private static readonly Dictionary<short, OpCode> storeLocal = new Dictionary<short, OpCode>()
        {
            {0, OpCodes.Stloc_0},
            {1, OpCodes.Stloc_1},
            {2, OpCodes.Stloc_2},
            {3, OpCodes.Stloc_3}
        };

        private static readonly Dictionary<int, OpCode> intConsts = new Dictionary<int, OpCode>()
        {
            {0, OpCodes.Ldc_I4_0},
            {1, OpCodes.Ldc_I4_1},
            {2, OpCodes.Ldc_I4_2},
            {3, OpCodes.Ldc_I4_3},
            {4, OpCodes.Ldc_I4_4},
            {5, OpCodes.Ldc_I4_5},
            {6, OpCodes.Ldc_I4_6},
            {7, OpCodes.Ldc_I4_7},
            {8, OpCodes.Ldc_I4_8}
        };

        private static readonly Dictionary<Type, OpCode> stElemOpCode = new Dictionary<Type, OpCode>()
        {
            {Types.Bool, OpCodes.Stelem_I1},
            {Types.Byte, OpCodes.Stelem_I1},
            {Types.Char, OpCodes.Stelem_I2},
            {Types.Short, OpCodes.Stelem_I2},
            {Types.Int, OpCodes.Stelem_I4},
            {Types.Long, OpCodes.Stelem_I8},
            {Types.Float, OpCodes.Stelem_R4},
            {Types.Double, OpCodes.Stelem_R8},
        };

        public static void LoadInt(int val, ILGenerator body)
        {
            EmitSpecific(val, intConsts, OpCodes.Ldc_I4, body);
        }

        public static void LoadLocal(LocalVariableInfo local, ILGenerator body)
        {
            EmitSpecific((short) local.LocalIndex, loadLocal, OpCodes.Ldloc, body);
        }

        public static void StoreLocal(LocalVariableInfo local, ILGenerator body)
        {
            EmitSpecific((short) local.LocalIndex, storeLocal, OpCodes.Stloc, body);
        }

        private static void EmitSpecific(short val, IReadOnlyDictionary<short, OpCode> map, OpCode generalCode, ILGenerator body)
        {
            OpCode code;
            if (map.TryGetValue(val, out code))
            {
                body.Emit(code);
            }
            else
            {
                body.Emit(generalCode, val);
            }
        }

        private static void EmitSpecific(int val, IReadOnlyDictionary<int, OpCode> map, OpCode generalCode, ILGenerator body)
        {
            OpCode code;
            if (map.TryGetValue(val, out code))
            {
                body.Emit(code);
            }
            else
            {
                body.Emit(generalCode, val);
            }
        }

        public static OpCode ArrayStoreOpCodeByType(Type type)
        {
            OpCode opcode;
            if (stElemOpCode.TryGetValue(type, out opcode))
            {
                return opcode;
            }

            return OpCodes.Stelem_Ref;
        }
    }
}