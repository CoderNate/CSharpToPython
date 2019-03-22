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
using System.Linq.Expressions;
#else

#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Scripting;

namespace IronPython.Compiler.Ast {
    
    


    public abstract class Node  {
        private ScopeStatement _parent;


        protected Node() {
        }

        #region Public API

        public ScopeStatement Parent {
            get { return _parent; }
            set { _parent = value; }
        }
        

        

        public bool Synthetic;

        public SourceSpan Span {
            get {
                throw new NotImplementedException();
                //if (Synthetic) return SourceSpan.None;
                //if (End.Index == 0 && !this.GetType().Name.Contains("Import") && !this.GetType().Name.Contains("ModuleName"))
                //{
                //    var a = 1;
                //}
                //return new SourceSpan(Start, End);
            }
        }

        public abstract void Walk(PythonWalker walker);

        public virtual string NodeName {
            get {
                return GetType().Name;
            }
        }

        #endregion

        #region Base Class Overrides

        /// <summary>
        /// Returns true if the node can throw, false otherwise.  Used to determine
        /// whether or not we need to update the current dynamic stack info.
        /// </summary>
        internal virtual bool CanThrow {
            get {
                return true;
            }
        }


        public override string ToString() {
            return GetType().Name;
        }

        #endregion


        #region Transformation Helpers

        internal static bool CanAssign(Type/*!*/ to, Type/*!*/ from) {
            return to.IsAssignableFrom(from) && (to.GetTypeInfo().IsValueType == from.GetTypeInfo().IsValueType);
        }

        #endregion

    }
}
