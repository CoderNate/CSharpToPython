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
using System.Collections.Generic;






namespace IronPython.Compiler.Ast {
    

    public class DictionaryExpression : Expression {
        private readonly SliceExpression[] _items;

        public DictionaryExpression(params SliceExpression[] items) {
            _items = items;
        }

        public IList<SliceExpression> Items {
            get { return _items; }
        }




        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                if (_items != null) {
                    foreach (SliceExpression s in _items) {
                        s.Walk(walker);
                    }
                }
            }
            walker.PostWalk(this);
        }
    }
}
