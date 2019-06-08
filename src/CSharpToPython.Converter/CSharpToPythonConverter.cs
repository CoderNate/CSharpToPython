using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PyAst = IronPython.Compiler.Ast;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using PythonOperator = IronPython.Compiler.PythonOperator;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace IronPython.Compiler.Ast {
    public class FunctionDefinition2 { }
}
namespace CSharpToPython {
    public class CSharpToPythonConvert : Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor<PyAst.Node> {


        public override PyAst.Node DefaultVisit(SyntaxNode node) {
            throw new NotImplementedException($"Node type {node.GetType().Name} not implemented yet.");
        }

        public override PyAst.Node VisitLiteralExpression(LiteralExpressionSyntax node) {
            object constantValue;
            switch (node.Kind()) {
                case CSharpSyntaxKind.NumericLiteralExpression:
                case CSharpSyntaxKind.StringLiteralExpression:
                case CSharpSyntaxKind.CharacterLiteralExpression:
                case CSharpSyntaxKind.TrueLiteralExpression:
                case CSharpSyntaxKind.FalseLiteralExpression:
                case CSharpSyntaxKind.NullLiteralExpression:
                    constantValue = node.Token.Value;
                    break;
                default:
                    throw new NotImplementedException($"Literal of kind {node.Kind()} not implemented yet.");
            }
            return new PyAst.ConstantExpression(constantValue);
        }

        public override PyAst.Node VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node) {
            var formatString = string.Join(
                "",
                node.Contents.Select(a => a is InterpolatedStringTextSyntax interpText ? interpText.TextToken.Text : "{}")
            );
            var interpExpressionArgs = node.Contents.OfType<InterpolationSyntax>()
                .Select(a => new PyAst.Arg((PyAst.Expression)Visit(a.Expression))).ToArray();
            return new PyAst.CallExpression(
                new PyAst.MemberExpression(new PyAst.ConstantExpression(formatString), "format"),
                interpExpressionArgs);
        }

        public override PyAst.Node VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node) {
            return new PyAst.ListExpression(node.Initializer.Expressions.Select(e => (PyAst.Expression)Visit(e)).ToArray());
        }
        public override PyAst.Node VisitArrayCreationExpression(ArrayCreationExpressionSyntax node) {
            var expressions = node.Initializer?.Expressions ?? Enumerable.Empty<ExpressionSyntax>();
            return new PyAst.ListExpression(expressions.Select(e => (PyAst.Expression)Visit(e)).ToArray());
        }
        public override PyAst.Node VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) {
            if (node.Initializer != null) {
                throw new NotImplementedException();
            }
            var args = node.ArgumentList.Arguments.Select(a => (PyAst.Arg)Visit(a)).ToArray();
            TypeSyntax type;
            if (node.Type is IdentifierNameSyntax ident && ident.Identifier.Text == "Exception") {
                type = SyntaxFactory.ParseTypeName("System.Exception");
            } else {
                type = node.Type;
            }
            return new PyAst.CallExpression((PyAst.Expression)Visit(type), args);
        }

        public override PyAst.Node VisitConditionalExpression(ConditionalExpressionSyntax node) {
            return new PyAst.ConditionalExpression(
                (PyAst.Expression)Visit(node.Condition),
                (PyAst.Expression)Visit(node.WhenTrue),
                (PyAst.Expression)Visit(node.WhenFalse)
            );
        }

        public override PyAst.Node VisitBinaryExpression(BinaryExpressionSyntax node) {
            var nodeKind = node.Kind();
            var leftExpr = (PyAst.Expression)Visit(node.Left);
            var rightExpr = (PyAst.Expression)Visit(node.Right);
            switch (nodeKind) {
                case CSharpSyntaxKind.LogicalAndExpression:
                    return new PyAst.AndExpression(leftExpr, rightExpr);
                case CSharpSyntaxKind.CoalesceExpression:
                case CSharpSyntaxKind.LogicalOrExpression:
                    return new PyAst.OrExpression(leftExpr, rightExpr);
                case CSharpSyntaxKind.IsExpression:
                    return new PyAst.CallExpression(
                        new PyAst.NameExpression("isinstance"),
                        new[] { new PyAst.Arg(leftExpr), new PyAst.Arg(rightExpr) }
                    );
                case CSharpSyntaxKind.AsExpression:
                    var innerLambda = new PyAst.LambdaExpression(
                        new PyAst.FunctionDefinition(null, new[] { new PyAst.Parameter("__arg__") },
                            new PyAst.ReturnStatement(
                                new PyAst.ConditionalExpression(
                                    new PyAst.CallExpression(
                                        new PyAst.NameExpression("isinstance"),
                                        new[] { new PyAst.Arg(new PyAst.NameExpression("__arg__")), new PyAst.Arg(rightExpr) }
                                    ),
                                    new PyAst.NameExpression("__arg__"),
                                    new PyAst.ConstantExpression(null)
                                )
                            )
                        )
                    );
                    return new PyAst.CallExpression(
                        new PyAst.ParenthesisExpression(innerLambda),
                        new [] {new PyAst.Arg(leftExpr)}
                    );
            }
            PythonOperator pythonOp;
            switch (nodeKind) {
                case CSharpSyntaxKind.AddExpression: pythonOp = PythonOperator.Add; break;
                case CSharpSyntaxKind.SubtractExpression: pythonOp = PythonOperator.Subtract; break;
                case CSharpSyntaxKind.MultiplyExpression: pythonOp = PythonOperator.Multiply; break;
                case CSharpSyntaxKind.DivideExpression: pythonOp = PythonOperator.Divide; break;
                case CSharpSyntaxKind.EqualsExpression: pythonOp = PythonOperator.Equal; break;
                case CSharpSyntaxKind.NotEqualsExpression: pythonOp = PythonOperator.NotEqual; break;
                case CSharpSyntaxKind.GreaterThanExpression: pythonOp = PythonOperator.GreaterThan; break;
                case CSharpSyntaxKind.GreaterThanOrEqualExpression: pythonOp = PythonOperator.GreaterThanOrEqual; break;
                case CSharpSyntaxKind.LessThanExpression: pythonOp = PythonOperator.LessThan; break;
                case CSharpSyntaxKind.LessThanOrEqualExpression: pythonOp = PythonOperator.LessThanOrEqual; break;
                case CSharpSyntaxKind.ModuloExpression: pythonOp = PythonOperator.Mod; break;
                case CSharpSyntaxKind.LeftShiftExpression: pythonOp = PythonOperator.LeftShift; break;
                case CSharpSyntaxKind.RightShiftExpression: pythonOp = PythonOperator.RightShift; break;
                case CSharpSyntaxKind.BitwiseAndExpression: pythonOp = PythonOperator.BitwiseAnd; break;
                case CSharpSyntaxKind.BitwiseOrExpression: pythonOp = PythonOperator.BitwiseOr; break;
                case CSharpSyntaxKind.ExclusiveOrExpression: pythonOp = PythonOperator.ExclusiveOr; break;
                default:
                    throw new NotImplementedException($"Binary expression kind {node.Kind()} not implemented yet.");
            }
            return new PyAst.BinaryExpression(pythonOp, leftExpr, rightExpr);
        }

        public override PyAst.Node VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {
            PythonOperator op;
            switch (node.Kind()) {
                case CSharpSyntaxKind.UnaryPlusExpression: op = PythonOperator.Add; break;
                case CSharpSyntaxKind.UnaryMinusExpression: op = PythonOperator.Subtract; break;
                case CSharpSyntaxKind.LogicalNotExpression: op = PythonOperator.Not; break;
                case CSharpSyntaxKind.BitwiseNotExpression: op = PythonOperator.Invert; break;
                default:
                    throw new NotImplementedException($"Prefix unary operator {node.Kind()} not implemented");
            }
            return new PyAst.UnaryExpression(op, (PyAst.Expression)Visit(node.Operand));
        }

        public override PyAst.Node VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) {
            PythonOperator op;
            switch (node.Kind()) {
                case CSharpSyntaxKind.PostIncrementExpression: op = PythonOperator.Add; break;
                case CSharpSyntaxKind.PostDecrementExpression: op = PythonOperator.Subtract; break;
                default:
                    throw new NotImplementedException($"Postfix unary operator {node.Kind()} not implemented");
            }
            return new PyAst.AugmentedAssignStatement(
                op,
                (PyAst.Expression)Visit(node.Operand),
                new PyAst.ConstantExpression(1)
            );
        }

        public override PyAst.Node VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) {
            return new PyAst.ParenthesisExpression((PyAst.Expression)Visit(node.Expression));
        }

        public override PyAst.Node VisitThisExpression(ThisExpressionSyntax node) {
            return new PyAst.NameExpression("self");
        }
        public override PyAst.Node VisitBaseExpression(BaseExpressionSyntax node) {
                return new PyAst.CallExpression(
                    new PyAst.NameExpression("super"),
                    new PyAst.Arg[] {
                        new PyAst.Arg(new PyAst.NameExpression(_classNamesStack.Peek())),
                        new PyAst.Arg(new PyAst.NameExpression("self"))
                    }
                );
        }

        public override PyAst.Node VisitIdentifierName(IdentifierNameSyntax node) {
            return new PyAst.NameExpression(node.Identifier.Text);
        }
        public override PyAst.Node VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
            return new PyAst.MemberExpression((PyAst.Expression)Visit(node.Expression), node.Name.Identifier.Text);
        }

        public override PyAst.Node VisitCastExpression(CastExpressionSyntax node) {
            var visitedExpr = (PyAst.Expression)Visit(node.Expression);
            if (node.Type is PredefinedTypeSyntax predefined) {
                string conversionFuncName;
                switch (predefined.Keyword.RawKind) {
                    case (int)CSharpSyntaxKind.IntKeyword: conversionFuncName = "int"; break;
                    case (int)CSharpSyntaxKind.BoolKeyword: conversionFuncName = "bool"; break;
                    case (int)CSharpSyntaxKind.FloatKeyword: conversionFuncName = "float"; break;
                    case (int)CSharpSyntaxKind.DoubleKeyword: conversionFuncName = "float"; break;
                    case (int)CSharpSyntaxKind.CharKeyword: conversionFuncName = "chr"; break;
                    default: conversionFuncName = null; break;
                }
                if (conversionFuncName != null) {
                    return new PyAst.CallExpression(
                        new PyAst.NameExpression(conversionFuncName),
                        new [] { new PyAst.Arg(visitedExpr)}
                    );
                }
            }

            return new PyAst.CallExpression(
                new PyAst.MemberExpression(new PyAst.NameExpression("clr"), "Convert"),
                new [] { new PyAst.Arg(visitedExpr), new PyAst.Arg((PyAst.Expression)Visit(node.Type))}
            );
        }

        public override PyAst.Node VisitTypeOfExpression(TypeOfExpressionSyntax node) => Visit(node.Type);

        public override PyAst.Node VisitPredefinedType(PredefinedTypeSyntax node) {
            string convertedTypeName;
            switch (node.Keyword.RawKind) {
                case (int)CSharpSyntaxKind.StringKeyword: convertedTypeName = "str"; break;
                case (int)CSharpSyntaxKind.IntKeyword: convertedTypeName = "int"; break;
                case (int)CSharpSyntaxKind.ObjectKeyword: convertedTypeName = "object"; break;
                case (int)CSharpSyntaxKind.BoolKeyword: convertedTypeName = "bool"; break;
                case (int)CSharpSyntaxKind.FloatKeyword: convertedTypeName = "float"; break;
                case (int)CSharpSyntaxKind.DoubleKeyword: convertedTypeName = "float"; break;
                default:
                    throw new NotImplementedException($"Predefined type {node} not implemented");
            }
            return new PyAst.NameExpression(convertedTypeName);
        }
        public override PyAst.Node VisitQualifiedName(QualifiedNameSyntax node) {
            var convertedLeft = (PyAst.Expression)Visit(node.Left);
            var convertedRight = Visit(node.Right);
            if (convertedRight is PyAst.IndexExpression indexExpr) {
                var memberName = ((PyAst.NameExpression)indexExpr.Target).Name;
                return new PyAst.IndexExpression(
                    new PyAst.MemberExpression(convertedLeft, memberName),
                    indexExpr.Index
                );
            } else if (convertedRight is PyAst.NameExpression nameExpr) {
                return new PyAst.MemberExpression( convertedLeft, nameExpr.Name );
            } else {
                throw new NotImplementedException("VisitQualifiedName only implemented for this 'right' expression.");
            }
        }
        public override PyAst.Node VisitSimpleBaseType(SimpleBaseTypeSyntax node) {
            return Visit(node.Type);
        }
        public override PyAst.Node VisitGenericName(GenericNameSyntax node) {
            var typeArgs = node.TypeArgumentList.Arguments;
            var convertedTypeArgs = typeArgs.Select(a => (PyAst.Expression)Visit(a)).ToArray();
            PyAst.Expression convertedTypeArgsList;
            if (typeArgs.Count > 1) {
                convertedTypeArgsList = new PyAst.TupleExpression(false, convertedTypeArgs);
            } else {
                convertedTypeArgsList = convertedTypeArgs.Single();
            }
            return new PyAst.IndexExpression(new PyAst.NameExpression(node.Identifier.Text), convertedTypeArgsList);
        }
        public override PyAst.Node VisitNullableType(NullableTypeSyntax node) {
            var separatedTypeList = SyntaxFactory.SingletonSeparatedList(node.ElementType);
            var typeArgs = SyntaxFactory.TypeArgumentList(separatedTypeList);
            var converted = SyntaxFactory.QualifiedName(
                 SyntaxFactory.IdentifierName("System"),
                 SyntaxFactory.GenericName("Nullable").WithTypeArgumentList(typeArgs)
            );
            return Visit(converted);
        }

        public override PyAst.Node VisitElementAccessExpression(ElementAccessExpressionSyntax node) {
            if (node.ArgumentList.Arguments.Count > 1) {
                throw new NotImplementedException("VisitElementAccessExpression not implemented for multiple args");
            }
            return new PyAst.IndexExpression(
                (PyAst.Expression)Visit(node.Expression),
                (PyAst.Expression)Visit(node.ArgumentList.Arguments.Single().Expression)
            );
        }

        public override PyAst.Node VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
            return VisitLambdaExpression(new[] { node.Parameter }, node.Body);
        }
        public override PyAst.Node VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
            return VisitLambdaExpression(node.ParameterList.Parameters, node.Body);
        }
        private PyAst.Node VisitLambdaExpression(
                IEnumerable<ParameterSyntax> parameters,
                Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode body) {
            if (body is StatementSyntax) {
                throw new NotImplementedException("Lambda expression with statement body not implemented");
            }
            var convertedParameters = parameters.Select(p => new PyAst.Parameter(p.Identifier.Text)).ToArray();
            return new PyAst.LambdaExpression(
                new PyAst.FunctionDefinition(
                    null,
                    convertedParameters,
                    new PyAst.ReturnStatement((PyAst.Expression)Visit(body))
                )
            );
        }

        public override PyAst.Node VisitInvocationExpression(InvocationExpressionSyntax node) {
            return new PyAst.CallExpression(
                (PyAst.Expression)Visit(node.Expression),
                node.ArgumentList.Arguments.Select(a => (PyAst.Arg)Visit(a)).ToArray()
            );
        }
        public override PyAst.Node VisitArgument(ArgumentSyntax node) {
            var expr = (PyAst.Expression)Visit(node.Expression);
            if (node.NameColon != null) {
                return new PyAst.Arg(node.NameColon.Name.Identifier.ValueText, expr);
            }
            return new PyAst.Arg(expr);
        }

        public override PyAst.Node VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) {
            var args = node.Initializers.Select(i => new PyAst.Arg(
                i.NameEquals?.Name.Identifier.Text ?? ((PyAst.NameExpression)Visit(i.Expression)).Name,
                (PyAst.Expression)Visit(i.Expression)
            )).ToArray();
            return new PyAst.CallExpression( new PyAst.NameExpression("AnonymousObject"), args );
        }

        public override PyAst.Node VisitAssignmentExpression(AssignmentExpressionSyntax node) {
            var leftExpr = (PyAst.Expression)Visit(node.Left);
            var right = (PyAst.Expression)Visit(node.Right);
            PythonOperator op;
            switch (node.Kind()) {
                case CSharpSyntaxKind.SimpleAssignmentExpression:
                    return new PyAst.AssignmentStatement(new[] { leftExpr }, right);
                case CSharpSyntaxKind.AddAssignmentExpression: op = PythonOperator.Add; break;
                case CSharpSyntaxKind.SubtractAssignmentExpression: op = PythonOperator.Subtract; break;
                case CSharpSyntaxKind.MultiplyAssignmentExpression: op = PythonOperator.Multiply; break;
                case CSharpSyntaxKind.DivideAssignmentExpression: op = PythonOperator.Divide; break;
                default:
                    throw new NotImplementedException($"Assignment operator {node.Kind()} not implemented yet");
            }
            return new PyAst.AugmentedAssignStatement(op, leftExpr, right);
        }

        public override PyAst.Node VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) {
            return Visit(node.Declaration);
        }
        public override PyAst.Node VisitVariableDeclaration(VariableDeclarationSyntax node) {
            PyAst.Statement convertInitializedVariable(VariableDeclaratorSyntax initializedVariable) {
                return new PyAst.AssignmentStatement(
                    new[] { new PyAst.NameExpression(initializedVariable.Identifier.Text) },
                    (PyAst.Expression)Visit(initializedVariable.Initializer.Value)
                );
            }
            var assignments = node.Variables
                .Where(a => a.Initializer != null)
                .Select(convertInitializedVariable).ToArray();
            return assignments.Length == 1 ? assignments.Single() : new PyAst.SuiteStatement(assignments);
        }
        public override PyAst.Node VisitExpressionStatement(ExpressionStatementSyntax node) {
            var visited = Visit(node.Expression);
            if (visited is PyAst.AssignmentStatement || visited is PyAst.AugmentedAssignStatement) {
                return visited;
            }
            return new PyAst.ExpressionStatement((PyAst.Expression)visited);
        }

        public override PyAst.Node VisitReturnStatement(ReturnStatementSyntax node) {
            return new PyAst.ReturnStatement((PyAst.Expression)base.Visit(node.Expression));
        }

        public override PyAst.Node VisitIfStatement(IfStatementSyntax node) {
            var ifClauses = new List<PyAst.IfStatementTest>();
            var ifStmt = node;
            StatementSyntax finalElseStmt = null;
            while (ifStmt != null) {
                ifClauses.Add(new PyAst.IfStatementTest(
                    (PyAst.Expression)Visit(ifStmt.Condition),
                    (PyAst.Statement)Visit(ifStmt.Statement)
                ));
                var elseStmt = ifStmt.Else?.Statement;
                ifStmt = elseStmt as IfStatementSyntax;
                if (ifStmt is null) {
                    finalElseStmt = elseStmt;
                }
            }
            return new PyAst.IfStatement(
                ifClauses.ToArray(),
                finalElseStmt is null ? null : (PyAst.Statement)Visit(finalElseStmt)
            );
        }

        public override PyAst.Node VisitSwitchStatement(SwitchStatementSyntax node) {
            var switchedExpr = (PyAst.Expression)Visit(node.Expression);
            IReadOnlyList<StatementSyntax> removeBreaks(SwitchSectionSyntax switchSection) {
                var fixedSection = (SwitchSectionSyntax)new BreakRemovingVisitor().Visit(switchSection);
                return fixedSection.Statements;
            }
            PyAst.IfStatementTest convertSection(SwitchSectionSyntax section) {
                var labels = section.Labels.Cast<CaseSwitchLabelSyntax>();
                PyAst.Expression convertLabel(CaseSwitchLabelSyntax caseLabel) {
                    var equalityExpr = new PyAst.BinaryExpression(
                        PythonOperator.Equals,
                        switchedExpr,
                        (PyAst.Expression)Visit(caseLabel.Value)
                    );
                    return new PyAst.ParenthesisExpression(equalityExpr);
                }
                var test = labels.Select(convertLabel)
                    .Aggregate((left, right) => new PyAst.OrExpression(left, right));
                return new PyAst.IfStatementTest(test, ConvertStatementsToSuiteStmt(removeBreaks(section)));
            }
            var sectionGroups = node.Sections
                .ToLookup(s => s.Labels.Any(a => a.IsKind(CSharpSyntaxKind.DefaultSwitchLabel)));
            var defaultSection = sectionGroups.Where(a => a.Key).SelectMany(a => a).SingleOrDefault();
            var regularSections = sectionGroups.Where(a => !a.Key).SelectMany(a => a).ToArray();
            var ifTests = regularSections.Select(convertSection).ToArray();
            var elseStmt = defaultSection is null
                ? null
                : ConvertStatementsToSuiteStmt(removeBreaks(defaultSection));
            return new PyAst.IfStatement(
                ifTests,
                elseStmt
            );
        }

        public override PyAst.Node VisitForEachStatement(ForEachStatementSyntax node) {
            return new PyAst.ForStatement(
                new PyAst.NameExpression(node.Identifier.Text),
                (PyAst.Expression)Visit(node.Expression),
                (PyAst.Statement)Visit(node.Statement),
                null
            );
        }

        public override PyAst.Node VisitForStatement(ForStatementSyntax node) {
            var whileBodyStmts = new PyAst.Statement[1 + node.Incrementors.Count];
            whileBodyStmts[0] = (PyAst.Statement)Visit(node.Statement);
            for (var incrementorIndex = 0; incrementorIndex < node.Incrementors.Count; incrementorIndex++) {
                whileBodyStmts[incrementorIndex + 1] = (PyAst.Statement)Visit(node.Incrementors[incrementorIndex]);
            }
            var whileStmt = new PyAst.WhileStatement(
                (PyAst.Expression)Visit(node.Condition),
                new PyAst.SuiteStatement(whileBodyStmts),
                null
            );
            if (node.Declaration is null) {
                return whileStmt;
            }
            var assignStmt = (PyAst.Statement)Visit(node.Declaration);
            return new PyAst.SuiteStatement(new PyAst.Statement[] { assignStmt, whileStmt });
        }

        public override PyAst.Node VisitWhileStatement(WhileStatementSyntax node) {
            return new PyAst.WhileStatement(
                (PyAst.Expression)Visit(node.Condition),
                (PyAst.Statement)Visit(node.Statement),
                null
            );
        }
        public override PyAst.Node VisitDoStatement(DoStatementSyntax node) {
            var testExpr = (PyAst.Expression)Visit(node.Condition);
            var body = (PyAst.Statement)Visit(node.Statement);
            var breakOnConditionStmt = new PyAst.IfStatement(
                new[] { new PyAst.IfStatementTest(testExpr, new PyAst.BreakStatement()) },
                null
            );
            return new PyAst.WhileStatement(
                new PyAst.ConstantExpression(true),
                new PyAst.SuiteStatement(new[] { body, breakOnConditionStmt }),
                null
            );
        }

        public override PyAst.Node VisitEmptyStatement(EmptyStatementSyntax node) {
            return new PyAst.SuiteStatement(Array.Empty<PyAst.Statement>());
        }

        public override PyAst.Node VisitBreakStatement(BreakStatementSyntax node) {
            return new PyAst.BreakStatement();
        }
        public override PyAst.Node VisitContinueStatement(ContinueStatementSyntax node) {
            return new PyAst.ContinueStatement();
        }

        public override PyAst.Node VisitUsingStatement(UsingStatementSyntax node) {
            var body = (PyAst.Statement)Visit(node.Statement);
            if (node.Expression is null) {
                var v = node.Declaration.Variables.Single();
                return new PyAst.WithStatement(
                    (PyAst.Expression)Visit(v.Initializer.Value),
                    new PyAst.NameExpression(v.Identifier.Text),
                    body
                );
            }
            return new PyAst.WithStatement((PyAst.Expression)Visit(node.Expression), null, body);
        }

        public override PyAst.Node VisitTryStatement(TryStatementSyntax node) {
            PyAst.TryStatementHandler ConvertCatchBlock(CatchClauseSyntax catchClause) {
                if (catchClause.Declaration is null) {
                    throw new NotImplementedException("Catch statement with no declaration not implemented");
                }
                var hasIdentifier = catchClause.Declaration.Identifier.Text != "";
                return new PyAst.TryStatementHandler(
                    (PyAst.Expression)Visit(catchClause.Declaration.Type),
                    hasIdentifier ? new PyAst.NameExpression(catchClause.Declaration.Identifier.Text) : null,
                    (PyAst.Statement)Visit(catchClause.Block)
                );
            }
            return new PyAst.TryStatement(
                (PyAst.Statement)Visit(node.Block),
                node.Catches.Select(ConvertCatchBlock).ToArray(),
                null,
                node.Finally is null ? null : (PyAst.Statement)Visit(node.Finally.Block)
            );
        }

        public override PyAst.Node VisitThrowStatement(ThrowStatementSyntax node) {
            return new PyAst.RaiseStatement(null, (PyAst.Expression)Visit(node.Expression), null);
        }

        public override PyAst.Node VisitBlock(BlockSyntax node) => ConvertStatementsToSuiteStmt(node.Statements);
        private PyAst.SuiteStatement ConvertStatementsToSuiteStmt(IReadOnlyList<StatementSyntax> statements) {
            var convertedStatements = statements.Select(s => (PyAst.Statement)Visit(s)).ToArray();
            return new PyAst.SuiteStatement(EnsureAtleastOneStatement(convertedStatements));
        }

        public override PyAst.Node VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) {
            var parameters = node.ParameterList.Parameters.Select(p => (PyAst.Parameter)Visit(p)).ToArray();
            var body = node.ExpressionBody is null
                ? (PyAst.SuiteStatement)VisitBlock(node.Body ?? SyntaxFactory.Block())
                : (PyAst.Statement)new PyAst.ReturnStatement((PyAst.Expression)Visit(node.ExpressionBody.Expression));
            return new PyAst.FunctionDefinition(node.Identifier.Text, parameters, body);
        }

        private static readonly System.Reflection.MethodInfo _functionDecoratorsSetter =
                typeof(PyAst.FunctionDefinition).GetMethod("set_Decorators", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        private void AssignDecorators(PyAst.FunctionDefinition funcDef, params PyAst.Expression[] decorators) {
            _functionDecoratorsSetter.Invoke(funcDef, new object[] { decorators });
        }
        public override PyAst.Node VisitMethodDeclaration(MethodDeclarationSyntax node) {
            var parameters = node.ParameterList.Parameters.Select(p => (PyAst.Parameter)Visit(p)).ToArray();
            var isStatic = node.Modifiers.Any(CSharpSyntaxKind.StaticKeyword);
            if (!isStatic && node.Parent is ClassDeclarationSyntax) {
                parameters = InsertSelfParameter(parameters);
            }

            var body = node.ExpressionBody is null
                ? (PyAst.SuiteStatement)VisitBlock(node.Body ?? SyntaxFactory.Block() )
                : (PyAst.Statement)new PyAst.ReturnStatement((PyAst.Expression)Visit(node.ExpressionBody.Expression));
            var funcDef = new PyAst.FunctionDefinition(node.Identifier.Text, parameters, body);
            if (isStatic)
            {
                var decorators = new List<PyAst.Expression> {
                    new PyAst.NameExpression("staticmethod")
                };
                AssignDecorators(funcDef, new PyAst.NameExpression("staticmethod"));
            }
            return funcDef;
        }
        public override PyAst.Node VisitParameter(ParameterSyntax node) {
            var paramsKeyword = CSharpSyntaxKind.ParamsKeyword;
            var isParams = node.Modifiers.Any(a => a.RawKind == (int)paramsKeyword);
            var argKind = isParams ? PyAst.ParameterKind.List : PyAst.ParameterKind.Normal;
            var rslt = new PyAst.Parameter(node.Identifier.Text, argKind);
            if (node.Default != null) {
                rslt.DefaultValue = (PyAst.Expression)Visit(node.Default.Value);
            }
            return rslt;
        }

        public override PyAst.Node VisitConstructorDeclaration(ConstructorDeclarationSyntax node) {
            var parentClass = (ClassDeclarationSyntax)node.Parent;
            var hasBaseList = parentClass.BaseList != null & parentClass.BaseList.Types.Any();
            return VisitConstructorDeclaration(node, parentClass.Identifier, hasBaseList);
        }
        private PyAst.Node VisitConstructorDeclaration(
                ConstructorDeclarationSyntax node,
                SyntaxToken ownerClassIdentifier,
                bool ownerClassHasBases) {
            PyAst.Statement callSuperInitStatement;
            // Without semantic analysis we don't know if everything in the BaseList is an interface
            if (node.Initializer != null || ownerClassHasBases) {
                var callSuper = new PyAst.CallExpression(
                    new PyAst.NameExpression("super"),
                    new PyAst.Arg[] {
                        new PyAst.Arg(new PyAst.NameExpression(ownerClassIdentifier.Text)),
                        new PyAst.Arg(new PyAst.NameExpression("self"))
                    }
                );
                var callInit = new PyAst.CallExpression(
                    new PyAst.MemberExpression(callSuper, "__init__"),
                    node.Initializer?.ArgumentList.Arguments.Select(a => (PyAst.Arg)VisitArgument(a)).ToArray()
                        ?? Array.Empty<PyAst.Arg>()
                );
                callSuperInitStatement = new PyAst.ExpressionStatement(callInit);
            } else {
                callSuperInitStatement = null;
            }
            var parameters = InsertSelfParameter(
                node.ParameterList.Parameters.Select(p => (PyAst.Parameter)Visit(p)).ToArray()
            );
            var body = (PyAst.SuiteStatement)VisitBlock(node.Body);
            if (callSuperInitStatement != null) {
                var newBodyStatements = new[] { callSuperInitStatement }
                    .Concat(body.Statements.Where(a => !(a is PyAst.EmptyStatement))).ToArray();
                body = new PyAst.SuiteStatement(newBodyStatements);
            }
            return new PyAst.FunctionDefinition("__init__", parameters, body);
        }
        private PyAst.Parameter[] InsertSelfParameter(PyAst.Parameter[] parameters) {
            var newArray = new PyAst.Parameter[parameters.Length + 1];
            Array.Copy(parameters, 0, newArray, 1, parameters.Length);
            newArray[0] = new PyAst.Parameter("self");
            return newArray;
        }

        public PyAst.Expression GetInitializerExpression(EqualsValueClauseSyntax node) {
            return node is null ? new PyAst.NameExpression("None") : (PyAst.Expression)Visit(node.Value);
        }
        public override PyAst.Node VisitFieldDeclaration(FieldDeclarationSyntax node) {
            System.Diagnostics.Debug.Assert(node.Modifiers.Any(CSharpSyntaxKind.StaticKeyword));
            var declarator = node.Declaration.Variables.Single();
            return new PyAst.AssignmentStatement(
                new PyAst.Expression[] { new PyAst.NameExpression(declarator.Identifier.Text) },
                GetInitializerExpression(declarator.Initializer)
            );
        }

        public override PyAst.Node VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
            if (node.Modifiers.Any(CSharpSyntaxKind.StaticKeyword)) {
                throw new NotImplementedException("Static properties not implemented");
            }

            var getterAndSetterStatements = new List<PyAst.Statement>();
            PyAst.Statement createFromStatement(PyAst.Statement statement, bool isGetter) {
                var selfParameter = new PyAst.Parameter("self");
                var parameters = isGetter
                    ? new[] { selfParameter }
                    : new[] { selfParameter, new PyAst.Parameter("value") };
                var funcDef = new PyAst.FunctionDefinition(
                    node.Identifier.Text,
                    parameters,
                    statement
                );
                var decoratorExpr = isGetter
                    ? (PyAst.Expression)new PyAst.NameExpression("property")
                    : new PyAst.MemberExpression(new PyAst.NameExpression(node.Identifier.Text), "setter");
                AssignDecorators(funcDef, decoratorExpr);
                return funcDef;
            }
            PyAst.Statement create(ExpressionSyntax getterExpression, bool isGetter) {
                return createFromStatement(
                    new PyAst.ReturnStatement((PyAst.Expression)Visit(getterExpression)),
                    isGetter);
            }
            if (node.AccessorList != null) {
                foreach (var accessor in node.AccessorList.Accessors) {
                    var isGetter = accessor.IsKind(CSharpSyntaxKind.GetAccessorDeclaration);
                    if (!isGetter) {
                        System.Diagnostics.Debug.Assert(accessor.IsKind(CSharpSyntaxKind.SetAccessorDeclaration));
                    }
                    if (accessor.ExpressionBody != null) {
                        getterAndSetterStatements.Add(create(accessor.ExpressionBody.Expression, isGetter));
                    } else if (accessor.Body != null) {
                        var body = (PyAst.Statement)Visit(accessor.Body);
                        getterAndSetterStatements.Add(createFromStatement(body, isGetter));
                    } else {
                        PyAst.Statement body;
                        var backingFieldName = new PyAst.MemberExpression(
                            new PyAst.NameExpression("self"),
                            "_" + node.Identifier.Text
                        );
                        if (isGetter) {
                            body = new PyAst.ReturnStatement(backingFieldName);
                        } else {
                            body = new PyAst.AssignmentStatement(
                                new PyAst.Expression[] { backingFieldName },
                                new PyAst.NameExpression("value")
                            );
                        }
                        getterAndSetterStatements.Add(createFromStatement(body, isGetter));
                        //throw new NotImplementedException("Auto property not implemented yet");
                    }
                }
            } else if (node.ExpressionBody != null) {
                getterAndSetterStatements.Add(create(node.ExpressionBody.Expression, true));
            }
            return getterAndSetterStatements.Count == 1
                ? getterAndSetterStatements.Single()
                : new PyAst.SuiteStatement(getterAndSetterStatements.ToArray());
        }

        private readonly Stack<string> _classNamesStack = new Stack<string>();

        private class FieldInfo {
            public string IdentifierName;
            public EqualsValueClauseSyntax Initializer;
        }
        public override PyAst.Node VisitClassDeclaration(ClassDeclarationSyntax node) {
            _classNamesStack.Push(node.Identifier.Text);
            var bases = node.BaseList?.Types.Select(t => (PyAst.Expression)Visit(t)).ToArray()
                ?? new PyAst.Expression[] { new PyAst.NameExpression("object") };

            var convertedMembers = ConvertClassMembers(
                    node.Members,
                    node.Identifier,
                    hasBases: node.BaseList != null);

            var popped = _classNamesStack.Pop();
            System.Diagnostics.Debug.Assert(popped == node.Identifier.Text);
            return new PyAst.ClassDefinition(
                node.Identifier.Text,
                bases,
                new PyAst.SuiteStatement(convertedMembers)
            );
        }

        private PyAst.Statement[] ConvertClassMembers(
                SyntaxList<MemberDeclarationSyntax> classMembers,
                SyntaxToken classNameIdentifier,
                bool hasBases) {

            // Keep track of fields and auto-properties so we can initialize them in the constructor.
            var instanceFields = new List<FieldInfo>();
            var nonFieldMembers = new List<MemberDeclarationSyntax>();
            var constructorCount = 0;
            foreach (var member in classMembers) {
                if (member is ConstructorDeclarationSyntax) {
                    constructorCount++;
                }
                if (member is FieldDeclarationSyntax field
                        && !field.Modifiers.Any(CSharpSyntaxKind.StaticKeyword)) {
                    foreach (var variable in field.Declaration.Variables) {
                        instanceFields.Add(new FieldInfo {
                            IdentifierName = variable.Identifier.Text,
                            Initializer = variable.Initializer
                        });
                    }
                } else {
                    nonFieldMembers.Add(member);
                }
                if (member is PropertyDeclarationSyntax propDecl && propDecl.AccessorList != null) {
                    var autoGetAccessors = propDecl.AccessorList.Accessors
                        .Where(a => a.IsKind(CSharpSyntaxKind.GetAccessorDeclaration)
                                    && a.ExpressionBody is null
                                    && a.Body is null)
                        .ToArray();
                    if (autoGetAccessors.Any()) {
                        instanceFields.Add(new FieldInfo {
                            IdentifierName = "_" + propDecl.Identifier.Text,
                            Initializer = propDecl.Initializer
                        });
                    }
                }
            }
            IReadOnlyList<MemberDeclarationSyntax> members;
            if (constructorCount == 0 && (instanceFields.Any() || hasBases)) {
                var fakeConstructor = SyntaxFactory.ConstructorDeclaration("").WithBody(SyntaxFactory.Block());
                members = new MemberDeclarationSyntax[] { fakeConstructor }.Concat(nonFieldMembers).ToArray();
            } else {
                members = nonFieldMembers;
            }

            PyAst.Statement convertMember(MemberDeclarationSyntax member) {
                PyAst.Statement visited;
                if (member is ConstructorDeclarationSyntax constructor) {
                    if (constructorCount > 1) {
                        throw new NotImplementedException("Multiple constructors not implemented");
                    }
                    var alreadyAssigned = constructor.Body is null
                            ? new HashSet<string>()
                            : GetDefinitelyAssignedNames(constructor.Body.Statements);
                    var convertedConstructor =
                            (PyAst.FunctionDefinition)VisitConstructorDeclaration(constructor, classNameIdentifier, hasBases);
                    if (instanceFields.Any()) {
                        PyAst.Statement convertField(FieldInfo field) {
                            var memberAccess = new PyAst.MemberExpression(
                                new PyAst.NameExpression("self"),
                                field.IdentifierName
                            );
                            var left = new PyAst.Expression[] { memberAccess };
                            var right = GetInitializerExpression(field.Initializer);
                            return new PyAst.AssignmentStatement(left, right);
                        }
                        var fieldInitializers = instanceFields
                            .Where(f => !alreadyAssigned.Contains(f.IdentifierName))
                            .Select(convertField).ToArray();
                        var bodyStatements = (convertedConstructor.Body is PyAst.SuiteStatement suiteStmt
                            ? suiteStmt.Statements
                            : new[] { convertedConstructor.Body })
                            .Where(a => !(a is PyAst.EmptyStatement));
                        // Field initializers should go first in case the constructor already contains
                        // an assignment statement for the field.
                        var augmentedBodyStatements = fieldInitializers.Concat(bodyStatements).ToArray();
                        augmentedBodyStatements = EnsureAtleastOneStatement(augmentedBodyStatements);
                        convertedConstructor.Body = new PyAst.SuiteStatement(augmentedBodyStatements);
                    }
                    visited = convertedConstructor;
                } else {
                    visited = (PyAst.Statement)Visit(member);
                }
                return visited;
            }

            var convertedMembers = EnsureAtleastOneStatement(
                members.Select(convertMember).Where(a => !(a is PyAst.SuiteStatement s && !s.Statements.Any())).ToArray()
            );
            return convertedMembers;
        }

        private HashSet<string> GetDefinitelyAssignedNames(IEnumerable<StatementSyntax> statements) {
            // TODO: Do a better job of determining definite assignment.
            var rslts = new HashSet<string>();
            foreach (var stmt in statements) {
                if (stmt is ExpressionStatementSyntax exprStmt) {
                    if (exprStmt.Expression is AssignmentExpressionSyntax assign) {
                        if (assign.Left is SimpleNameSyntax simpleName) {
                                rslts.Add(simpleName.Identifier.Text);
                        } else if (assign.Left is MemberAccessExpressionSyntax memberAccess
                            && memberAccess.Expression is ThisExpressionSyntax) {
                            rslts.Add(memberAccess.Name.Identifier.Text);
                        }
                    }
                }
            }
            return rslts;
        }

        private PyAst.Statement[] EnsureAtleastOneStatement(PyAst.Statement[] statements) {
            if (statements.Length == 0) {
                return new PyAst.Statement[] { new PyAst.EmptyStatement() };
            }
            return statements;
        }

        public override PyAst.Node VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) {
            return new PyAst.SuiteStatement(Array.Empty<PyAst.Statement>());
        }

        public override PyAst.Node VisitStructDeclaration(StructDeclarationSyntax node) {
            var convertedMembers = ConvertClassMembers(
                    node.Members,
                classNameIdentifier: node.Identifier,
                hasBases: node.BaseList != null);

            return new PyAst.ClassDefinition(
                node.Identifier.Text,
                Array.Empty<PyAst.Expression>(),
                new PyAst.SuiteStatement(convertedMembers)
            );
        }

        public override PyAst.Node VisitEnumDeclaration(EnumDeclarationSyntax node) {
            PyAst.Statement convertEnumMember(EnumMemberDeclarationSyntax member) {
                var counter = 0;
                PyAst.Expression initializer;
                if (member.EqualsValue is null) {
                    initializer = new PyAst.ConstantExpression(counter++);
                } else {
                    initializer = (PyAst.Expression)Visit(member.EqualsValue.Value);
                }
                return new PyAst.AssignmentStatement(
                    new PyAst.Expression[] { new PyAst.NameExpression(member.Identifier.Text) },
                    initializer
                );
            }
            var bodyStatements = node.Members
                .Select(convertEnumMember).ToArray();
            return new PyAst.ClassDefinition(
                node.Identifier.Text,
                new PyAst.Expression[0],
                new PyAst.SuiteStatement(bodyStatements));
        }

        public override PyAst.Node VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) {
            var converted = node.Members.Select(m => (PyAst.Statement)Visit(m)).ToArray();
            if (converted.Length == 1) {
                return converted.Single();
            }
            return new PyAst.SuiteStatement(converted);
        }

        internal static readonly string UsingStaticMagicString = "USING_STATIC_ERROR";
        public override PyAst.Node VisitUsingDirective(UsingDirectiveSyntax node) {
            var isUsingStatic = !node.StaticKeyword.IsKind(CSharpSyntaxKind.None);
            var moduleName = new PyAst.ModuleName(
                (node.Name.ToString() + (isUsingStatic ? "." + UsingStaticMagicString: "")) .Split('.')
            );
            if (node.Alias != null) {
                var asNames = node.Alias is null ? null : new[] { node.Alias.Name.Identifier.Text };
                return new PyAst.ImportStatement(new[] { moduleName }, asNames, false);
            }
            return new PyAst.FromImportStatement(moduleName, null, null, false, false);
        }

        public override PyAst.Node VisitCompilationUnit(CompilationUnitSyntax node) {
            var usings = node.Usings.Select(u => (PyAst.Statement)Visit(u));
            var members = node.Members.Select(m => (PyAst.Statement)Visit(m));
            var allStatements = usings.Concat(members).ToArray();
            return new PyAst.SuiteStatement(allStatements);
        }







































































        //public override PyAst.Node VisitAliasQualifiedName(AliasQualifiedNameSyntax node) {
        //    return base.VisitAliasQualifiedName(node);
        //}

        //public override PyAst.Node VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) {
        //    return base.VisitAnonymousMethodExpression(node);
        //}

        //public override PyAst.Node VisitArrayRankSpecifier(ArrayRankSpecifierSyntax node) {
        //    return base.VisitArrayRankSpecifier(node);
        //}

        //public override PyAst.Node VisitArrayType(ArrayTypeSyntax node) {
        //    return base.VisitArrayType(node);
        //}

        //public override PyAst.Node VisitAttribute(AttributeSyntax node) {
        //    return base.VisitAttribute(node);
        //}

        //public override PyAst.Node VisitAttributeArgument(AttributeArgumentSyntax node) {
        //    return base.VisitAttributeArgument(node);
        //}

        //public override PyAst.Node VisitAttributeArgumentList(AttributeArgumentListSyntax node) {
        //    return base.VisitAttributeArgumentList(node);
        //}

        //public override PyAst.Node VisitAttributeList(AttributeListSyntax node) {
        //    return base.VisitAttributeList(node);
        //}

        //public override PyAst.Node VisitAttributeTargetSpecifier(AttributeTargetSpecifierSyntax node) {
        //    return base.VisitAttributeTargetSpecifier(node);
        //}

        //public override PyAst.Node VisitAwaitExpression(AwaitExpressionSyntax node) {
        //    return base.VisitAwaitExpression(node);
        //}

        //public override PyAst.Node VisitBadDirectiveTrivia(BadDirectiveTriviaSyntax node) {
        //    return base.VisitBadDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitBaseList(BaseListSyntax node) {
        //    return base.VisitBaseList(node);
        //}

        //public override PyAst.Node VisitBracketedArgumentList(BracketedArgumentListSyntax node) {
        //    return base.VisitBracketedArgumentList(node);
        //}

        //public override PyAst.Node VisitBracketedParameterList(BracketedParameterListSyntax node) {
        //    return base.VisitBracketedParameterList(node);
        //}

        //public override PyAst.Node VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node) {
        //    return base.VisitCasePatternSwitchLabel(node);
        //}

        //public override PyAst.Node VisitCatchDeclaration(CatchDeclarationSyntax node) {
        //    return base.VisitCatchDeclaration(node);
        //}

        //public override PyAst.Node VisitCatchFilterClause(CatchFilterClauseSyntax node) {
        //    return base.VisitCatchFilterClause(node);
        //}

        //public override PyAst.Node VisitCheckedExpression(CheckedExpressionSyntax node) {
        //    return base.VisitCheckedExpression(node);
        //}

        //public override PyAst.Node VisitCheckedStatement(CheckedStatementSyntax node) {
        //    return base.VisitCheckedStatement(node);
        //}

        //public override PyAst.Node VisitClassOrStructConstraint(ClassOrStructConstraintSyntax node) {
        //    return base.VisitClassOrStructConstraint(node);
        //}

        //public override PyAst.Node VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node) {
        //    return base.VisitConditionalAccessExpression(node);
        //}

        //public override PyAst.Node VisitConstantPattern(ConstantPatternSyntax node) {
        //    return base.VisitConstantPattern(node);
        //}

        //public override PyAst.Node VisitConstructorConstraint(ConstructorConstraintSyntax node) {
        //    return base.VisitConstructorConstraint(node);
        //}

        //public override PyAst.Node VisitConstructorInitializer(ConstructorInitializerSyntax node) {
        //    return base.VisitConstructorInitializer(node);
        //}

        //public override PyAst.Node VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node) {
        //    return base.VisitConversionOperatorDeclaration(node);
        //}

        //public override PyAst.Node VisitConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax node) {
        //    return base.VisitConversionOperatorMemberCref(node);
        //}

        //public override PyAst.Node VisitCrefBracketedParameterList(CrefBracketedParameterListSyntax node) {
        //    return base.VisitCrefBracketedParameterList(node);
        //}

        //public override PyAst.Node VisitCrefParameter(CrefParameterSyntax node) {
        //    return base.VisitCrefParameter(node);
        //}

        //public override PyAst.Node VisitCrefParameterList(CrefParameterListSyntax node) {
        //    return base.VisitCrefParameterList(node);
        //}

        //public override PyAst.Node VisitDeclarationExpression(DeclarationExpressionSyntax node) {
        //    return base.VisitDeclarationExpression(node);
        //}

        //public override PyAst.Node VisitDeclarationPattern(DeclarationPatternSyntax node) {
        //    return base.VisitDeclarationPattern(node);
        //}

        //public override PyAst.Node VisitDefaultExpression(DefaultExpressionSyntax node) {
        //    return base.VisitDefaultExpression(node);
        //}

        //public override PyAst.Node VisitDefineDirectiveTrivia(DefineDirectiveTriviaSyntax node) {
        //    return base.VisitDefineDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitDelegateDeclaration(DelegateDeclarationSyntax node) {
        //    return base.VisitDelegateDeclaration(node);
        //}

        //public override PyAst.Node VisitDestructorDeclaration(DestructorDeclarationSyntax node) {
        //    return base.VisitDestructorDeclaration(node);
        //}

        //public override PyAst.Node VisitDiscardDesignation(DiscardDesignationSyntax node) {
        //    return base.VisitDiscardDesignation(node);
        //}

        //public override PyAst.Node VisitElementBindingExpression(ElementBindingExpressionSyntax node) {
        //    return base.VisitElementBindingExpression(node);
        //}

        //public override PyAst.Node VisitEnumDeclaration(EnumDeclarationSyntax node) {
        //    return base.VisitEnumDeclaration(node);
        //}

        //public override PyAst.Node VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node) {
        //    return base.VisitEnumMemberDeclaration(node);
        //}

        //public override PyAst.Node VisitErrorDirectiveTrivia(ErrorDirectiveTriviaSyntax node) {
        //    return base.VisitErrorDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitEventDeclaration(EventDeclarationSyntax node) {
        //    return base.VisitEventDeclaration(node);
        //}

        //public override PyAst.Node VisitEventFieldDeclaration(EventFieldDeclarationSyntax node) {
        //    return base.VisitEventFieldDeclaration(node);
        //}

        //public override PyAst.Node VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node) {
        //    return base.VisitExplicitInterfaceSpecifier(node);
        //}

        //public override PyAst.Node VisitExternAliasDirective(ExternAliasDirectiveSyntax node) {
        //    return base.VisitExternAliasDirective(node);
        //}

        //public override PyAst.Node VisitFixedStatement(FixedStatementSyntax node) {
        //    return base.VisitFixedStatement(node);
        //}

        //public override PyAst.Node VisitFromClause(FromClauseSyntax node) {
        //    return base.VisitFromClause(node);
        //}

        //public override PyAst.Node VisitGenericName(GenericNameSyntax node) {
        //    return base.VisitGenericName(node);
        //}

        //public override PyAst.Node VisitGlobalStatement(GlobalStatementSyntax node) {
        //    return base.VisitGlobalStatement(node);
        //}

        //public override PyAst.Node VisitGotoStatement(GotoStatementSyntax node) {
        //    return base.VisitGotoStatement(node);
        //}

        //public override PyAst.Node VisitGroupClause(GroupClauseSyntax node) {
        //    return base.VisitGroupClause(node);
        //}

        //public override PyAst.Node VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node) {
        //    return base.VisitIfDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitImplicitElementAccess(ImplicitElementAccessSyntax node) {
        //    return base.VisitImplicitElementAccess(node);
        //}

        //public override PyAst.Node VisitImplicitStackAllocArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax node) {
        //    return base.VisitImplicitStackAllocArrayCreationExpression(node);
        //}

        //public override PyAst.Node VisitIncompleteMember(IncompleteMemberSyntax node) {
        //    return base.VisitIncompleteMember(node);
        //}

        //public override PyAst.Node VisitIndexerDeclaration(IndexerDeclarationSyntax node) {
        //    return base.VisitIndexerDeclaration(node);
        //}

        //public override PyAst.Node VisitIndexerMemberCref(IndexerMemberCrefSyntax node) {
        //    return base.VisitIndexerMemberCref(node);
        //}

        //public override PyAst.Node VisitInitializerExpression(InitializerExpressionSyntax node) {
        //    return base.VisitInitializerExpression(node);
        //}

        //public override PyAst.Node VisitInterpolation(InterpolationSyntax node) {
        //    return base.VisitInterpolation(node);
        //}

        //public override PyAst.Node VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node) {
        //    return base.VisitInterpolationAlignmentClause(node);
        //}

        //public override PyAst.Node VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node) {
        //    return base.VisitInterpolationFormatClause(node);
        //}

        //public override PyAst.Node VisitIsPatternExpression(IsPatternExpressionSyntax node) {
        //    return base.VisitIsPatternExpression(node);
        //}

        //public override PyAst.Node VisitJoinClause(JoinClauseSyntax node) {
        //    return base.VisitJoinClause(node);
        //}

        //public override PyAst.Node VisitJoinIntoClause(JoinIntoClauseSyntax node) {
        //    return base.VisitJoinIntoClause(node);
        //}

        //public override PyAst.Node VisitLabeledStatement(LabeledStatementSyntax node) {
        //    return base.VisitLabeledStatement(node);
        //}

        //public override PyAst.Node VisitLetClause(LetClauseSyntax node) {
        //    return base.VisitLetClause(node);
        //}

        //public override PyAst.Node VisitLineDirectiveTrivia(LineDirectiveTriviaSyntax node) {
        //    return base.VisitLineDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitLockStatement(LockStatementSyntax node) {
        //    return base.VisitLockStatement(node);
        //}

        //public override PyAst.Node VisitMakeRefExpression(MakeRefExpressionSyntax node) {
        //    return base.VisitMakeRefExpression(node);
        //}

        //public override PyAst.Node VisitMemberBindingExpression(MemberBindingExpressionSyntax node) {
        //    return base.VisitMemberBindingExpression(node);
        //}

        //public override PyAst.Node VisitNameColon(NameColonSyntax node) {
        //    return base.VisitNameColon(node);
        //}

        //public override PyAst.Node VisitNameEquals(NameEqualsSyntax node) {
        //    return base.VisitNameEquals(node);
        //}

        //public override PyAst.Node VisitNameMemberCref(NameMemberCrefSyntax node) {
        //    return base.VisitNameMemberCref(node);
        //}

        //public override PyAst.Node VisitNullableType(NullableTypeSyntax node) {
        //    return base.VisitNullableType(node);
        //}

        //public override PyAst.Node VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node) {
        //    return base.VisitOmittedArraySizeExpression(node);
        //}

        //public override PyAst.Node VisitOmittedTypeArgument(OmittedTypeArgumentSyntax node) {
        //    return base.VisitOmittedTypeArgument(node);
        //}

        //public override PyAst.Node VisitOperatorDeclaration(OperatorDeclarationSyntax node) {
        //    return base.VisitOperatorDeclaration(node);
        //}

        //public override PyAst.Node VisitOperatorMemberCref(OperatorMemberCrefSyntax node) {
        //    return base.VisitOperatorMemberCref(node);
        //}

        //public override PyAst.Node VisitOrderByClause(OrderByClauseSyntax node) {
        //    return base.VisitOrderByClause(node);
        //}

        //public override PyAst.Node VisitOrdering(OrderingSyntax node) {
        //    return base.VisitOrdering(node);
        //}

        //public override PyAst.Node VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node) {
        //    return base.VisitParenthesizedVariableDesignation(node);
        //}

        //public override PyAst.Node VisitPointerType(PointerTypeSyntax node) {
        //    return base.VisitPointerType(node);
        //}

        //public override PyAst.Node VisitQualifiedCref(QualifiedCrefSyntax node) {
        //    return base.VisitQualifiedCref(node);
        //}

        //public override PyAst.Node VisitQueryBody(QueryBodySyntax node) {
        //    return base.VisitQueryBody(node);
        //}

        //public override PyAst.Node VisitQueryContinuation(QueryContinuationSyntax node) {
        //    return base.VisitQueryContinuation(node);
        //}

        //public override PyAst.Node VisitQueryExpression(QueryExpressionSyntax node) {
        //    return base.VisitQueryExpression(node);
        //}

        //public override PyAst.Node VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node) {
        //    return base.VisitReferenceDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitRefExpression(RefExpressionSyntax node) {
        //    return base.VisitRefExpression(node);
        //}

        //public override PyAst.Node VisitRefType(RefTypeSyntax node) {
        //    return base.VisitRefType(node);
        //}

        //public override PyAst.Node VisitRefTypeExpression(RefTypeExpressionSyntax node) {
        //    return base.VisitRefTypeExpression(node);
        //}

        //public override PyAst.Node VisitRefValueExpression(RefValueExpressionSyntax node) {
        //    return base.VisitRefValueExpression(node);
        //}

        //public override PyAst.Node VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node) {
        //    return base.VisitRegionDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitSelectClause(SelectClauseSyntax node) {
        //    return base.VisitSelectClause(node);
        //}

        //public override PyAst.Node VisitShebangDirectiveTrivia(ShebangDirectiveTriviaSyntax node) {
        //    return base.VisitShebangDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitSingleVariableDesignation(SingleVariableDesignationSyntax node) {
        //    return base.VisitSingleVariableDesignation(node);
        //}

        //public override PyAst.Node VisitSizeOfExpression(SizeOfExpressionSyntax node) {
        //    return base.VisitSizeOfExpression(node);
        //}

        //public override PyAst.Node VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node) {
        //    return base.VisitSkippedTokensTrivia(node);
        //}

        //public override PyAst.Node VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node) {
        //    return base.VisitStackAllocArrayCreationExpression(node);
        //}

        //public override PyAst.Node VisitStructDeclaration(StructDeclarationSyntax node) {
        //    return base.VisitStructDeclaration(node);
        //}

        //public override PyAst.Node VisitThrowExpression(ThrowExpressionSyntax node) {
        //    return base.VisitThrowExpression(node);
        //}

        //public override PyAst.Node VisitTupleElement(TupleElementSyntax node) {
        //    return base.VisitTupleElement(node);
        //}

        //public override PyAst.Node VisitTupleExpression(TupleExpressionSyntax node) {
        //    return base.VisitTupleExpression(node);
        //}

        //public override PyAst.Node VisitTupleType(TupleTypeSyntax node) {
        //    return base.VisitTupleType(node);
        //}

        //public override PyAst.Node VisitTypeArgumentList(TypeArgumentListSyntax node) {
        //    return base.VisitTypeArgumentList(node);
        //}

        //public override PyAst.Node VisitTypeConstraint(TypeConstraintSyntax node) {
        //    return base.VisitTypeConstraint(node);
        //}

        //public override PyAst.Node VisitTypeCref(TypeCrefSyntax node) {
        //    return base.VisitTypeCref(node);
        //}

        //public override PyAst.Node VisitTypeOfExpression(TypeOfExpressionSyntax node) {
        //    return base.VisitTypeOfExpression(node);
        //}

        //public override PyAst.Node VisitTypeParameter(TypeParameterSyntax node) {
        //    return base.VisitTypeParameter(node);
        //}

        //public override PyAst.Node VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node) {
        //    return base.VisitTypeParameterConstraintClause(node);
        //}

        //public override PyAst.Node VisitTypeParameterList(TypeParameterListSyntax node) {
        //    return base.VisitTypeParameterList(node);
        //}

        //public override PyAst.Node VisitUndefDirectiveTrivia(UndefDirectiveTriviaSyntax node) {
        //    return base.VisitUndefDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitUnsafeStatement(UnsafeStatementSyntax node) {
        //    return base.VisitUnsafeStatement(node);
        //}

        //public override PyAst.Node VisitVariableDeclarator(VariableDeclaratorSyntax node) {
        //    return base.VisitVariableDeclarator(node);
        //}

        //public override PyAst.Node VisitWarningDirectiveTrivia(WarningDirectiveTriviaSyntax node) {
        //    return base.VisitWarningDirectiveTrivia(node);
        //}

        //public override PyAst.Node VisitWhenClause(WhenClauseSyntax node) {
        //    return base.VisitWhenClause(node);
        //}

        //public override PyAst.Node VisitWhereClause(WhereClauseSyntax node) {
        //    return base.VisitWhereClause(node);
        //}


        //public override PyAst.Node VisitYieldStatement(YieldStatementSyntax node) {
        //    return base.VisitYieldStatement(node);
        //}

    }


    public class BreakRemovingVisitor : Microsoft.CodeAnalysis.CSharp.CSharpSyntaxRewriter {
        public override SyntaxNode VisitBreakStatement(BreakStatementSyntax node) {
            return SyntaxFactory.EmptyStatement();
        }
    }
}
