﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.Utils.AstHelper;
using ResolvableType = CSharpRpp.TypeSystem.ResolvableType;

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
                _classes.Add(CreateCompanion(clazz.Name, clazz.ClassParams));
            }
        }

        public void PreAnalyze(SymbolTable scope)
        {
            _classes.ForEach(c => scope.AddType(c.Type));
            NodeUtils.PreAnalyze(scope, _classes);
        }

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

        private static RppClass CreateCompanion(string name, IEnumerable<RppField> classParamsCollection)
        {
            RTypeName typeName = new RTypeName(name);
            IEnumerable<RppField> classParams = classParamsCollection as IList<RppField> ?? classParamsCollection.ToList();
            var classParamsTypes = classParams.Select(p => p.Type).ToList();
            RppFunc apply = CreateApply(typeName, classParamsTypes);
            RppFunc unapply = CreateUnapply(typeName, classParams);
            var exprs = List(apply, unapply);
            RppClass clazz = new RppClass(ClassKind.Object, new HashSet<ObjectModifier>(), name, Collections.NoFields, exprs,
                Collections.NoVariantTypeParams,
                RppBaseConstructorCall.Object);

            return clazz;
        }

        private static RppFunc CreateApply(RTypeName className, IEnumerable<ResolvableType> classParams)
        {
            int paramIndex = 0;
            IEnumerable<IRppParam> funcParams = classParams.Select(t => new RppParam($"_{paramIndex++}", t)).ToList();
            RppNew newExpr = new RppNew(new ResolvableType(className), funcParams.Select(p => new RppId(p.Name, p)));
            return new RppFunc("apply", funcParams, new ResolvableType(className), newExpr);
        }

        private static RppFunc CreateUnapply([NotNull] RTypeName className, IEnumerable<RppField> classParamsCollection)
        {
            IRppExpr expr;
            IEnumerable<RppField> classParams = classParamsCollection as IList<RppField> ?? classParamsCollection.ToList();
            if (!classParams.Any())
            {
                // Boolean
                expr = NotNull(Id("obj"));
            }
            else if (classParams.Count() == 1)
            {
                // Option[T]
                /*
                    // T - object type
                    // A - field type
                    def unapply(obj: T) : Option[A] = if(obj != null) Some(obj.Field) else None
                    
                */
                RppField first = classParams.First();
                expr = If(NotNull(Id("obj")),
                    Call("Some", List<IRppExpr>(FieldSelect("obj", first.Name)), List(first.Type)),
                    Id("None"));
            }
            else
            {
                /*
                    // T - object type
                    // A<id> - field types
                    def unapply(obj: T) : Option[TupleX[A1, A2, ...]] = if(obj != null) Some(new TupleX(obj.Field1, obj.Field2, ...)) else None
                */
                IEnumerable<RppSelector> listOfFields = classParams.Select(p => FieldSelect("obj", p.Name));

                string tupleTypeNameString = "Tuple" + classParams.Count();
                RTypeName tupleType = CreateTupleType(classParams.Select(p => p.Type.Name.Name));

                expr = If(NotNull(Id("obj")),
                    Call("Some", List<IRppExpr>(New(tupleTypeNameString, listOfFields)), List(new ResolvableType(tupleType))),
                    Id("None"));
            }

            ResolvableType unapplyReturnType = CreateUnapplyReturnType(classParams.Select(p => p.Type.Name.Name));
            return new RppFunc("unapply", List(Param("obj", className)), unapplyReturnType, expr);
        }

        private static ResolvableType CreateUnapplyReturnType(IEnumerable<string> typeNames)
        {
            IEnumerable<string> names = typeNames as IList<string> ?? typeNames.ToList();
            if (!names.Any())
            {
                return ResolvableType.BooleanTy;
            }

            if (names.Count() == 1)
            {
                RTypeName optionType = new RTypeName("Option");
                optionType.AddGenericArgument(new RTypeName(names.First()));
                return new ResolvableType(optionType);
            }
            else
            {
                RTypeName optionType = new RTypeName("Option");
                RTypeName tuppleType = CreateTupleType(names);
                optionType.AddGenericArgument(tuppleType);
                return new ResolvableType(optionType);
            }
        }

        private static RTypeName CreateTupleType(IEnumerable<string> names)
        {
            IEnumerable<string> typeNames = names as IList<string> ?? names.ToList();
            string tupleTypeNameString = "Tuple" + typeNames.Count();
            RTypeName tuppleType = new RTypeName(tupleTypeNameString);
            typeNames.Select(n => new RTypeName(n)).ForEach(tuppleType.AddGenericArgument);
            return tuppleType;
        }
    }
}