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
                var converter = new CSharpToPython.CSharpToPythonConvert();
                var pyAst = converter.Visit(parsed);
                var printer = new CSharpToPython.PythonAstPrinter();
                printer.Visit(pyAst);
                var converted = printer.stringBuilder.ToString();
                return converted;
            } catch (Exception ex) {
                return ex.ToString();
            }
        }
    }
}
