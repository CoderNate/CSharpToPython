using System;

namespace CSharpToPython.Web {
    public class Program {
        static void Main(string[] args) {

            // Call ConvertCSharpToPython here just to make sure it doesn't get eliminated as dead code during compile.
            // Eventually I should fix this...
            var converted = ConvertCSharpToPython("class SomeClass{}");
            Console.WriteLine("Hello there World!");
        }

        public static string ConvertCSharpToPython(string code)
        {
            try
            {
                var parsed = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(code).GetRoot();
                var rewritten = MultiLineLambdaRewriter.RewriteMultiLineLambdas(parsed);
                var pyAst = new CSharpToPython.CSharpToPythonConvert().Visit(rewritten);
                var converted = PythonAstPrinter.PrintPythonAst(pyAst);
                return converted;
            } catch (Exception ex) {
                return ex.ToString();
            }
        }
    }
}
