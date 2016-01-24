using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using static CSharpRpp.ListExtensions;

namespace CSharpRpp
{
    [DebuggerDisplay("Classes = {_classes.Count}")]
    public class RppProgram : RppNode
    {
        public IEnumerable<RppClass> Classes => _classes.AsEnumerable();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] private IList<RppClass> _classes = new List<RppClass>();

        public void Add(RppClass clazz)
        {
            _classes.Add(clazz);
            if (clazz.Modifiers.Contains(ObjectModifier.OmCase))
            {
                _classes.Add(CreateCompanion(clazz.Name, clazz.ClassParams.Select(p => p.Type)));
            }
        }

        private static RppClass CreateCompanion(string name, IEnumerable<ResolvableType> classParamTypes)
        {
            RppFunc factoryFunc = CreateApply(new RTypeName(name), classParamTypes);
            var exprs = List(factoryFunc);
            RppClass clazz = new RppClass(ClassKind.Object, new HashSet<ObjectModifier>(), name, Collections.NoFields, exprs,
                Collections.NoVariantTypeParams,
                RppBaseConstructorCall.Object);

            return clazz;
        }

        private static RppFunc CreateApply(RTypeName classType, IEnumerable<ResolvableType> classParams)
        {
            int paramIndex = 0;
            IEnumerable<IRppParam> funcParams = classParams.Select(t => new RppParam($"_{paramIndex++}", t)).ToList();
            RppNew newExpr = new RppNew(new ResolvableType(classType), funcParams.Select(p => new RppId(p.Name, p)));
            return new RppFunc("apply", funcParams, new ResolvableType(classType), newExpr);
        }


        public void PreAnalyze(SymbolTable scope)
        {
            _classes.ForEach(c => scope.AddType(c.Type));
            /*
            _classes.Where(c => c.Modifiers.Contains(ObjectModifier.OmCase))
                .Select(caseClass => CreateOrUpdateCompanion(scope, caseClass))
                .Where(c => c != null).ToList().ForEach(_classes.Add);
                */
            NodeUtils.PreAnalyze(scope, _classes);
        }

        /*
        private RppClass CreateOrUpdateCompanion(SymbolTable scope, RppClass caseClass)
        {
            TypeSymbol lookupObject = scope.LookupObject(caseClass.Name);
            if (lookupObject == null)
            {
                return caseClass.CreateCompanion(scope);
            }

            throw new NotImplementedException();

            // TODO I don't like this, return null if we update...we should probably split these cases somehow...
            // return null;
        }
        */
        public override void Accept(IRppNodeVisitor visitor)
        {
            visitor.Visit(this);
            _classes.ForEach(clazz => clazz.Accept(visitor));
        }

        public override IRppNode Analyze(SymbolTable scope, Diagnostic diagnostic)
        {
            _classes = NodeUtils.Analyze(scope, _classes, diagnostic);
            return this;
        }
    }
}