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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;






namespace IronPython.Compiler.Ast {
    

    public class ForStatement : Statement, ILoopStatement {
        private int _headerIndex;
        private readonly Expression _left;
        private Expression _list;
        private Statement _body;
        private readonly Statement _else;

        public ForStatement(Expression left, Expression list, Statement body, Statement else_) {
            _left = left;
            _list = list;
            _body = body;
            _else = else_;
        }

        public int HeaderIndex {
            set { _headerIndex = value; }
        }

        public Expression Left {
            get { return _left; }
        }

        public Statement Body {
            get { return _body; }
            set { _body = value; }
        }

        public Expression List {
            get { return _list; }
            set { _list = value; }
        }

        public Statement Else {
            get { return _else; }
        }



        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                if (_left != null) {
                    _left.Walk(walker);
                }
                if (_list != null) {
                    _list.Walk(walker);
                }
                if (_body != null) {
                    _body.Walk(walker);
                }
                if (_else != null) {
                    _else.Walk(walker);
                }
            }
            walker.PostWalk(this);
        }


        internal override bool CanThrow {
            get {
                if (_left.CanThrow) {
                    return true;
                }

                if (_list.CanThrow) {
                    return true;
                }

                // most constants (int, float, long, etc...) will throw here
                ConstantExpression ce = _list as ConstantExpression;
                if (ce != null) {
                    if (ce.Value is string) {
                        return false;
                    }
                    return true;
                }

                return false;
            }
        }
    }
}
