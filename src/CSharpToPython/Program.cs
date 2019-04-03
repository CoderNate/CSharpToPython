using System;
using System.Linq;
using PyAst = IronPython.Compiler.Ast;

namespace CSharpToPython {
    public class Program
    {
        public static void Example() {
            var engineWrapper = new EngineWrapper();
            var engine = engineWrapper.Engine;
            var parsedAst = ParsePythonSource(engine, "clr.Reference[System.Int32](p)").Body;


            var result = ConvertAndRunCode(engineWrapper, "int GetInt() { return 1.0; }");
        }

        private static PyAst.PythonAst ParsePythonSource(Microsoft.Scripting.Hosting.ScriptEngine engine, string code) {
            var src = engine.CreateScriptSourceFromString(code);
            var sourceUnit = Microsoft.Scripting.Hosting.Providers.HostingHelpers.GetSourceUnit(src);
            var langContext = Microsoft.Scripting.Hosting.Providers.HostingHelpers.GetLanguageContext(engine);
            var compilerCtxt = new Microsoft.Scripting.Runtime.CompilerContext(
                    sourceUnit,
                    langContext.GetCompilerOptions(),
                    Microsoft.Scripting.ErrorSink.Default);
            var parser = IronPython.Compiler.Parser.CreateParser(
                compilerCtxt,
                (IronPython.PythonOptions)langContext.Options
            );
            return parser.ParseFile(false);
        }


        public static object ConvertAndRunExpression(EngineWrapper engine, string csharpCode) {
            var csharpAst = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseExpression(csharpCode);
            return ConvertAndRunCode(engine, csharpAst);
        }
        public static object ConvertAndRunStatements(
                EngineWrapper engine,
                string csharpCode,
                string[] requiredImports = null) {
            var wrappedCode = "object WrapperFunc(){\r\n" + csharpCode + "\r\n})";
            var csharpAst = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(wrappedCode).GetRoot();
            return ConvertAndRunCode(engine, csharpAst, requiredImports);
        }
        public static object ConvertAndRunCode(EngineWrapper engine, string csharpCode) {
            var csharpAst = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(csharpCode).GetRoot();
            return ConvertAndRunCode(engine, csharpAst);
        }
        private static object ConvertAndRunCode(
                EngineWrapper engine,
                Microsoft.CodeAnalysis.SyntaxNode csharpAstNode,
                string[] requiredImports = null) {
            var rewritten = MultiLineLambdaRewriter.RewriteMultiLineLambdas(csharpAstNode);
            var pythonAst = new CSharpToPythonConvert().Visit(rewritten);
            var convertedCode = PythonAstPrinter.PrintPythonAst(pythonAst);
            var extraImports = requiredImports is null ? "" : string.Join("\r\n", requiredImports.Select(i => "import " + i));
            convertedCode = "import clr\r\n" + extraImports + "\r\n" + convertedCode;

            if (pythonAst is PyAst.SuiteStatement suiteStmt) {
                var pythonStatements = suiteStmt.Statements
                    .Where(s => !(s is PyAst.FromImportStatement || s is PyAst.ImportStatement)).ToList();
                // If the AST contained only a function definition, run it
                if (pythonStatements.Count == 1 && pythonStatements.Single() is PyAst.FunctionDefinition funcDef) {
                    convertedCode += $"\r\n{funcDef.Name}()";
                }

                if (pythonStatements.Count >= 1 && pythonStatements.All(s => s is PyAst.ClassDefinition)) {
                    var lastClassDef = (PyAst.ClassDefinition)pythonStatements.Last();
                    convertedCode += $"\r\n{lastClassDef.Name}()";
                }
            }
            var scope = engine.Engine.CreateScope();
            var source = engine.Engine.CreateScriptSourceFromString(convertedCode, Microsoft.Scripting.SourceCodeKind.AutoDetect);
            return source.Execute(scope);
        }

        public static string ConvertExpressionCode(string csharpCode) {
            var csharpAst = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseExpression(csharpCode);
            return ConvertCsharpAST(csharpAst);
        }
        public static string ConvertCode(string csharpCode) {
            var csharpAst = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(csharpCode).GetRoot();
            return ConvertCsharpAST(csharpAst);
        }
        private static string ConvertCsharpAST(Microsoft.CodeAnalysis.SyntaxNode csharpAst) {
            var rewritten = MultiLineLambdaRewriter.RewriteMultiLineLambdas(csharpAst);
            var pythonAst = new CSharpToPythonConvert().Visit(rewritten);
            return PythonAstPrinter.PrintPythonAst(pythonAst);
        }
    }

    public class EngineWrapper {
        internal readonly Microsoft.Scripting.Hosting.ScriptEngine Engine = IronPython.Hosting.Python.CreateEngine();
    }
}
