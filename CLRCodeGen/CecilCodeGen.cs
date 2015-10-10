using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace CLRCodeGen
{
    internal class CecilCodeGen
    {
        public static void DoSomething()
        {
            AssemblyNameDefinition name = new AssemblyNameDefinition("CecilAssembly", new Version(1, 0));
            AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(name, "CLRCodeGen", ModuleKind.Dll);
            ModuleDefinition module = ModuleDefinition.CreateModule("myModule", ModuleKind.Dll);
            TypeReference objectReference = module.Import(typeof (object));
            var met = objectReference.Resolve().Methods;
            TypeDefinition typeDef = new TypeDefinition("sample", "Main", TypeAttributes.Class | TypeAttributes.Public);

            AddEmptyConstructor(typeDef, met[0], module);
            TypeDefinition resolved = typeDef.Resolve();
            module.Types.Add(resolved);
            assembly.Write("my.dll");
        }

        private static void AddEmptyConstructor(TypeDefinition type, MethodReference baseEmptyConstructor, ModuleDefinition module)
        {
            const MethodAttributes methodAttributes =
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            var method = new MethodDefinition(".ctor", methodAttributes, module.TypeSystem.Void);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, baseEmptyConstructor));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            type.Methods.Add(method);
        }
    }
}