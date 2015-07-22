using System;
using System.Runtime.InteropServices.ComTypes;
using AlgoW;

namespace VisitorThingy
{
    interface INodeVisitor
    {
        void Visit(PlusNode node);
        void Visit(StoreNode node);
        void Visit(VarNode node);
        void Visit(NumberNode node);
    }

    abstract class Node
    {
        public abstract void Accept(INodeVisitor visitor);
    }

    class PlusNode : Node
    {
        public Node LeftNode { get; set; }
        public Node RightNode { get; set; }

        public PlusNode(Node leftNode, Node rightNode)
        {
            LeftNode = leftNode;
            RightNode = rightNode;
        }

        public override void Accept(INodeVisitor visitor)
        {
            LeftNode.Accept(visitor);
            RightNode.Accept(visitor);

            visitor.Visit(this);
        }
    }

    class StoreNode : Node
    {
        public Node LeftNode { get; set; }
        public Node RightNode { get; set; }

        public StoreNode(Node leftNode, Node rightNode)
        {
            LeftNode = leftNode;
            RightNode = rightNode;
        }

        public override void Accept(INodeVisitor visitor)
        {
            RightNode.Accept(visitor);
            visitor.Visit(this);
        }
    }

    class VarNode : Node
    {
        public string Name { get; set; }
        public Node InitExpr { get; set; }

        public override void Accept(INodeVisitor visitor)
        {
            if (InitExpr != null)
            {
                InitExpr.Accept(visitor);
            }

            visitor.Visit(this);
        }
    }

    class NumberNode : Node
    {
        public int Number { get; private set; }

        public NumberNode(int number)
        {
            Number = number;
        }

        public override void Accept(INodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    class Codegen : INodeVisitor
    {
        public void Visit(PlusNode node)
        {
            Console.WriteLine("PlusNode");
        }

        public void Visit(StoreNode node)
        {
            Console.WriteLine("StoreNode");
        }

        public void Visit(VarNode node)
        {
            Console.WriteLine("VarNode");
        }

        public void Visit(NumberNode node)
        {
            Console.WriteLine("NumberNode: " + node.Number);
        }
    }

    class Program
    {
        private static void Main(string[] args)
        {
            /*
            Node program = new VarNode {Name = "x", InitExpr = new PlusNode(new NumberNode(5), new NumberNode(10))};
            INodeVisitor codegen = new Codegen();
            program.Accept(codegen);

            PrintlnSomething();
             */

            HindleyMilner.DoMain();
        }

        public static void Println(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public static void PrintlnSomething()
        {
            Println("{0}, {1}", 10, 20);
        }
    }
}