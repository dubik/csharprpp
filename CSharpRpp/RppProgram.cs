using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSharpRpp.Expr;
using CSharpRpp.Reporting;
using CSharpRpp.Symbols;
using CSharpRpp.TypeSystem;
using JetBrains.Annotations;
using static CSharpRpp.ListExtensions;
using static CSharpRpp.RppAst;
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
                expr = If(NotNull(Id("obj")),
                    Call("Some", List<IRppExpr>(New(tupleTypeNameString, listOfFields))),
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
                string tupleTypeNameString = "Tuple" + names.Count();
                RTypeName tuppleType = new RTypeName(tupleTypeNameString);
                names.Select(n => new RTypeName(n)).ForEach(tuppleType.AddGenericArgument);
                optionType.AddGenericArgument(tuppleType);
                return new ResolvableType(optionType);
            }
        }
    }

    internal static class RppAst
    {
        public static RppParam Param(string name, string type)
        {
            return new RppParam(name, ResolvableType(type));
        }

        public static RppParam Param(string name, RTypeName typeName)
        {
            return new RppParam(name, new ResolvableType(typeName));
        }

        public static RppBinOp NotNull(IRppExpr expr)
        {
            return BinOp("!=", expr, Null);
        }

        public static RppId Id(string name)
        {
            return new RppId(name);
        }

        public static RppNew New(string typeNameString, IEnumerable<IRppExpr> args)
        {
            return New(ResolvableType(typeNameString), args);
        }

        public static RppNew New(ResolvableType type, IEnumerable<IRppExpr> args)
        {
            return new RppNew(type, args);
        }

        public static RppBinOp BinOp(string op, IRppExpr left, IRppExpr right)
        {
            return RppBinOp.Create(op, left, right);
        }

        public static RTypeName TypeName(string name)
        {
            return new RTypeName(name);
        }

        public static ResolvableType ResolvableType(string name)
        {
            return new ResolvableType(TypeName(name));
        }

        public static RppSelector FieldSelect(string target, string fieldName)
        {
            return Selector(Id(target), Id(fieldName));
        }

        public static RppSelector Selector(IRppExpr target, RppMember path)
        {
            return new RppSelector(target, path);
        }

        public static RppIf If(IRppExpr condition, IRppExpr thenExpr, IRppExpr elseExpr)
        {
            return new RppIf(condition, thenExpr, elseExpr);
        }

        public static RppFuncCall Call(string name, IList<IRppExpr> args)
        {
            return new RppFuncCall(name, args);
        }

        public static RppFuncCall Call(string name, IList<IRppExpr> args, IList<ResolvableType> typeList)
        {
            return new RppFuncCall(name, args, typeList);
        }

        public static RppNull Null = RppNull.Instance;
    }
}