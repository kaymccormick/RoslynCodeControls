using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynCodeControls
{
    public class Walker : CSharpSyntaxWalker
    {
        public SemanticModel Model { get; }
        private Stack<StructureNode> _nodes = new Stack<StructureNode>();

        public Walker(SemanticModel model=null, SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node) : base(depth)
        {
            Model = model;
            var compilationUnitNode = new CompilationUnitNode();
            CompilationUnitNode = compilationUnitNode;
            _nodes.Push(compilationUnitNode);
        }

        public CompilationUnitNode CompilationUnitNode { get; set; }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            base.VisitLocalFunctionStatement(node);
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            base.VisitUsingDirective(node);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var namespaceNode = new NamespaceNode(node.Name.ToString());
            _nodes.Peek().Children.Add(namespaceNode);
            _nodes.Push(namespaceNode);
            base.VisitNamespaceDeclaration(node);
            _nodes.Pop();
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var classNode = new ClassNode(node.Identifier.ToString(), node);
            _nodes.Peek().Children.Add(classNode);
            _nodes.Push(classNode);
            base.VisitClassDeclaration(node);
            _nodes.Pop();
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            base.VisitStructDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            base.VisitInterfaceDeclaration(node);
        }


        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            base.VisitEnumDeclaration(node);
        }

        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            base.VisitDelegateDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            base.VisitFieldDeclaration(node);
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            base.VisitEventFieldDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var si = Model?.GetDeclaredSymbol(node);
            string s=null;
            s = si?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            if (s == null)
            {
                s = $"{node.ReturnType} {node.Identifier}";
            }
            var classNode = new MethodNode(s);
            _nodes.Peek().Children.Add(classNode);
            _nodes.Push(classNode);

            base.VisitMethodDeclaration(node);
            _nodes.Pop();
	    
        }

        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            base.VisitOperatorDeclaration(node);
        }

        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            base.VisitConversionOperatorDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var si = Model?.GetDeclaredSymbol(node);
            string s = null;
            s = si?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            if (s == null)
            {
                s = $"{node.Identifier}";
            }
            var constructorNode = new ConstructorNode(node);
            _nodes.Peek().Children.Add(constructorNode);
            _nodes.Push(constructorNode);

            base.VisitConstructorDeclaration(node);
            _nodes.Pop();

        }

        public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            base.VisitDestructorDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var si = Model?.GetDeclaredSymbol(node);
            string s = null;
            s = si?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            if (s == null)
            {
                s = $"{node.Identifier}";
            }
            var constructorNode = new PropertyNode(node);
            _nodes.Peek().Children.Add(constructorNode);
            _nodes.Push(constructorNode);


            base.VisitPropertyDeclaration(node); 
            
            _nodes.Pop();
            


        }

        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            base.VisitEventDeclaration(node);
        }

        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            base.VisitIndexerDeclaration(node);
        }

        public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            base.VisitAccessorDeclaration(node);
        }
    }
}