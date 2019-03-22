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
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;



#if FEATURE_CORE_DLR
using MSAst = System.Linq.Expressions;
#else

#endif
using Microsoft.Scripting;

namespace IronPython.Compiler.Ast {
    
    

    public abstract class ScopeStatement : Statement {
        private bool _importStar;                   // from module import *
        private bool _unqualifiedExec;              // exec "code"
        private bool _nestedFreeVariables;          // nested function with free variable
        private bool _locals;                       // The scope needs locals dictionary
                                                    // due to "exec" or call to dir, locals, eval, vars...
        private bool _hasLateboundVarSets;          // calls code which can assign to variables
        private bool _containsExceptionHandling;    // true if this block contains a try/with statement


        private Dictionary<string, PythonVariable> _variables;          // mapping of string to variables
        private List<PythonVariable> _freeVars;                         // list of variables accessed from outer scopes
        private List<string> _globalVars;                               // global variables accessed from this scope
        private List<string> _cellVars;                                 // variables accessed from nested scopes
        private Dictionary<string, PythonReference> _references;        // names of all variables referenced, null after binding completes


        internal const string NameForExec = "module: <exec>";
        
        internal bool ContainsImportStar {
            get { return _importStar; }
            set { _importStar = value; }
        }

        internal bool ContainsExceptionHandling {
            get {
                return _containsExceptionHandling;
            }
            set {
                _containsExceptionHandling = value;
            }
        }

        internal bool ContainsUnqualifiedExec {
            get { return _unqualifiedExec; }
            set { _unqualifiedExec = value; }
        }

        internal virtual bool IsGeneratorMethod {
            get {
                return false;
            }
        }


        /// <summary>
        /// True if this scope accesses a variable from an outer scope.
        /// </summary>
        internal bool IsClosure {
            get { return FreeVariables != null && FreeVariables.Count > 0; }
        }

        /// <summary>
        /// True if an inner scope is accessing a variable defined in this scope.
        /// </summary>
        internal bool ContainsNestedFreeVariables {
            get { return _nestedFreeVariables; }
            set { _nestedFreeVariables = value; }
        }

        /// <summary>
        /// True if we are forcing the creation of a dictionary for storing locals.
        /// 
        /// This occurs for calls to locals(), dir(), vars(), unqualified exec, and
        /// from ... import *.
        /// </summary>
        internal bool NeedsLocalsDictionary {
            get { return _locals; }
            set { _locals = value; }
        }

        public virtual string Name {
            get {
                return "<unknown>";
            }
        }

        /// <summary>
        /// True if variables can be set in a late bound fashion that we don't
        /// know about at code gen time - for example via from foo import *.
        /// 
        /// This is tracked independently of the ContainsUnqualifiedExec/NeedsLocalsDictionary
        /// </summary>
        internal virtual bool HasLateBoundVariableSets {
            get {
                return _hasLateboundVarSets;
            }
            set {
                _hasLateboundVarSets = value;
            }
        }

        internal Dictionary<string, PythonVariable> Variables {
            get { return _variables; }
        }

        internal virtual bool IsGlobal {
            get { return false; }
        }

        internal bool NeedsLocalContext {
            get {
                return NeedsLocalsDictionary || ContainsNestedFreeVariables;
            }
        }

        internal virtual string[] ParameterNames {
            get {
                throw new NotImplementedException();
                //return ArrayUtils.EmptyStrings;
            }
        }

        internal virtual int ArgCount {
            get {
                return 0;
            }
        }


        internal virtual string ScopeDocumentation {
            get {
                return null;
            }
        }

        internal virtual Delegate OriginalDelegate {
            get {
                return null;
            }
        }

        internal virtual IList<string> GetVarNames() {
            List<string> res = new List<string>();

            AppendVariables(res);

            return res;
        }


        internal void AddFreeVariable(PythonVariable variable, bool accessedInScope) {
            if (_freeVars == null) {
                _freeVars = new List<PythonVariable>();
            }

            if(!_freeVars.Contains(variable)) {
                _freeVars.Add(variable);
            }
        }


        internal string AddReferencedGlobal(string name) {
            if (_globalVars == null) {
                _globalVars = new List<string>();
            }
            if (!_globalVars.Contains(name)) {
                _globalVars.Add(name);
            }
            return name;
        }

        internal void AddCellVariable(PythonVariable variable) {
            if (_cellVars == null) {
                _cellVars = new List<string>();
            }

            if (!_cellVars.Contains(variable.Name)) {
                _cellVars.Add(variable.Name);
            }
        }

        internal List<string> AppendVariables(List<string> res) {
            if (Variables != null) {
                foreach (var variable in Variables) {
                    if (variable.Value.Kind != VariableKind.Local) {
                        continue;
                    }

                    if (CellVariables == null || !CellVariables.Contains(variable.Key)) {
                        res.Add(variable.Key);
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Variables that are bound in an outer scope - but not a global scope
        /// </summary>
        internal IList<PythonVariable> FreeVariables {
            get {
                return _freeVars;
            }
        }

        /// <summary>
        /// Variables that are bound to the global scope
        /// </summary>
        internal IList<string> GlobalVariables {
            get {
                return _globalVars;
            }
        }

        /// <summary>
        /// Variables that are referred to from a nested scope and need to be
        /// promoted to cells.
        /// </summary>
        internal IList<string> CellVariables {
            get {
                return _cellVars;
            }
        }


        internal abstract bool ExposesLocalVariable(PythonVariable variable);


        private bool TryGetAnyVariable(string name, out PythonVariable variable) {
            if (_variables != null) {
                return _variables.TryGetValue(name, out variable);
            } else {
                variable = null;
                return false;
            }
        }

        internal bool TryGetVariable(string name, out PythonVariable variable) {
            if (TryGetAnyVariable(name, out variable)) {
                return true;
            } else {
                variable = null;
                return false;
            }
        }

        internal virtual bool TryBindOuter(ScopeStatement from, PythonReference reference, out PythonVariable variable) {
            // Hide scope contents by default (only functions expose their locals)
            variable = null;
            return false;
        }

        private static bool HasClosureVariable(List<ClosureInfo> closureVariables, PythonVariable variable) {
            if (closureVariables == null) {
                return false;
            }

            for (int i = 0; i < closureVariables.Count; i++) {
                if (closureVariables[i].Variable == variable) {
                    return true;
                }
            }

            return false;
        }

        private void EnsureVariables() {
            if (_variables == null) {
                _variables = new Dictionary<string, PythonVariable>(StringComparer.OrdinalIgnoreCase);
            }
        }

        internal void AddGlobalVariable(PythonVariable variable) {
            EnsureVariables();
            _variables[variable.Name] = variable;
        }

        internal PythonReference Reference(string name) {
            if (_references == null) {
                _references = new Dictionary<string, PythonReference>(StringComparer.OrdinalIgnoreCase);
            }
            PythonReference reference;
            if (!_references.TryGetValue(name, out reference)) {
                _references[name] = reference = new PythonReference(name);
            }
            return reference;
        }

        internal bool IsReferenced(string name) {
            PythonReference reference;
            return _references != null && _references.TryGetValue(name, out reference);
        }

        internal PythonVariable/*!*/ CreateVariable(string name, VariableKind kind) {
            EnsureVariables();
            Debug.Assert(!_variables.ContainsKey(name));
            PythonVariable variable;
            _variables[name] = variable = new PythonVariable(name, kind, this);
            return variable;
        }

        internal PythonVariable/*!*/ EnsureVariable(string name) {
            PythonVariable variable;
            if (!TryGetVariable(name, out variable)) {
                return CreateVariable(name, VariableKind.Local);
            }
            return variable;
        }

        internal PythonVariable DefineParameter(string name) {
            return CreateVariable(name, VariableKind.Parameter);
        }


        #region Debug Info Tracking

        #endregion

        internal ScopeStatement CopyForRewrite() {
            return (ScopeStatement)MemberwiseClone();
        }


        struct ClosureInfo {
            public PythonVariable Variable;
            public bool AccessedInScope;

            public ClosureInfo(PythonVariable variable, bool accessedInScope) {
                Variable = variable;
                AccessedInScope = accessedInScope;
            }
        }

        internal virtual bool PrintExpressions {
            get {
                return false;
            }
        }

    }
}
