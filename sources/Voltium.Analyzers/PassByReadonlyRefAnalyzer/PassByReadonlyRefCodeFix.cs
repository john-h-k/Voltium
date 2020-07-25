//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.CodeFixes;

//namespace Voltium.Analyzers.PassByReadonlyRefAnalyzer
//{
//    internal class PassByReadonlyRefCodeFix : CodeFixProvider
//    {
//        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PassByReadonlyRefAnalyzer.RuleId);

//        public override Task RegisterCodeFixesAsync(CodeFixContext context)
//        {
//            context.RegisterCodeFix(
//                CodeAction.Create(
//                    "MakePassByReadonlyRef",
//                    token => MakePassByReadonlyRef(token)
//                ),
//                context.Di
//            );

//            return Task.CompletedTask;
//        }

//        private Task<Document> MakePassByReadonlyRef(CancellationToken fixContext)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
