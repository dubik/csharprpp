using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CSharpRpp.Codegen
{
    class ClrCodegen : IRppNodeVisitor
    {
        private AssemblyName _assemblyName;
        private AssemblyBuilder _assemblyBuilder;
        private ModuleBuilder _moduleBuilder;

        public void Visit(RppProgram node)
        {
            _assemblyName = new AssemblyName(node.Name);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Save);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(node.Name, node.Name + ".exe");
        }

        public void Visit(RppClass node)
        {
            
        }

        public void Visit(RppFunc node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppVar node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppBlockExpr node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(BinOp node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppInteger node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppString node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppFuncCall node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppSelector node)
        {
            throw new System.NotImplementedException();
        }

        public void Visit(RppId node)
        {
            throw new System.NotImplementedException();
        }
    }
}