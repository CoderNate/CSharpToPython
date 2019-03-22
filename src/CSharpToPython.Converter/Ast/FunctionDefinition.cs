/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;



#if FEATURE_CORE_DLR
using MSAst = System.Linq.Expressions;
#else

#endif

using Microsoft.Scripting;




namespace IronPython.Compiler.Ast {
    

    public class FunctionDefinition : ScopeStatement {
        protected Statement _body;
        private readonly string _name;
        private readonly Parameter[] _parameters;
        private IList<Expression> _decorators;
        private bool _generator;                        // The function is a generator
        private bool _isLambda;

        // true if this function can set sys.exc_info(). Only functions with an except block can set that.
        private bool _canSetSysExcInfo;
        private bool _containsTryFinally;               // true if the function contains try/finally, used for generator optimization

        private PythonVariable _variable;               // The variable corresponding to the function name or null for lambdas
        private int _headerIndex;

        private static int _lambdaId;

        public FunctionDefinition(string name, Parameter[] parameters)
            : this(name, parameters, (Statement)null) {            
        }

        
        public FunctionDefinition(string name, Parameter[] parameters, Statement body) {

            if (name == null) {
                _name = "<lambda$" + Interlocked.Increment(ref _lambdaId) + ">";
                _isLambda = true;
            } else {
                _name = name;
            }

            _parameters = parameters;
            _body = body;
        }

        public bool IsLambda {
            get {
                return _isLambda;
            }
        }

        public IList<Parameter> Parameters {
            get { return _parameters; }
        }

        internal override string[] ParameterNames {
            get {
                throw new NotImplementedException();
                //return ArrayUtils.ConvertAll(_parameters, val => val.Name);
            }
        }

        internal override int ArgCount {
            get {
                return _parameters.Length;
            }
        }

        public Statement Body {
            get { return _body; }
            set { _body = value; }
        }

        public int HeaderIndex {
            get { return _headerIndex; }
            set { _headerIndex = value; }
        }

        public override string Name {
            get { return _name; }
        }

        public IList<Expression> Decorators {
            get { return _decorators; }
            internal set { _decorators = value; }
        }

        internal override bool IsGeneratorMethod {
            get {
                return IsGenerator;
            }
        }

        public bool IsGenerator {
            get { return _generator; }
            set { _generator = value; }
        }

        // Called by parser to mark that this function can set sys.exc_info(). 
        // An alternative technique would be to just walk the body after the parse and look for a except block.
        internal bool CanSetSysExcInfo {
            set { _canSetSysExcInfo = value; }
        }

        internal bool ContainsTryFinally {
            get { return _containsTryFinally; }
            set { _containsTryFinally = value; }
        }

        internal PythonVariable PythonVariable {
            get { return _variable; }
            set { _variable = value; }
        }

        internal override bool ExposesLocalVariable(PythonVariable variable) {
            return NeedsLocalsDictionary; 
        }


        internal override bool TryBindOuter(ScopeStatement from, PythonReference reference, out PythonVariable variable) {
            // Functions expose their locals to direct access
            ContainsNestedFreeVariables = true;
            if (TryGetVariable(reference.Name, out variable)) {
                variable.AccessedInNestedScope = true;

                if (variable.Kind == VariableKind.Local || variable.Kind == VariableKind.Parameter) {
                    from.AddFreeVariable(variable, true);

                    for (ScopeStatement scope = from.Parent; scope != this; scope = scope.Parent) {
                        scope.AddFreeVariable(variable, false);
                    }

                    AddCellVariable(variable);
                } else {
                    from.AddReferencedGlobal(reference.Name);
                }
                return true;
            }
            return false;
        }


        

        internal override IList<string> GetVarNames() {
            List<string> res = new List<string>();

            foreach (Parameter p in _parameters) {
                res.Add(p.Name);
            }

            AppendVariables(res);

            return res;
        }
        

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                if (_parameters != null) {
                    foreach (Parameter p in _parameters) {
                        p.Walk(walker);
                    }
                }
                if (_decorators != null) {
                    foreach (Expression decorator in _decorators) {
                        decorator.Walk(walker);
                    }
                }
                if (_body != null) {
                    _body.Walk(walker);
                }
            }
            walker.PostWalk(this);
        }


        internal override bool CanThrow {
            get {
                return false;
            }
        }



    }
}
