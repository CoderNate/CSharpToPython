using System;
using System.Collections.Generic;
using System.Linq;
using PyAst = IronPython.Compiler.Ast;

namespace CSharpToPython {
    public class PythonAstPrinter {
        private int IndentLevel;
        public readonly System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

        public static string PrintPythonAst(PyAst.Node node) {
            var printer = new PythonAstPrinter();
            printer.Visit(node);
            return printer.stringBuilder.ToString();
        }

        private void AppendWithIndentation(string line)
                => stringBuilder.Append(new string(' ', IndentLevel * 4) + line);
        private void AppendLineWithIndentation(string line)
                => stringBuilder.AppendLine(new string(' ', IndentLevel * 4) + line);

        private string VisitExpressionsList(IEnumerable<PyAst.Expression> nodes) {
            return string.Join(", ", nodes.Select(n => Visit(n)));
        }

        public string Visit(PyAst.Expression node) {
            switch (node) {
                case PyAst.AndExpression n: return Visit(n);
                case PyAst.BackQuoteExpression n: return Visit(n);
                case PyAst.BinaryExpression n: return Visit(n);
                case PyAst.CallExpression n: return Visit(n);
                case PyAst.ConditionalExpression n: return Visit(n);
                case PyAst.ConstantExpression n: return Visit(n);
                case PyAst.DictionaryComprehension n: return Visit(n);
                case PyAst.DictionaryExpression n: return Visit(n);
                case PyAst.ErrorExpression n: return Visit(n);
                case PyAst.GeneratorExpression n: return Visit(n);
                case PyAst.IndexExpression n: return Visit(n);
                case PyAst.LambdaExpression n: return Visit(n);
                case PyAst.ListComprehension n: return Visit(n);
                case PyAst.ListExpression n: return Visit(n);
                case PyAst.MemberExpression n: return Visit(n);
                case PyAst.NameExpression n: return Visit(n);
                case PyAst.OrExpression n: return Visit(n);
                case PyAst.ParenthesisExpression n: return Visit(n);
                case PyAst.SetComprehension n: return Visit(n);
                case PyAst.SetExpression n: return Visit(n);
                case PyAst.SliceExpression n: return Visit(n);
                case PyAst.TupleExpression n: return Visit(n);
                case PyAst.UnaryExpression n: return Visit(n);
                case PyAst.YieldExpression n: return Visit(n);
                default:
                    throw new NotImplementedException($"Printing of expression node {node} not implemented");
            }
        }
        public string Visit(PyAst.AndExpression node) => $"({Visit(node.Left)} and {Visit(node.Right)})";
        public string Visit(PyAst.BackQuoteExpression node) => throw CreateNotImplementedEx();
        public string Visit(PyAst.BinaryExpression node) {
            string operatorText;
            switch (node.Operator) {
                case IronPython.Compiler.PythonOperator.Add: operatorText = "+"; break;
                case IronPython.Compiler.PythonOperator.Subtract: operatorText = "-"; break;
                case IronPython.Compiler.PythonOperator.Multiply: operatorText = "*"; break;
                case IronPython.Compiler.PythonOperator.Divide: operatorText = "/"; break;
                case IronPython.Compiler.PythonOperator.Equal: operatorText = "=="; break;
                case IronPython.Compiler.PythonOperator.NotEqual: operatorText = "!="; break;
                case IronPython.Compiler.PythonOperator.GreaterThan: operatorText = ">"; break;
                case IronPython.Compiler.PythonOperator.GreaterThanOrEqual: operatorText = ">="; break;
                case IronPython.Compiler.PythonOperator.LessThan: operatorText = "<"; break;
                case IronPython.Compiler.PythonOperator.LessThanOrEqual: operatorText = "<="; break;
                case IronPython.Compiler.PythonOperator.Mod: operatorText = "%"; break;
                case IronPython.Compiler.PythonOperator.LeftShift: operatorText = "<<"; break;
                case IronPython.Compiler.PythonOperator.RightShift: operatorText = ">>"; break;
                case IronPython.Compiler.PythonOperator.BitwiseAnd: operatorText = "&"; break;
                case IronPython.Compiler.PythonOperator.BitwiseOr: operatorText = "|"; break;
                case IronPython.Compiler.PythonOperator.ExclusiveOr: operatorText = "^"; break;
                default:
                    throw new NotImplementedException($"Printing of operator {node.Operator} not implemented");
            }
            return $"({Visit(node.Left)} {operatorText} {Visit(node.Right)})";
        }
        public string Visit(PyAst.CallExpression node) {
            return $"{Visit(node.Target)}({string.Join(", ", node.Args.Select(a => Visit(a))) })";
        }
        public string Visit(PyAst.ConditionalExpression node) {
            return $"({Visit(node.TrueExpression)} if {Visit(node.Test)} else {Visit(node.FalseExpression)})";
        }

        private static readonly string[] charsToEscape = new [] { '\\', '\'', '\"', '\t', '\r', '\n', }
            .Select(a => a.ToString()).ToArray();

        public string Visit(PyAst.ConstantExpression node) {
            string formatString(string rawString) {
                foreach (var charToEscape in charsToEscape) {
                    if (rawString.Contains(charToEscape)) {
                        rawString = rawString.Replace(charToEscape, "\\" + charToEscape);
                    }
                }
                return $"\"{ rawString }\"";
            }
            switch (node.Value) {
                case double val:
                    if (Math.Truncate(val) == val) {
                        // If we don't do this, the double value 1.0 will be output as integer 1
                        return val.ToString("0.0");
                    }
                    return node.Value.ToString();
                case char val:
                    return formatString(val.ToString());
                case string val:
                    return formatString(val);
                case int val:
                    return node.Value.ToString();
                case bool val:
                    return val ? "True" : "False";
                case null:
                    return "None";
                default:
                    throw new NotImplementedException($"Printing of constant expression {node.Value.GetType()} not implemented");
            }
        }
        public string Visit(PyAst.DictionaryComprehension node) => throw CreateNotImplementedEx();
        public string Visit(PyAst.DictionaryExpression node) => throw CreateNotImplementedEx();
        public string Visit(PyAst.ErrorExpression node) => throw CreateNotImplementedEx();
        public string Visit(PyAst.GeneratorExpression node) => throw CreateNotImplementedEx();
        public string Visit(PyAst.IndexExpression node) => $"{Visit(node.Target)}[{Visit(node.Index)}]";
        public string Visit(PyAst.LambdaExpression node) {
            var args = string.Join(", ", node.Function.Parameters.Select(p => Visit(p)));
            var convertedExpr = Visit(((PyAst.ReturnStatement)node.Function.Body).Expression);
            return $"lambda { args }: {convertedExpr}";
        }
        public string Visit(PyAst.ListComprehension node) => throw CreateNotImplementedEx();
        public string Visit(PyAst.ListExpression node) => $"[{ VisitExpressionsList(node.Items)}]";
        public string Visit(PyAst.MemberExpression node) => $"{Visit(node.Target)}.{node.Name}";
        public string Visit(PyAst.NameExpression node) => node.Name;
        public string Visit(PyAst.OrExpression node) => $"({Visit(node.Left)} or {Visit(node.Right)})";
        public string Visit(PyAst.ParenthesisExpression node) => $"({Visit(node.Expression)})";
        public string Visit(PyAst.SetComprehension node) => throw CreateNotImplementedEx();
        public string Visit(PyAst.SetExpression node) => throw CreateNotImplementedEx();
        public string Visit(PyAst.SliceExpression node) => throw CreateNotImplementedEx();
        public string Visit(PyAst.TupleExpression node) => $"({VisitExpressionsList(node.Items)})";
        public string Visit(PyAst.UnaryExpression node) {
            string operatorText;
            switch (node.Op) {
                case IronPython.Compiler.PythonOperator.Add: operatorText = "+"; break;
                case IronPython.Compiler.PythonOperator.Subtract: operatorText = "-"; break;
                case IronPython.Compiler.PythonOperator.Invert: operatorText = "~"; break;
                case IronPython.Compiler.PythonOperator.Not:
                    return $"(not {Visit(node.Expression)})";
                default:
                    throw new NotImplementedException($"Printing of operator {node.Op} not implemented");
            }
            return $"{operatorText}{Visit(node.Expression)}";
        }
        public string Visit(PyAst.YieldExpression node) => throw CreateNotImplementedEx();


        public void Visit(PyAst.Statement node) {
            switch (node) {
                case PyAst.AssertStatement n: Visit(n); return;
                case PyAst.AssignmentStatement n: Visit(n); return;
                case PyAst.AugmentedAssignStatement n: Visit(n); return;
                case PyAst.BreakStatement n: Visit(n); return;
                case PyAst.ClassDefinition n: Visit(n); return;
                case PyAst.ContinueStatement n: Visit(n); return;
                case PyAst.DelStatement n: Visit(n); return;
                case PyAst.EmptyStatement n: Visit(n); return;
                case PyAst.ExecStatement n: Visit(n); return;
                case PyAst.ExpressionStatement n: Visit(n); return;
                case PyAst.ForStatement n: Visit(n); return;
                case PyAst.FromImportStatement n: Visit(n); return;
                case PyAst.FunctionDefinition n: Visit(n); return;
                case PyAst.GlobalStatement n: Visit(n); return;
                case PyAst.IfStatement n: Visit(n); return;
                case PyAst.ImportStatement n: Visit(n); return;
                case PyAst.PrintStatement n: Visit(n); return;
                //case PyAst.PythonAst n: Visit(n); return;
                case PyAst.RaiseStatement n: Visit(n); return;
                case PyAst.ReturnStatement n: Visit(n); return;
                case PyAst.SuiteStatement n: Visit(n); return;
                case PyAst.TryStatement n: Visit(n); return;
                case PyAst.WhileStatement n: Visit(n); return;
                case PyAst.WithStatement n: Visit(n); return;
                default:
                    throw new NotImplementedException($"Printing of statement {node} not implemented");
            }
        }

        public void Visit(PyAst.AssertStatement node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.AssignmentStatement node) {
            AppendLineWithIndentation($"{VisitExpressionsList(node.Left)} = {Visit(node.Right)}");
        }
        public void Visit(PyAst.AugmentedAssignStatement node) {
            string op;
            switch (node.Operator) {
                case IronPython.Compiler.PythonOperator.Add: op = "+="; break;
                case IronPython.Compiler.PythonOperator.Subtract: op = "-="; break;
                case IronPython.Compiler.PythonOperator.Multiply: op = "*="; break;
                case IronPython.Compiler.PythonOperator.Divide: op = "/="; break;
                default:
                    throw CreateNotImplementedEx();
            }
            AppendLineWithIndentation($"{Visit(node.Left)} {op} {Visit(node.Right)}");
        }
        public void Visit(PyAst.BreakStatement node)=> AppendLineWithIndentation("break");
        public void Visit(PyAst.ClassDefinition node) {
            WriteDecorators(node.Decorators ?? Array.Empty<PyAst.Expression>());
            var basesPart = node.Bases.Any() ? $"({VisitExpressionsList(node.Bases)})" : "";
            AppendLineWithIndentation($"class {node.Name}{basesPart}:");
            using (new Indenter(this)) {
                Visit(node.Body);
            }
        }
        private void WriteDecorators(IList<PyAst.Expression> decorators) {
            foreach (var decorator in decorators) {
                AppendLineWithIndentation("@" + Visit(decorator));
            }
        }
        public void Visit(PyAst.ContinueStatement node) => AppendLineWithIndentation("continue");
        public void Visit(PyAst.DelStatement node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.EmptyStatement node) => AppendLineWithIndentation("pass");
        public void Visit(PyAst.ExecStatement node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.ExpressionStatement node) => AppendLineWithIndentation(Visit(node.Expression));
        public void Visit(PyAst.ForStatement node) {
            AppendLineWithIndentation($"for {Visit(node.Left)} in {Visit(node.List)}:");
            using (new Indenter(this)) {
                Visit(node.Body);
            }
        }
        public void Visit(PyAst.FromImportStatement node) {
            var aliasPart = node.AsNames is null ? "" : $" as {node.AsNames.Single()}";
            var isUsingStatic = node.Root.Names.Contains(CSharpToPythonConvert.UsingStaticMagicString);
            var modName = FormatDottedName(
                node.Root,
                isUsingStatic ? new[] { CSharpToPythonConvert.UsingStaticMagicString } : Array.Empty<string>()
            );
            var usingStaticWarningComment = isUsingStatic ? " #ERROR: Was using static directive" : "";
            AppendLineWithIndentation($"from {modName} import *{aliasPart}{usingStaticWarningComment}");
        }
        private string FormatDottedName(PyAst.DottedName name, params string[] namesToRemove) {
            var names = namesToRemove.Any() ? name.Names.Except(namesToRemove).ToList() : name.Names;
            return string.Join(".", names);
        }
        public void Visit(PyAst.FunctionDefinition node) {
            WriteDecorators(node.Decorators ?? Array.Empty<PyAst.Expression>());
            AppendLineWithIndentation($"def {node.Name}({ string.Join(", ", node.Parameters.Select(p => Visit(p)).ToArray()) }):");
            using (new Indenter(this)) {
                Visit(node.Body);
            }
        }
        public void Visit(PyAst.GlobalStatement node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.IfStatement node) {
            var isFirst = true;
            foreach (var ifTest in node.Tests) {
                AppendLineWithIndentation($"{(isFirst ? "if" : "elif")} {Visit(ifTest.Test)}:");
                using (new Indenter(this)) {
                    Visit(ifTest.Body);
                }
                isFirst = false;
            }
            if (node.ElseStatement != null) {
                AppendLineWithIndentation("else:");
                using (new Indenter(this)) {
                    Visit(node.ElseStatement);
                }
            }
        }
        public void Visit(PyAst.ImportStatement node) {
            if (node.AsNames.Count > 1 || node.Names.Count > 1) {
                throw CreateNotImplementedEx();
            }
            var modName = FormatDottedName(node.Names.Single());
            AppendLineWithIndentation($"import {modName} as {node.AsNames.Single()}");
        }
        public void Visit(PyAst.PrintStatement node)=> throw CreateNotImplementedEx();
        //public void Visit(PyAst.PythonAst node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.RaiseStatement node) {
            AppendLineWithIndentation("raise" + (node.Value is null ? "" : " " + Visit(node.Value)));
        }
        public void Visit(PyAst.ReturnStatement node) {
            AppendLineWithIndentation("return" + (node.Expression is null ? "" : " " + Visit(node.Expression)));
        }
        public void Visit(PyAst.SuiteStatement node) {
            foreach (var stmt in node.Statements) {
                Visit(stmt);
            }
        }
        public void Visit(PyAst.TryStatement node) {
            AppendLineWithIndentation("try:");
            using (new Indenter(this)) {
                Visit(node.Body);
            }
            foreach (var handler in node.Handlers) {
                var targetPart = (handler.Target is null ? "" : " as " + Visit(handler.Target));
                AppendLineWithIndentation($"except {Visit(handler.Test)}{targetPart}:");
                using (new Indenter(this)) {
                    Visit(handler.Body);
                }
            }
            if (node.Finally != null)
            {
                AppendLineWithIndentation("finally:");
                using (new Indenter(this))
                {
                    Visit(node.Finally);
                }
            }
        }
        public void Visit(PyAst.WhileStatement node) {
            AppendLineWithIndentation($"while {Visit(node.Test)}:");
            using (new Indenter(this)) {
                Visit(node.Body);
            }
        }
        public void Visit(PyAst.WithStatement node) {
            var asClause = node.Variable is null ? "" : " as " + Visit(node.Variable);
            AppendLineWithIndentation($"with {Visit(node.ContextManager)}{asClause}:");
            using (new Indenter(this)) {
                Visit(node.Body);
            }
        }
        public string Visit(PyAst.Arg node) => (node.Name is null ? "" : node.Name + " = ") + Visit(node.Expression);
        public void Visit(PyAst.ComprehensionFor node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.ComprehensionIf node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.DottedName node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.IfStatementTest node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.ModuleName node)=> throw CreateNotImplementedEx();
        public string Visit(PyAst.Parameter node) {
            var maybeStar = node.IsList ? "*" : "";
            return maybeStar + node.Name + (node.DefaultValue == null ? "" : $" = {Visit(node.DefaultValue)}");
        }
        public void Visit(PyAst.RelativeModuleName node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.SublistParameter node)=> throw CreateNotImplementedEx();
        public void Visit(PyAst.TryStatementHandler node)=> throw CreateNotImplementedEx();


        public void Visit(PyAst.Node node) {
            switch (node) {
                case PyAst.Expression n: stringBuilder.Append(Visit(n)); return;
                case PyAst.Statement n: Visit(n); return;
                default:
                    throw new NotImplementedException($"Printing of node {node} is not implemented");
            }
        }

        public class Indenter : IDisposable {
            public Indenter(PythonAstPrinter printer) {
                Printer = printer;
                printer.IndentLevel++;
            }

            public PythonAstPrinter Printer { get; }

            public void Dispose() {
                Printer.IndentLevel--;
            }
        }

        private NotImplementedException CreateNotImplementedEx(
                [System.Runtime.CompilerServices.CallerMemberName] string caller = null) {
            return new NotImplementedException(caller);
        }
    }
}
