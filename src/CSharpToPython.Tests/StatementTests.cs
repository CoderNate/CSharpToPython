using System;
using Xunit;

namespace CSharpToPython.Tests {
    public class StatementTests {

        private readonly EngineWrapper engine = new EngineWrapper();

        [Fact]
        public void VariableDeclarationStatementWithInitializerWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "var a = 1; return a;");
            Assert.Equal(1, rslt);
        }
        [Fact]
        public void VariableDeclarationStatementWithoutInitializerWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "int a; a = 1; return a;");
            Assert.Equal(1, rslt);
        }

        [Fact]
        public void CompoundAssignmentStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "int a = 1, b = 2; return b;");
            Assert.Equal(2, rslt);
        }

        [Fact]
        public void AugmentedAssignmentWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "int a = 1; a +=1; return a;");
            Assert.Equal(2, rslt);
        }

        [Fact(Skip = "Not implemented yet")]
        public void UnaryPlusPlusWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "int a = 1; return a++;");
            Assert.Equal(2, rslt);
        }

        [Fact(Skip = "Not implemented yet")]
        public void AssignnmentExpressionWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "int a, b = (a = 2); return a;");
            Assert.Equal(2, rslt);
        }

        [Fact]
        public void SimpleIfStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "if (true) { return 1; } return 2;");
            Assert.Equal(1, rslt);
        }

        [Fact]
        public void SimpleIfElseStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "if (true) { return 1; } else { return 2; }");
            Assert.Equal(1, rslt);
        }

        [Fact]
        public void EmptyIfStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "if (true) { } else { } return 0; ");
            Assert.Equal(0, rslt);
        }

        [Fact]
        public void IfElseIfStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "if (false) { return 1; } else if (true) { return 2; } else { return 0; }");
            Assert.Equal(2, rslt);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        public void SwitchStatementWorks(int input, int expectedOutput) {
            var code = $@"
var x = 0;
switch({input}) {{
  case 1:
    return 1;
  case 2:
    x = 2;
    break;
  default:
    return 3;
}}
return x;";
            var rslt = Program.ConvertAndRunStatements(engine, code);
            Assert.Equal(expectedOutput, rslt);
        }

        [Fact]
        public void ForEachStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "var total = 0; foreach (var i in new [] {1, 2}) { total = total + i; } return total;");
            Assert.Equal(3, rslt);
        }

        [Fact]
        public void ForStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "var total = 0; for (var i = 0; i < 3; i = i + 1) { total = total + i; } return total;");
            Assert.Equal(3, rslt);
        }
        [Fact]
        public void WhileStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "var x = true; while (x) {x = false } return x;");
            Assert.Equal(false, rslt);
        }
        [Fact]
        public void DoStatementWorks() {
            var code = "var x = true; do { x = false; break; } while(true); return x;";
            var rslt = Program.ConvertAndRunStatements(engine, code);
            Assert.Equal(false, rslt);
        }

        [Fact]
        public void TryCatchStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, "try { throw new Exception(); } catch (Exception ex) { return 1; }");
            Assert.Equal(1, rslt);
        }
        [Fact]
        public void TryCatchStatementWorks2() {
            var rslt = Program.ConvertAndRunStatements(engine, "try { throw new Exception(); } catch (Exception) { return 1; }");
            Assert.Equal(1, rslt);
        }

        [Fact]
        public void ThrowWorks() {
            var code = @"
throw new System.Exception(""Uhoh"");
";
            Assert.Throws<Exception>(
                () => Program.ConvertAndRunStatements(engine, code, requiredImports: new[] { "System" })
            );
        }

        [Fact]
        public void UsingStatementWorks() {
            var code = @"
using (var a = null) {
    return 1;
}";
            // Assert that executing the using statement attempts to call __exit__
            var ex = Assert.Throws<MissingMemberException>(() => Program.ConvertAndRunStatements(engine, code));
            Assert.Contains("'NoneType' object has no attribute '__exit__'", ex.Message);
        }
        [Fact]
        public void UsingStatementWithoutVariableWorks() {
            var code = @"
using (null) {
    return 1;
}";
            // Assert that executing the using statement attempts to call __exit__
            var ex = Assert.Throws<MissingMemberException>(() => Program.ConvertAndRunStatements(engine, code));
            Assert.Contains("'NoneType' object has no attribute '__exit__'", ex.Message);
        }

        [Fact]
        public void EmptySemicolonStatementWorks() {
            var rslt = Program.ConvertAndRunStatements(engine, ";; return 1");
            Assert.Equal(1, rslt);
        }

        [Fact]
        public void StatementBodiedLambdaWorksInsideIfStmt() {
            var code = @"
            object rslt;
            if (true)
                rslt = ((int a) => { return a; })(1);
            return rslt;";
            var rslt = Program.ConvertAndRunStatements(engine, code);
            Assert.Equal(1, rslt);
        }
        [Fact]
        public void StatementBodiedLambdaWorksInsideIfStmt2() {
            var code = @"
            object rslt;
            if (true)
                rslt = (a => { return a; })(1);
            return rslt;";
            var rslt = Program.ConvertAndRunStatements(engine, code);
            Assert.Equal(1, rslt);
        }
        [Fact]
        public void StatementBodiedLambdaWorksInsideExpressionBodiedMethod() {
            var code = @"
class A {
int SomeMethod() => ((int a) => { a = a + 1; return a; })(0);
}";
            dynamic classObj = Program.ConvertAndRunCode(engine, code);
            object rslt = classObj.SomeMethod();
            Assert.Equal(1, rslt);
        }
        [Fact]
        public void StatementBodiedLambdaWorksInsideMethod() {
            var code = @"
class A {
void SomeMethod() {
var x = (int a) => { a = a + 1; return a; }(0);
return x;
}
}";
            dynamic classObj = Program.ConvertAndRunCode(engine, code);
            object rslt = classObj.SomeMethod();
            Assert.Equal(1, rslt);
        }
        [Fact]
        public void StatementBodiedLambdaWorksInSwitchStmt() {
            var code = @"
            object rslt;
            switch (true) {
                case true:
                    rslt = ((int a) => { return a; })(1);
                    break;
            }
            return rslt;";
            var rslt = Program.ConvertAndRunStatements(engine, code);
            Assert.Equal(1, rslt);
        }
        [Fact]
        public void DoubleStatementBodiedLambdasWork() {
            var code = @"
            object rslt;
            switch (true) {
                case true:
                    rslt = ((int a) => { return a; })(((int a) => { return a; })(1));
                    break;
            }
            return rslt;";
            var rslt = Program.ConvertAndRunStatements(engine, code);
            Assert.Equal(1, rslt);
        }

        [Fact]
        public void GenericTypeArgWorks() {
            var code = @"
            return new System.Tuple<int>(1);";
            var rslt = Program.ConvertAndRunStatements(engine, code, new[] { "System" });
            Assert.IsType<Tuple<int>>(rslt);
        }
        [Fact]
        public void GenericTypeArgsWork() {
            var code = @"
            return new System.Tuple<int, int>(1, 1);";
            var rslt = Program.ConvertAndRunStatements(engine, code, new[] { "System" });
            Assert.IsType<Tuple<int, int>>(rslt);
        }
        [Fact]
        public void NullableTypeWorks() {
            var code = @"
            return new int?(1);";
            var rslt = Program.ConvertAndRunStatements(engine, code, new[] { "System" });
            Assert.IsType<int>(rslt);
        }

    }
}
