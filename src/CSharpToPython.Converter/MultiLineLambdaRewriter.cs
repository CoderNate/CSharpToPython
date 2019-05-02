using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CSharpToPython {
    public class MultiLineLambdaRewriter : Microsoft.CodeAnalysis.CSharp.CSharpSyntaxRewriter {
        private readonly Replacements _replacements;
        private readonly Dictionary<LambdaExpressionSyntax, int> _lambdaNumbersDict;

        public static SyntaxNode RewriteMultiLineLambdas(SyntaxNode node) {
            var replacements = GetMultilineLambdaReplacements(node);
            return new MultiLineLambdaRewriter(replacements).Visit(node);
        }

        private MultiLineLambdaRewriter(Replacements replacements) {
            // Statement containing lambda will either be inside a block, the single statement inside of
            // something like an if statement (and can be replaced by a block), or part of a switch case

            _replacements = replacements;
            _lambdaNumbersDict = replacements.AllLambdas
                .Select((l, index) => new { l, index })
                .ToDictionary(a => a.l, a => a.index);
        }
        public override SyntaxNode Visit(SyntaxNode node) {
            var visited = base.Visit(node);
            if (node is StatementSyntax stmt && _replacements.StatementReplaces.TryGetValue(stmt, out var replacements)) {
                var newStatements = replacements.Select(a => ConvertLambda(a.Lambda))
                    .Concat(new[] { (StatementSyntax)visited }).ToArray();
                var block = SyntaxFactory.Block(newStatements);
                return block;
            }
            return visited;
        }

        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
            return MaybeReplaceLambda(node) ?? base.VisitSimpleLambdaExpression(node);
        }
        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
            return MaybeReplaceLambda(node) ?? base.VisitParenthesizedLambdaExpression(node);
        }
        private ExpressionSyntax MaybeReplaceLambda(LambdaExpressionSyntax node) {
            if (_lambdaNumbersDict.TryGetValue(node, out var index)) {
                return SyntaxFactory.IdentifierName($"lambda__{index}");
            }
            return null;
        }

        private LocalFunctionStatementSyntax ConvertLambda(LambdaExpressionSyntax node) {
            LambdaExpressionSyntax visited;
            ParameterListSyntax parameterList;
            switch (node) {
                case ParenthesizedLambdaExpressionSyntax parLambda: {
                        var v = (ParenthesizedLambdaExpressionSyntax)base.VisitParenthesizedLambdaExpression(parLambda);
                        parameterList = v.ParameterList;
                        visited = v;
                        break;
                    }
                case SimpleLambdaExpressionSyntax simpleLambda: {
                        var v = (SimpleLambdaExpressionSyntax)base.VisitSimpleLambdaExpression(simpleLambda);
                        parameterList = SyntaxFactory.ParameterList(
                            SyntaxFactory.SingletonSeparatedList(v.Parameter)
                        );
                        visited = v;
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
            if (_lambdaNumbersDict.TryGetValue(node, out var index)) {
                return SyntaxFactory.LocalFunctionStatement(GetDummyMethodReturnType(), $"lambda__{index}")
                    .NormalizeWhitespace()
                    .WithParameterList(parameterList)
                    .WithBody((BlockSyntax)visited.Body);
            } else {
                throw new Exception();
            }
        }
        private PredefinedTypeSyntax GetDummyMethodReturnType()
                => SyntaxFactory.PredefinedType(SyntaxFactory.Token(CSharpSyntaxKind.ObjectKeyword));

        public override SyntaxNode VisitSwitchSection(SwitchSectionSyntax node) {
            var visited = (SwitchSectionSyntax)base.VisitSwitchSection(node);
            if (_replacements.SwitchSectionStatementInserts.TryGetValue(node, out var switchSectns)) {
                return visited.WithStatements(FixStatements(visited.Statements, node.Statements, switchSectns));
            }
            return visited;
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            var visited = (BlockSyntax)base.VisitBlock(node);
            if (_replacements.BlockModifies.TryGetValue(node, out var blockMods)) {
                return visited.WithStatements(FixStatements(visited.Statements, node.Statements, blockMods));
            }
            return visited;
        }

        private SyntaxList<StatementSyntax> FixStatements(
                SyntaxList<StatementSyntax> visitedStatements,
                SyntaxList<StatementSyntax> originalStatements,
                IEnumerable<IStatementInsertion> insertions) {
            var newStatements = new List<StatementSyntax>();
            for (var i = 0; i < visitedStatements.Count; i++) {
                var originalStmt = originalStatements[i];
                foreach (var insertion in insertions.Where(a => a.Statement == originalStmt)) {
                    newStatements.Add(ConvertLambda(insertion.Lambda));
                }
                newStatements.Add(visitedStatements[i]);
            }
            return new SyntaxList<StatementSyntax>(newStatements);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node) {
            var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
            if (node.ExpressionBody != null && _replacements.MethodReplaces.TryGetValue(node, out var replacements) ) {
                var newStatements = replacements
                    .Select(a => (StatementSyntax)ConvertLambda(a.Lambda))
                    .Concat(new[] { SyntaxFactory.ReturnStatement(
                        visited.ExpressionBody.Expression).NormalizeWhitespace()
                    })
                    .ToArray();

                return SyntaxFactory.MethodDeclaration(GetDummyMethodReturnType(), visited.Identifier)
                    .NormalizeWhitespace()
                    .WithBody(SyntaxFactory.Block(newStatements));
            }
            return visited;
        }

        private static Replacements GetMultilineLambdaReplacements(SyntaxNode node) {
            var methodReplacements = new List<MethodExpressionBodyReplace>();
            var blockMods = new List<BlockModify>();
            var stmtReplacements = new List<StatementReplace>();
            var switchInserts = new List<SwitchSectionStatementInsert>();
            var lambdasToInspect = node.DescendantNodes().OfType<LambdaExpressionSyntax>()
                .Where(lambdaExpr => lambdaExpr.Body is BlockSyntax);
            foreach (var lambda in lambdasToInspect) {
                StatementSyntax ownerStmt;
                SyntaxNode statementContainer;
                {
                    SyntaxNode statementParent = lambda;
                    for( ; ; ) {
                        statementParent = statementParent.Parent;
                        if (statementParent is StatementSyntax stmt) {
                            ownerStmt = stmt;
                            statementContainer = stmt.Parent;
                            break;
                        }
                        if (statementParent is MethodDeclarationSyntax methodDecl) {
                            ownerStmt = null;
                            statementContainer = methodDecl;
                            break;
                        }
                        if (statementParent is FieldDeclarationSyntax fieldDecl) {
                            throw new NotImplementedException("Rewriting of a lambda defined in a field initializer is not implemented.");
                        }
                        if (statementParent is PropertyDeclarationSyntax propDecl) {
                            throw new NotImplementedException("Rewriting of a lambda defined in a property initializer is not implemented.");
                        }
                    }
                }
                switch (statementContainer) {
                    case BlockSyntax blk:
                        blockMods.Add(new BlockModify { Block = blk, Lambda = lambda, Statement = ownerStmt });
                        break;
                    case SwitchSectionSyntax switchSectn:
                        switchInserts.Add(new SwitchSectionStatementInsert {
                            SwitchSection = switchSectn,
                            Statement = ownerStmt,
                            Lambda = lambda
                        });
                        break;
                    case MethodDeclarationSyntax methodDecl:
                        methodReplacements.Add(new MethodExpressionBodyReplace {
                            Method = methodDecl,
                            Lambda = lambda
                        });
                        break;
                    default:
                        stmtReplacements.Add(new StatementReplace { Lambda = lambda, Statement = ownerStmt });
                        break;
                }
            }
            return new Replacements {
                MethodReplaces = methodReplacements.ToLookup(a => a.Method).ToDictionary(a => a.Key, a => a.ToArray().AsEnumerable()),
                BlockModifies = blockMods.ToLookup(a => a.Block).ToDictionary(a => a.Key, a => a.ToArray().AsEnumerable()),
                StatementReplaces = stmtReplacements.ToLookup(a => a.Statement).ToDictionary(a => a.Key, a => a.ToArray().AsEnumerable()),
                SwitchSectionStatementInserts = switchInserts.ToLookup(a => a.SwitchSection).ToDictionary(a => a.Key, a => a.ToArray().AsEnumerable()),
                AllLambdas = lambdasToInspect,
            };
        }

        private class Replacements {
            public Dictionary<MethodDeclarationSyntax, IEnumerable<MethodExpressionBodyReplace>> MethodReplaces;
            public Dictionary<BlockSyntax, IEnumerable<BlockModify>> BlockModifies;
            public Dictionary<StatementSyntax, IEnumerable<StatementReplace>> StatementReplaces;
            public Dictionary<SwitchSectionSyntax, IEnumerable<SwitchSectionStatementInsert>> SwitchSectionStatementInserts;
            public IEnumerable<LambdaExpressionSyntax> AllLambdas;
        }
        private class MethodExpressionBodyReplace {
            public MethodDeclarationSyntax Method;
            public LambdaExpressionSyntax Lambda;
        }
        private class StatementReplace {
            public StatementSyntax Statement;
            public LambdaExpressionSyntax Lambda;
        }
        private interface IStatementInsertion {
            StatementSyntax Statement { get; set; }
            LambdaExpressionSyntax Lambda { get; set; }
        }
        private class BlockModify : IStatementInsertion {
            public BlockSyntax Block;
            public StatementSyntax Statement { get; set; }
            public LambdaExpressionSyntax Lambda { get; set; }
        }
        private class SwitchSectionStatementInsert : IStatementInsertion {
            public SwitchSectionSyntax SwitchSection;
            public StatementSyntax Statement { get; set; }
            public LambdaExpressionSyntax Lambda { get; set; }
        }
    }
}
