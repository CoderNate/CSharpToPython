using System;
using System.Collections.Generic;
using Xunit;

namespace CSharpToPython.Tests {
    public class FunctionTests {

        private readonly EngineWrapper engine = new EngineWrapper();

        [Fact]
        public void BasicFunctionWorks() {
            var rslt = Program.ConvertAndRunCode(engine, "int GetInt() { return 1.0; }");
            Assert.Equal(1.0, rslt);
        }

        [Fact]
        public void FunctionWithParameterWorks() {
            var rslt = Program.ConvertAndRunCode(engine, "int GetInt(double arg = 1.0) { return arg; }");
            Assert.Equal(1.0, rslt);
        }

        /// <summary>
        /// Similar to <see cref="ClassTests.ParamsMethodParameterWorks"/> except that test passes arguments
        /// </summary>
        [Fact]
        public void FunctionWithParamsParameterWorks() {
            var rslt = Program.ConvertAndRunCode(engine, "int GetInt(params int[] args) { return args; }");
            Assert.Equal(new int[0], rslt);
        }

        [Fact]
        public void ExpressionBodiedFunctionWorks() {
            var rslt = Program.ConvertAndRunCode(engine, "int GetInt() => 1");
            Assert.Equal(1, rslt);
        }

        [Fact]
        public void LocalFunctionsWork() {
            var code = @"
int GetInt() {
   int innerGetInt() => 1;
   return innerGetInt();
}";
            var rslt = Program.ConvertAndRunCode(engine, code);
            Assert.Equal(1, rslt);
        }
    }
}
