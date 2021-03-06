﻿using Microsoft.CodeAnalysis;

namespace RoslynCodeControls
{
    public class ClassNode : StructureNode
    {
        public string ClassIdentifier { get; }
        public override string DisplayText => "Class " + ClassIdentifier;

        public ClassNode(string classIdentifier, SyntaxNode node)
        {
            ClassIdentifier = classIdentifier;
            Node = node;
        }
    }
}