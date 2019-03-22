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

using System;
using System.Diagnostics;
using System.Dynamic;





namespace IronPython.Compiler.Ast {
    

    public class NameExpression : Expression {
        private readonly string _name;
        private PythonReference _reference;
        private bool _assigned;                  // definitely assigned

        public NameExpression(string name) {
            _name = name;
        }

        public string Name {
            get { return _name; }
        }

        internal PythonReference Reference {
            get { return _reference; }
            set { _reference = value; }
        }

        internal bool Assigned {
            get { return _assigned; }
            set { _assigned = value; }
        }

        public override string ToString() {
            return base.ToString() + ":" + _name;
        }


        internal override string CheckAssign() {
            return null;
        }

        internal override string CheckDelete() {
            return null;
        }


        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
            }
            walker.PostWalk(this);
        }

        internal override bool CanThrow {
            get {
                return !Assigned;
            }
        }
    }
}
