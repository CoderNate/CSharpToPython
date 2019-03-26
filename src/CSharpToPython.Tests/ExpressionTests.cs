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
        public void BinaryOperatorsWork(string code, object expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunExpression(engine, code));
        }

        [Theory]
        [InlineData("!false", true)]
        [InlineData("-(1)", -1)]
        public void UnaryOperatorsWork(string code, object expectedResult) {
            Assert.Equal(expectedResult, Program.ConvertAndRunExpression(engine, code));
        }

        [Fact]
        public void IsExpressionWorks() {
            Assert.Equal(true, Program.ConvertAndRunExpression(engine, "1 is int"));
        }

        [Theory]
        [InlineData("new [] {1, 2}", new [] { 1, 2 })]
        [InlineData("new int[] {1, 2}", new [] { 1, 2 })]
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

    }
}
