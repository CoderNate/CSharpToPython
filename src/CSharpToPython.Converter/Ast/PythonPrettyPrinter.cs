using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronPython.Compiler.Ast
{
    public static class PythonPrettyPrinter
    {
        
        public static string Format(this IEnumerable<Node> nodes, string prefix = "", string separator = ", ", string suffix = "")
        {
            if (nodes == null || !nodes.Any())
                return "";
            return prefix + string.Join(separator, nodes.Select(n => Format(n))) + suffix;
        }

        public static string Format(Node node)
        {
            if (node is Statement) return Format((Statement)node);
            if (node is Expression) return Format((Expression)node);
            if (node is Parameter) return Format((Parameter)node);
            if (node is Arg) return Format((Arg)node);
            if (node is IfStatementTest) return Format((IfStatementTest)node);
            if (node is TryStatementHandler) return Format((TryStatementHandler)node);
            throw new NotImplementedException();
        }

        public static string Format(Statement node)
        {
            if (node is SuiteStatement) return Format((SuiteStatement)node);
            if (node is FromImportStatement) return Format((FromImportStatement)node);
            if (node is ImportStatement) return Format((ImportStatement)node);
            if (node is ClassDefinition) return Format((ClassDefinition)node);
            if (node is FunctionDefinition) return Format((FunctionDefinition)node);
            if (node is IfStatement) return Format((IfStatement)node);
            if (node is ForStatement) return Format((ForStatement)node);
            if (node is WhileStatement) return Format((WhileStatement)node);
            if (node is ReturnStatement) return Format((ReturnStatement)node);
            if (node is ContinueStatement) return Format((ContinueStatement)node);
            if (node is AssignmentStatement) return Format((AssignmentStatement)node);
            if (node is ExpressionStatement) return Format((ExpressionStatement)node);
            if (node is TryStatement) return Format((TryStatement)node);
            if (node is RaiseStatement) return Format((RaiseStatement)node);

            throw new NotImplementedException();
        }
        public static string Format(SuiteStatement node)
        {
            return string.Join("\r\n", node.Statements.Select(s => Format(s)).ToArray());
        }
        public static string Format(FromImportStatement node)
        {
            return "from " + string.Join(".", node.Root.Names) + (node.Names.Any() ? " import " + string.Join(", ", node.Names) : "");
        }
        public static string Format(ImportStatement node)
        {
            return "import " + string.Join(",", node.Names.Select(n => string.Join(".", n.Names)).ToArray()) + " as " + string.Join(", ", node.AsNames);
        }
        public static string Format(ClassDefinition node)
        {
            return "class " + node.Name + node.Bases.Format("(", suffix: ")") + ":"
                + Environment.NewLine + Format(node.Body) + Environment.NewLine;
        }
        public static string Format(FunctionDefinition node)
        {
            return "def " + node.Name + "(" + node.Parameters.Format() + "):"
                + Environment.NewLine + Format(node.Body) + Environment.NewLine;
        }
        public static string Format(IfStatement node)
        {
            return node.Tests.Format(separator: Environment.NewLine) + 
                (node.ElseStatement == null ? "" : Environment.NewLine + "else: " + Environment.NewLine + Format(node.ElseStatement) );
        }
        public static string Format(IfStatementTest node)
        {
            return "if " + Format(node.Test) + ":" + Environment.NewLine + Format(node.Body);
        }
        public static string Format(ForStatement node)
        {
            return "for " + Format(node.Left) + " in " + Format(node.List) + ":" + Environment.NewLine + Format(node.Body);
        }
        public static string Format(WhileStatement node)
        {
            return "while" + Format(node.Test) + ":" + Environment.NewLine + Format(node.Body);
        }
        public static string Format(ReturnStatement node)
        {
            return "return " + Format(node.Expression);
        }
        public static string Format(ContinueStatement node)
        {
            return "continue";
        }
        public static string Format(AssignmentStatement node)
        {
            return node.Left.Format() + " = " + Format(node.Right);
        }
        public static string Format(ExpressionStatement node)
        {
            return Format(node.Expression);
        }
        public static string Format(TryStatement node)
        {
            if (node.Else != null)
            {
                throw new NotImplementedException();
            }
            return "try:" + Environment.NewLine + Format(node.Body) + Environment.NewLine
                + node.Handlers.Format(separator: Environment.NewLine) + Environment.NewLine
                + (node.Finally == null ? "" : Format(node.Finally));
        }
        public static string Format(TryStatementHandler node)
        {
            return "except " + Format(node.Target) + "  " + (node.Test == null ? "" : Format(node.Test)) + ":" + Environment.NewLine
                + Format(node.Body);
        }
        public static string Format(RaiseStatement node)
        {
            return "raise " + Format(node.ExceptType) + "(" + Format(node.Value) + ")";
        }


        public static string Format(Expression node)
        {
            if (node is CallExpression) return Format((CallExpression)node);
            if (node is IndexExpression) return Format((IndexExpression)node);
            if (node is NameExpression) return Format((NameExpression)node);
            if (node is BinaryExpression) return Format((BinaryExpression)node);
            if (node is UnaryExpression) return Format((UnaryExpression)node);
            if (node is MemberExpression) return Format((MemberExpression)node);
            if (node is NameExpression) return Format((NameExpression)node);
            if (node is LambdaExpression) return Format((LambdaExpression)node);
            if (node is ListExpression) return Format((ListExpression)node);
            if (node is DictionaryExpression) return Format((DictionaryExpression)node);
            if (node is SliceExpression) return Format((SliceExpression)node);
            if (node is OrExpression) return Format((OrExpression)node);
            if (node is AndExpression) return Format((AndExpression)node);
            if (node is ConditionalExpression) return Format((ConditionalExpression)node);
            if (node is ConstantExpression) return Format((ConstantExpression)node);
            throw new NotImplementedException();
        }
        public static string Format(CallExpression node)
        {
            return Format(node.Target) + "(" + node.Args.Format() + ")";
        }
        public static string Format(IndexExpression node)
        {
            return Format(node.Target) + "[" + Format(node.Index) + "]";
        }
        public static string Format(LambdaExpression node)
        {
            return "lambda" + node.Function.Parameters.Format(" ") + ":" + Format(node.Function.Body);
        }
        public static string Format(ListExpression node)
        {
            return "[" + node.Items.Format() + "]";
        }
        public static string Format(DictionaryExpression node)
        {
            return "{" + node.Items.Format() + "}";
        }
        public static string Format(SliceExpression node)
        {
            if (node.SliceStep != null)
            {
                throw new NotImplementedException();
            }
            return Format(node.SliceStart) + ":" + Format(node.SliceStop);
        }
        public static string Format(BinaryExpression node)
        {
            return "(" + Format(node.Left) + " " + node.Operator.ToString() + " " + Format(node.Right) + ")";
        }
        public static string Format(UnaryExpression node)
        {
            return node.Op.ToString() + " (" + Format(node.Expression) + ")";
        }
        public static string Format(AndExpression node)
        {
            return Format(node.Left) + " And " + Format(node.Right);
        }
        public static string Format(OrExpression node)
        {
            return Format(node.Left) + " Or " + Format(node.Right);
        }

        public static string Format(Parameter node)
        {
            return node.Name + (node.DefaultValue == null ? "" : " = " + Format(node.DefaultValue));
        }
        public static string Format(Arg node)
        {
            return Format(node.Expression);
        }

        public static string Format(MemberExpression node)
        {
            return(node.Target == null ? "" : Format(node.Target) + ".") + node.Name;
        }
        public static string Format(NameExpression node)
        {
            return node.Name;
        }
        public static string Format(ConditionalExpression node)
        {
            return Format(node.TrueExpression) + " if " + Format(node.Test) + " else " + Format(node.FalseExpression);
        }
        public static string Format(ConstantExpression node)
        {
            if (node.Value == null)
            {
                return "NoValue";
            }
            var preAndPostFix = (node.Value is string ? "\"" : "");
            return preAndPostFix + node.Value.ToString() + preAndPostFix;
        }

    }
}
