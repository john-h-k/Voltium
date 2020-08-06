using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Voltium.Analyzers
{
    internal abstract class PredicatedTypeGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // if you wanna debug this method, uncomment this
            // ugly but works. blame roslyn devs not me

            var receiver = (SyntaxTypeReceiver<TypeDeclarationSyntax>)context.SyntaxReceiver!;

            var nodes = receiver.SyntaxNodes;
            var comp = context.Compilation;

            OnExecute(context);


            // this handles partial types, which have multiple type declaration nodes
            var visited = new HashSet<string>();
            foreach (var (tree, node) in nodes)
            {
                var semantics = comp.GetSemanticModel(tree);
                var type = (semantics.GetDeclaredSymbol(node) as INamedTypeSymbol)!;

                if (!Predicate(context, type) || visited.Contains(type.Name))
                {
                    continue;
                }

                GenerateFromSyntax(context, node, tree);
                GenerateFromSymbol(context, type);
                visited.Add(type.Name);
            }
        }

        protected virtual void OnExecute(SourceGeneratorContext context) { }

        protected abstract bool Predicate(SourceGeneratorContext context, INamedTypeSymbol decl);


        protected virtual void GenerateFromSyntax(SourceGeneratorContext context, TypeDeclarationSyntax syntax, SyntaxTree tree) { }
        protected virtual void GenerateFromSymbol(SourceGeneratorContext context, INamedTypeSymbol symbol) {  }

        public void Initialize(InitializationContext context)
            => context.RegisterForSyntaxNotifications(() => new SyntaxTypeReceiver<TypeDeclarationSyntax>());
    }

    internal abstract class PredicatedGenerator<T> : ISourceGenerator where T : SyntaxNode
    {
        public void Execute(SourceGeneratorContext context)
        {
            // if you wanna debug this method, uncomment this
            // ugly but works. blame roslyn devs not me

            var receiver = (SyntaxTypeReceiver<T>)context.SyntaxReceiver!;

            var nodes = receiver.SyntaxNodes;

            var comp = context.Compilation;

            OnExecute(context);

            // this handles partial types, which have multiple type declaration nodes
            var visited = new HashSet<ISymbol>();
            foreach (var (tree, node) in nodes)
            {
                var semantics = comp.GetSemanticModel(tree);
                var symbol = semantics.GetDeclaredSymbol(node);

                if (!Predicate(context, symbol) || visited.Contains(symbol))
                {
                    continue;
                }

                Generate(context, symbol, node);
                visited.Add(symbol);
            }
        }

        protected virtual void OnExecute(SourceGeneratorContext context) { }

        protected abstract bool Predicate(SourceGeneratorContext context, ISymbol decl);

        protected abstract void Generate(SourceGeneratorContext context, ISymbol symbol, T syntax);

        public void Initialize(InitializationContext context)
            => context.RegisterForSyntaxNotifications(() => new SyntaxTypeReceiver<T>());
    }
}
