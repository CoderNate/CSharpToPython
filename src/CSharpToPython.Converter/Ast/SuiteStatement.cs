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

#if FEATURE_CORE_DLR
using MSAst = System.Linq.Expressions;
#else

#endif

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;





namespace IronPython.Compiler.Ast {
    
    
    public sealed class SuiteStatement : Statement {
        private readonly Statement[] _statements;

        public SuiteStatement(Statement[] statements) {
            _statements = statements;
        }

        public IList<Statement> Statements {
            get { return _statements; }
        } 



        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                if (_statements != null) {
                    foreach (Statement s in _statements) {
                        s.Walk(walker);
                    }
                }
            }
            walker.PostWalk(this);
        }

        public override string Documentation {
            get {                
                if (_statements.Length > 0) {
                    return _statements[0].Documentation;
                }
                return null;
            }
        }

        internal override bool CanThrow {
            get {
                foreach (Statement stmt in _statements) {
                    if (stmt.CanThrow) {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
