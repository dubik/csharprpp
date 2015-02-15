using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CLRCodeGen
{
    class CecilCodeGen
    {
        public static void doSomething()
        {
            AssemblyNameDefinition name = new AssemblyNameDefinition("CecilAssembly", new Version(1, 0));
            AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(name, "CLRCodeGen", ModuleKind.Dll);
            ModuleDefinition module = ModuleDefinition.CreateModule("myModule", ModuleKind.Dll);
        }
    }
}
