using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Voltium.Analyzers
{
    internal sealed class SyntaxTypeReceiver<T> : ISyntaxReceiver where T : SyntaxNode
    {
        public SyntaxTypeReceiver(Func<T, bool>? predicate = null)
        {
            _predicate = predicate;
        }

        private Func<T, bool>? _predicate;
        public List<(SyntaxTree Tree, T Node)> SyntaxNodes = new List<(SyntaxTree Tree, T Node)>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is T decl && (_predicate?.Invoke(decl) ?? true))
            {
                SyntaxNodes.Add((decl.SyntaxTree, decl));
            }
        }
    }
}
