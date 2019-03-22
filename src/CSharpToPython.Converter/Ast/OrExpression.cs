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

namespace IronPython.Compiler.Ast {
    
    

    public class OrExpression : Expression {
        private readonly Expression _left, _right;

        public OrExpression(Expression left, Expression right) {

            _left = left;
            _right = right;
            //StartIndex = left.StartIndex;
            //EndIndex = right.EndIndex;
        }

        public Expression Left {
            get { return _left; }
        }
        public Expression Right {
            get { return _right; }
        }


        public override Type Type {
            get {
                Type leftType = _left.Type;
                return leftType == _right.Type ? leftType : typeof(object);
            }
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                if (_left != null) {
                    _left.Walk(walker);
                }
                if (_right != null) {
                    _right.Walk(walker);
                }
            }
            walker.PostWalk(this);
        }

        internal override bool CanThrow {
            get {
                return _left.CanThrow || _right.CanThrow;
            }
        }
    }
}
