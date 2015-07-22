/*
 * 
 * PUBLIC DOMAIN
 * 
 * Implements :
 * 
 * http://en.wikipedia.org/wiki/Hindley%E2%80%93Milner#Algorithm_W
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AlgoW
{
    internal class Generic : IType
    {
        protected Generic() : this(null)
        {
        }

        protected Generic(IType[] args)
        {
            Args = args ?? new IType[] {};
        }

        public IType[] Args { get; private set; }
    }

    internal class Var : Generic
    {
        private static int uid;
        private readonly int id;

        internal Var()
        {
            id = Uid;
        }

        private static int Uid
        {
            get { return Interlocked.Increment(ref uid); }
        }

        internal int Id
        {
            get { return id; }
        }

        internal IType Type { get; set; }

        public override string ToString()
        {
            return ((Type != null) ? Type.ToString() : String.Format("`{0}", id));
        }
    }

    internal class Type : Generic
    {
        internal const string Function = "Function";
        internal const string Boolean = "Boolean";
        internal const string Integer = "Integer";
        internal const string String = "String";

        internal static readonly IDictionary<string, IType> Const = new Dictionary<string, IType>();

        static Type()
        {
            Const[Boolean] = new Type(Boolean);
            Const[Integer] = new Type(Integer);
            Const[String] = new Type(String);
        }

        internal Type(string constructor) : this(constructor, null)
        {
        }

        internal Type(string constructor, IType[] args) : base(args)
        {
            Constructor = constructor;
        }

        internal string Constructor { get; private set; }

        public override string ToString()
        {
            return ((Args.Length > 0) ? String.Format("{0}<{1}>", Constructor, String.Join(",", Args.Select(arg => arg.ToString()))) : Constructor);
        }
    }

    internal static class Unifier
    {
        private static IType Prune(IType t)
        {
            var var = t as Var;
            return ((var != null) && (var.Type != null)) ? (var.Type = Prune(var.Type)) : t;
        }

        private static bool OccursCheck(IType t, IType s)
        {
            s = Prune(s);

            if (t == s)
            {
                return true;
            }

            if (s is Type)
            {
                return OccursChecks(t, s.Args);
            }

            return false;
        }

        private static bool OccursChecks(IType t, IEnumerable<IType> types)
        {
            return types.Any(type => OccursCheck(t, type));
        }

        internal static IType Fresh(IType t, IType[] types)
        {
            return Fresh(t, types, null);
        }

        internal static IType Fresh(IType t, IType[] types, IDictionary<int, Var> vars)
        {
            if (t == null)
            {
                throw new ArgumentNullException("t", "cannot be null");
            }

            vars = vars ?? new Dictionary<int, Var>();

            t = Prune(t);

            Type type = t as Type;
            Var var = t as Var;

            if (var != null) // t is a Var
            {
                if (!OccursChecks(t, types))
                {
                    if (!vars.ContainsKey(var.Id))
                    {
                        vars[var.Id] = new Var();
                    }
                    return vars[var.Id];
                }
                return t;
            }
            if (type != null) // t is a Type
            {
                return new Type(type.Constructor, type.Args.Select(arg => Fresh(arg, types, vars)).ToArray());
            }
            throw new ArgumentOutOfRangeException("t", String.Format("unsupported symbol type ({0})", t.GetType().Name));
        }

        internal static void Unify(IType t, IType s)
        {
            t = Prune(t);
            s = Prune(s);
            if (t is Var)
            {
                if (t != s)
                {
                    if (OccursCheck(t, s))
                    {
                        throw new InvalidOperationException("recursive unification");
                    }
                    ((Var) t).Type = s;
                }
            }
            else if ((t is Type) && (s is Var))
            {
                Unify(s, t);
            }
            else if ((t is Type) && (s is Type))
            {
                var tType = (Type) t;
                var sType = (Type) s;

                if ((tType.Constructor != sType.Constructor) || (tType.Args.Length != sType.Args.Length))
                {
                    throw new InvalidOperationException(String.Concat(tType.ToString(), " incompatible with ", sType.ToString()));
                }

                for (int i = 0; i < tType.Args.Length; i++)
                {
                    Unify(tType.Args[i], sType.Args[i]);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("t and/or s", "undecided unification or unsupported symbol type");
            }
        }
    }

    public interface IType
    {
        IType[] Args { get; }
    }

    public interface INode
    {
        object Name { get; } // If not null, can be either an identifier (as string, possibly empty to denote anonymity), or an identifier reference (as INode)
        string Type { get; } // If not null, cannot be the empty string and holds a programmer-provided / -tagged type name
        INode[] Args { get; }
        // If not null, holds either function definition formal params, or function call actual params (could also be used to hold child nodes in general)
        object Term { get; } // If not null, is either a special inner INode (e.g., a function body), or a basic type value (which might in fact accept null)
    }

    public class Node : INode
    {
        public object Name { get; set; }
        public string Type { get; set; }
        public INode[] Args { get; set; }
        public object Term { get; set; }
    }

    public struct TypeOrError
    {
        public string Error;
        public IType Type;
    }

    public static class Inference
    {
        private static IType Infer(IDictionary<string, IType> env, INode node)
        {
            return Infer(env, node, null);
        }

        private static IType Infer(IDictionary<string, IType> env, INode node, IType[] known)
        {
            Func<INode, IType> taggedType = tagged =>
            {
                if (!Type.Const.ContainsKey(tagged.Type))
                {
                    throw new InvalidOperationException(String.Format("unknown basic type : \"{0}\"", tagged.Type));
                }
                return Type.Const[tagged.Type];
            };

            known = known ?? new IType[] {};

            if ((node.Name is string) && (node.Args == null) && (node.Term == null))
            {
                if (!env.ContainsKey((string) node.Name))
                {
                    throw new InvalidOperationException(String.Format("unknown identifier : \"{0}\"", node.Name));
                }
                return Unifier.Fresh(env[(string) node.Name], known);
            }

            if ((node.Name == null) && (node.Type != null))
            {
                return taggedType(node);
            }

            if ((node.Name is INode) && (node.Args != null) && (node.Term == null))
            {
                List<IType> args = node.Args.Select(arg => Infer(env, arg, known)).ToList();
                var type = new Var();
                args.Add(type);
                Unifier.Unify(new Type(Type.Function, args.ToArray()), Infer(env, (INode) node.Name, known));
                return type;
            }

            if ((node.Name is string) && (node.Args != null) && (node.Term is INode))
            {
                // Optional: node.Type (string)
                List<IType> types = known.ToList();
                var args = new List<IType>();
                Type type;
                foreach (INode arg in node.Args)
                {
                    IType var;
                    if (arg.Type == null)
                    {
                        var = new Var();
                        types.Add(var);
                    }
                    else
                    {
                        var = taggedType(arg);
                    }
                    env[(string) arg.Name] = var;
                    args.Add(var);
                }
                args.Add(Infer(env, (INode) node.Term, types.ToArray()));
                if (node.Type != null)
                {
                    Unifier.Unify(args[args.Count - 1], taggedType(node));
                }
                type = new Type(Type.Function, args.ToArray());

                if (((string) node.Name).Length > 0)
                {
                    env[(string) node.Name] = type;
                }
                return type;
            }

            if ((node.Name is string) && (((string) node.Name).Length > 0) && (node.Term is INode))
            {
                // Optional: node.Type (string)
                IType type = Infer(env, (INode) node.Term, known);
                if (node.Type != null)
                {
                    Unifier.Unify(type, taggedType(node));
                }
                return (env[(string) node.Name] = type);
            }
            throw new ArgumentOutOfRangeException("node", "missing or invalid attribute(s)");
        }

        public static TypeOrError[] GetTypeSystem(INode ast)
        {
            return GetTypeSystem(null, ast);
        }

        public static TypeOrError[] GetTypeSystem(IDictionary<string, IType> env, INode ast)
        {
            var items = new List<TypeOrError>();
            if (ast == null)
            {
                throw new ArgumentNullException("ast", "cannot be null");
            }
            if (ast.Args == null)
            {
                throw new ArgumentNullException("ast.Args", "cannot be null");
            }
            env = env ?? new Dictionary<string, IType>();
            foreach (INode node in ast.Args)

                try
                {
                    items.Add(new TypeOrError {Type = Infer(env, node)});
                }
                catch (Exception ex)
                {
                    items.Add(new TypeOrError {Error = ex.Message});
                }
            return items.ToArray();
        }
    }

    public class HindleyMilner
    {
        public static void DoMain()
        {
            var ast = new Node
            {
                Args = new INode[]
                {
                    new Node
                    {
                        Name = "num",
                        Term = new Node {Type = "Integer", Term = 100}
                    },
                    new Node
                    {
                        Name = "id",
                        Args = new INode[] {new Node {Name = "x"}},
                        Term = new Node {Name = "x"}
                    },
                    new Node
                    {
                        Name = "str2intFunc",
                        Args = new INode[] {new Node {Name = "s", Type = "String"}},
                        Term = new Node {Name = "num"}
                    },
                    new Node
                    {
                        Name = String.Empty,
                        Args = new INode[] {new Node {Name = "a"}},
                        Term = new Node {Name = "str2intFunc"}
                    },
                    new Node
                    {
                        Name = new Node {Name = "id"},
                        Args = new INode[] {new Node {Type = "Boolean", Term = true}}
                    },
                    new Node
                    {
                        Name = new Node {Name = "str2intFunc"},
                        Args = new INode[] {new Node {Type = "String", Term = "abc"}}
                    },
                    new Node
                    {
                        Name = new Node {Name = "str2intFunc"},
                        Args = new INode[] {new Node {Type = "Integer", Term = 200}}
                    },
                    new Node
                    {
                        Name = new Node {Name = "str2intFunc"},
                        Args = new INode[]
                        {
                            new Node
                            {
                                Name = new Node {Name = "id"},
                                Args = new INode[] {new Node {Type = "Boolean", Term = false}}
                            }
                        }
                    }
                }
            };

            TypeOrError[] items = Inference.GetTypeSystem(ast);

            Console.WriteLine();
            foreach (TypeOrError item in items)
            {
                Console.WriteLine(item.Type != null ? String.Concat("Type : ", item.Type.ToString()) : String.Concat("Error: ", item.Error));
                Console.WriteLine();
            }
            Console.ReadLine();
        }
    }
}