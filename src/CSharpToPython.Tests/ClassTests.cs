using System;
using Xunit;

namespace CSharpToPython.Tests {
    public class ClassTests {

        private readonly EngineWrapper engine = new EngineWrapper();

        [Fact]
        public void SimpleClassWorks() {
            var code = @"
public class SomeClass {
    int Main() { return 1; }
}";
            var rslt = Program.ConvertAndRunCode(engine, code);
            Assert.NotNull(rslt);
        }

        [Fact]
        public void SimpleStructWorks() {
            var code = @"
public struct SomeStruct {
    int Main() { return 1; }
}";
            var rslt = Program.ConvertAndRunCode(engine, code);
            Assert.NotNull(rslt);
        }

        [Fact]
        public void ClassBaseTypesWork() {
            var code = @"
public class SomeBaseClass { public int GetInt() => 1; }
public class SomeClass : SomeBaseClass {
}";
            var rslt = Program.ConvertAndRunCode(engine, code);
            Assert.Equal(1, (object)((dynamic)rslt).GetInt());
        }

        [Fact]
        public void ClassConstructorCanCallBaseClassConstructor() {
            var code = @"
public class BaseClass : object {
   public BaseClass(int x) { this.TheInt = x; }
   int TheInt ;
   int GetInt() => this.TheInt;
}
public class SomeClass : BaseClass {
    public SomeClass() : base(1) { }
}";
            var rslt = Program.ConvertAndRunCode(engine, code);
            Assert.Equal(1, (object)((dynamic)rslt).GetInt());
        }
        [Fact]
        public void ClassConstructorCallsBaseClassConstructor() {
            var code = @"
public class BaseClass : object {
   public BaseClass() { this.TheInt = 1; }
   int TheInt ;
   int GetInt() => this.TheInt;
}
public class SomeClass : BaseClass {
    public SomeClass() { }
}";
            var rslt = Program.ConvertAndRunCode(engine, code);
            Assert.Equal(1, (object)((dynamic)rslt).GetInt());
        }

        [Fact]
        public void InstanceMethodWorks() {
            var code = @"
public class SomeClass {
    public int GetInt() { return 1; }
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            int theInt = rslt.GetInt();
            Assert.Equal(1, theInt);
        }

        [Fact]
        public void OverrideMethodCanCallBase() {
            var code = @"
public class SomeBaseClass {
    public virtual int GetInt() => 1;
}
public class SomeClass : SomeBaseClass {
    public override int GetInt() => base.GetInt();
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            int theInt = rslt.GetInt();
            Assert.Equal(1, theInt);
        }

        [Fact]
        public void StaticMethodWorks() {
            var code = @"
public class SomeClass {
    public static int StaticGetInt() { return 1; }
    public int GetInt() { return SomeClass.StaticGetInt(); }
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            int theInt = rslt.GetInt();
            Assert.Equal(1, theInt);
        }

        /// <summary>
        /// Similar to <see cref="FunctionTests.FunctionWithParameterWorks"/> except that test passes no arguments
        /// </summary>
        [Fact]
        public void ParamsMethodParameterWorks() {
            var code = @"
public class SomeClass {
    public int[] GetInts(params int[] intVals) { return intVals; }
    public int GetIntArray() { return this.GetInts(1, 2); }
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            System.Collections.Generic.IEnumerable<object> theInt = rslt.GetIntArray();
            Assert.Equal(new object[] { 1, 2 }, theInt);
        }

        [Fact]
        public void FieldWithInitializerWorks() {
            var code = @"
public class SomeClass {
    public int SomeInt = 1;
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            int someInt = rslt.SomeInt;
            Assert.Equal(1, someInt);
        }
        [Fact]
        public void FieldWithoutInitializerWorks() {
            var code = @"
public class SomeClass {
    public object SomeValue;
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            object someValue = rslt.SomeValue;
            Assert.Null(someValue);
        }
        [Fact]
        public void FieldWithMultipleVariablesWorks() {
            var code = @"
public class SomeClass {
    public object SomeValue, SomeValue2;
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            object someValue = rslt.SomeValue;
            Assert.Null(someValue);
        }
        [Fact]
        public void StaticFieldWithoutInitializerWorks() {
            var code = @"
public class SomeClass {
    public static object SomeValue;
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            object someValue = rslt.SomeValue;
            Assert.Null(someValue);
        }
        [Fact(Skip = "Not implemented yet. Need to figure out best way to get default value for a type.")]
        public void ValueTypeFieldWithoutInitializerWorks() {
            var code = @"
public class SomeClass {
    public int SomeValue;
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            object someValue = rslt.SomeValue;
            Assert.Equal(0, someValue);
        }

        [Fact]
        public void PropertyWorks() {
            var code = @"
public class SomeClass {
    int _someValue;
    public int SomeValue { get { return this._someValue; } set { this._someValue = value; }
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            rslt.SomeValue = 2;
            int someValue = rslt.SomeValue;
            Assert.Equal(2, someValue);
        }

        [Fact]
        public void ExpressionBodiedPropertyWorks() {
            var code = @"
public class SomeClass {
    public int SomeValue => 1;
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            int someValue = rslt.SomeValue;
            Assert.Equal(1, someValue);
        }

        [Fact]
        public void AutoPropertyWorks() {
            var code = @"
public class SomeClass {
    public int SomeValue { get; set; } = 1;
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            int someValue = rslt.SomeValue;
            Assert.Equal(1, someValue);
        }

        [Fact(Skip = "Not sure how to implement static properties")]
        public void StaticPropertyWorks() {
            var code = @"
public class SomeClass {
    public static int SomeValue { get { return 1; } }
    public int GetInt() { return SomeClass.SomeValue; }
}";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            int someValue = rslt.GetInt();
            Assert.Equal(1, someValue);
        }

        [Fact]
        public void NamespaceWorks() {
            var code = @"
namespace SomeNamespace {
    public class SomeClass { }
}";
            var rslt = Program.ConvertAndRunCode(engine, code);
            Assert.NotNull(rslt);
        }

        [Fact]
        public void UsingDirectiveWorks() {
            var code = @"
using System;
class SomeClass {
   public object GetObj() { return new Random() ; }
}
";
            dynamic someClass = Program.ConvertAndRunCode(engine, code);
            var obj = (object)someClass.GetObj();
            Assert.IsType<System.Random>(obj);
        }
        [Fact]
        public void UsingDirectiveWithAliasWorks() {
            var code = @"
using SOMEALIAS = System;
class SomeClass {
   public object GetObj() { return new SOMEALIAS.Random() ; }
}
";
            dynamic someClass = Program.ConvertAndRunCode(engine, code);
            var obj = (object)someClass.GetObj();
            Assert.IsType<System.Random>(obj);
        }
        [Fact]
        public void UsingStaticDirectiveDoesntWork() {
            var code = @"
using static SOMECLASS;
";
            var converted = Program.ConvertCode(code);
            Assert.Contains("#ERROR", converted);
        }

        [Fact]
        public void InterfaceIsIgnored() {
            var code = "interface ISomeInterface {}";
            Assert.Equal("", Program.ConvertCode(code));
        }
        [Fact]
        public void NestedInterfaceIsIgnored() {
            var code = "class Blah { interface ISomeInterface {} }";
            Assert.NotNull(Program.ConvertAndRunCode(engine, code));
        }

        [Fact]
        public void NestedClassesWork() {
            var code = @"
class SomeClass {
    public class SomeInnerClass {
        public int GetInt() => 1;
    }
}
class SomeOtherClass {
    public int GetSomeClassInt() => new SomeClass.SomeInnerClass().GetInt();
}
";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            int someValue = rslt.GetSomeClassInt();
            Assert.Equal(1, someValue);
        }

        [Fact]
        public void EnumsWork() {
            var code = @"
enum SomeEnum { x, y = 3 }
class SomeClass {
    public object GetEnumVal() => SomeEnum.x;
}
";
            dynamic rslt = Program.ConvertAndRunCode(engine, code);
            int someValue = rslt.GetEnumVal();
            Assert.Equal(0, someValue);
        }
    }
}
