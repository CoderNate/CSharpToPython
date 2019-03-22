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

#if FEATURE_CORE_DLR
using MSAst = System.Linq.Expressions;
#else

#endif


namespace IronPython.Compiler.Ast {
    public abstract class Statement : Node {
        public virtual string Documentation {
            get {
                return null;
            }
        }

        public virtual Type Type {
            get {
                return typeof(void);
            }
        }
    }


}
