using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace CLRCodeGen
{
    internal class CecilCodeGen
    {
        public static void DoSomething()
        {
            AssemblyNameDefinition name = new AssemblyNameDefinition("CecilAssembly", new Version(1, 0));
            AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(name, "CecilAssembly", ModuleKind.Dll);
            ModuleDefinition module = ModuleDefinition.CreateModule("myModule", ModuleKind.Dll);
            TypeReference objectReference = module.Import(typeof (object));
            var met = objectReference.Resolve().Methods;
            TypeDefinition typeDef = new TypeDefinition("sample", "Main", TypeAttributes.Class | TypeAttributes.Public);
            AddEmptyConstructor(typeDef, met[0], module);
            TypeDefinition resolved = typeDef.Resolve();
            module.Types.Add(resolved);
            assembly.Modules.Add(module);
            assembly.Write("CecilAssembly.dll");
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

        public static void ExploreCecilType()
        {
            AssemblyNameDefinition name = new AssemblyNameDefinition("mycecil", new Version(1, 0));
            AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(name, "myModule", ModuleKind.Console);
            ModuleDefinition module = assembly.MainModule;

            TypeReference objectRef = module.Import(typeof (object));
            MethodDefinition baseEmptyConstructor = objectRef.Resolve().Methods[0];
            MethodReference constrRef = module.Import(baseEmptyConstructor);

            TypeDefinition fooType = new TypeDefinition("", "Foo",
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, objectRef);
            module.Types.Add(fooType);

            GenericParameter p = new GenericParameter(0, GenericParameterType.Type, module) {Name = "A"};
            fooType.GenericParameters.Add(p);

            FieldDefinition fieldA = new FieldDefinition("id", FieldAttributes.Private, p);
            fooType.Fields.Add(fieldA);

            MethodDefinition create = new MethodDefinition("Create", MethodAttributes.Public, p);
            ParameterDefinition thisParam = new ParameterDefinition("this", ParameterAttributes.Optional, fooType);
            create.Parameters.Add(thisParam);
            fooType.Methods.Add(create);

            AddEmptyConstructor(fooType, constrRef, module);


            GenericInstanceType fooBaseClass = fooType.MakeGenericInstanceType(module.TypeSystem.Int32);
            TypeDefinition barType = new TypeDefinition("", "Bar", TypeAttributes.Public | TypeAttributes.Class, fooBaseClass);
            module.Types.Add(barType);
            AddEmptyConstructor(barType, fooType.Methods[0], module);
            assembly.Write("mycecil.dll");
        }
    }
}