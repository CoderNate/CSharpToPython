using System;
using Xunit;

namespace CSharpToPython.Tests {
    public class ExpressionTests {

        private readonly EngineWrapper engine = new EngineWrapper();

        [Fact]
        public void DoubleConversionWorks() {
            var rslt = Program.ConvertAndRunExpression(engine, "1.0");
            Assert.Equal(1.0, rslt);
            Assert.NotEqual(1, rslt);
        }

        [Theory]
        [InlineData("1", 1)]
        [InlineData("\"Hello\"", "Hello")]
        [InlineData("\"Something \\\"in\\\" quotes\"", "Something \"in\" quotes")]
        [InlineData("true", true)]
        [InlineData("null", null)]
        public void LiteralConversionWorks(string code, object expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunExpression(engine, code));
        }

        [Theory]
        [InlineData("$\"One {1} two {2}\"", "One 1 two 2")]
        [InlineData("$\"{1}\"", "1")]
        [InlineData("$\"{{1}}\"", "{1}")]
        public void InterpolatedStringsWork(string code, string expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunExpression(engine, code));
        }

        [Theory]
        [InlineData("@\"a\\b\"", "a\\b")]
        [InlineData("@\"a\"\"b\"", "a\"b")]
        public void RawStringsWork(string code, string expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunExpression(engine, code));
        }

        [Theory]
        [InlineData("1+1", 2)]
        [InlineData("1-1", 0)]
        [InlineData("true && true", true)]
        [InlineData("true || false", true)]
        [InlineData("1 == 1", true)]
        [InlineData("1 != 2", true)]
        [InlineData("1 >= 1", true)]
        [InlineData("1 > 0", true)]
        [InlineData("1 <= 1", true)]
        [InlineData("1 < 2", true)]
        [InlineData("3 % 2", 3 % 2)]
        [InlineData("4 << 1", 4 << 1)]
        [InlineData("4 >> 1", 4 >> 1)]
        [InlineData("3 & 1", 3 & 1)]
        [InlineData("2 | 1", 2 | 1)]
        [InlineData("2 ^ 1", 2 ^ 1)]
        [InlineData("null ?? new int[0]", new int [0])]
        [InlineData("new int[0] ?? null", null)] // This does NOT match C#...
        [InlineData("new [] {1} ?? null", new[] { 1 })]
        public void BinaryOperatorsWork(string code, object expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunExpression(engine, code));
        }

        [Theory]
        [InlineData("!false", true)]
        [InlineData("-(1)", -1)]
        [InlineData("~(5)", ~5)]
        public void UnaryOperatorsWork(string code, object expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunExpression(engine, code));
        }

        [Theory]
        [InlineData("(double)1", 1.0)]
        [InlineData("(int)1.2", 1)]
        [InlineData("(char)97", "a")]
        [InlineData("(object)1", 1)]
        public void CastsWork(string code, object expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunStatements(engine, "return " + code));
        }

        [Fact]
        public void IsExpressionWorks() {
            Assert.Equal(true, Program.ConvertAndRunExpression(engine, "1 is int"));
        }

        [Theory]
        [InlineData("new [] {1, 2}", new [] { 1, 2 })]
        [InlineData("new int[] {1, 2}", new [] { 1, 2 })]
        [InlineData("new int[0]", new int[0])]
        public void ArraysWork(string code, object expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunExpression(engine, code));
        }

        [Fact]
        public void ArrayIndexingWorks() {
            Assert.Equal(1, Program.ConvertAndRunExpression(engine, "new [] {1, 2}[0]"));
        }

        [Theory]
        [InlineData("true ? 1 : 2", 1)]
        public void TernaryExpressionWorks(string code, object expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunExpression(engine, code));
        }

        [Fact]
        public void SingleArgumentLambdaWorks() {
            Assert.Equal(1, Program.ConvertAndRunExpression(engine, "(a => a)(1)"));
        }

        [Fact]
        public void MultiArgumentLambdaWorks() {
            Assert.Equal(2, Program.ConvertAndRunExpression(engine, "((a, b) => a + b)(1, 1)"));
        }

        [Theory]
        [InlineData("new { a = 2 }", "AnonymousObject(a = 2)")]
        [InlineData("new { a }", "AnonymousObject(a = a)")]
        public void AnonymousObjectsWork(string code, string expected) {
            var converted = Program.ConvertExpressionCode(code);
            Assert.Equal(expected, converted);
        }
    }
}
