﻿using System;
using System.Collections.Generic;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.Scopes;

namespace TypeCobol.Compiler.Symbols
{
    /// <summary>
    /// Represents any symbol that contain other symbols (i.e. ProgramSymbol or NamespaceSymbol)
    /// Don't confuse with Scope class
    /// </summary>
    public abstract class AbstractScope : Symbol, IScope
    {
        /// <summary>
        /// Named constructor
        /// </summary>
        protected AbstractScope(string name, Kinds kind)
            : base(name, kind)
        {
        }

        public virtual Scope<TypedefSymbol> Types
        {
            get { return null; }
            protected set { ; }
        }

        public virtual Scope<VariableSymbol> FileData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<VariableSymbol> GlobalStorageData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<VariableSymbol> WorkingStorageData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<VariableSymbol> LocalStorageData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<VariableSymbol> LinkageStorageData
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<SectionSymbol> Sections
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<ParagraphSymbol> Paragraphs
        {
            get { return null; }
            protected set { }
        }

        public virtual Scope<FunctionSymbol> Functions
        {
            get { return null; }
            protected set {; }
        }

        public virtual Scope<ProgramSymbol> Programs
        {
            get { return null; }
            protected set { }
        }

        /// <summary>
        /// Resolve a Symbol.
        /// For a single symbol's name, the algorithm tries to resolve types starting from the local scope to the Top scope.
        /// For a qualified symbol, the algorithm tries to filter symbols whose qualified names strictly match the path.
        /// </summary>
        /// <param name="path">The type's path'</param>
        /// <param name="topScope">The top scope of the research</param>
        /// <param name="lookupSymbol">Delegate to lookup symbol inside a RootSymbolTable</param>
        /// <returns>The Set of resolve symbols</returns>
        protected Container<TS>.Entry ResolveSymbol<TS>(string[] path, AbstractScope topScope, Func<string, Container<TS>.Entry> lookupSymbol)
            where TS : Symbol
        {
            if (path == null || path.Length == 0 || path[0] == null)
                return null;

            string name = path[0];
            Container<TS>.Entry results = new Container<TS>.Entry(name);
            var candidates = lookupSymbol(name);
            if (candidates.Count == 0)
                return results;

            if (path.Length == 1)
            {
                bool bLocal = topScope != this;
                Container<TS>.Entry localResults = bLocal ? new Container<TS>.Entry(name) : null;
                Container<TS>.Entry topResults = results;
                string[] topPath = new string[] { name, topScope.Name };
                string[] localPath = bLocal ? new string[] { name, this.Name, topScope.Name } : null;
                foreach (var candidate in candidates)
                {
                    if (bLocal && candidate.IsMatchingPath(localPath))
                    {
                        localResults.Add(candidate);
                    }
                    else if (candidate.IsMatchingPath(topPath))
                    {
                        topResults.Add(candidate);
                    }
                }
                if (bLocal && localResults.Count != 0)
                    return localResults;
                if (topResults.Count != 0)
                    return topResults;
                return candidates;
            }
            foreach (var candidate in candidates)
            {
                if (candidate.IsStrictlyMatchingPath(path))
                {
                    results.Add(candidate);
                }
            }
            return results.Count == 0 ? candidates : results;
        }

        /// <summary>
        /// Resolve all types accessible from this scope by its path.
        /// </summary>
        /// <param name="root">The Root Symbol Table</param>
        /// <param name="path">The type's path</param>
        /// <returns>The set of types that match</returns>
        public abstract Container<TypedefSymbol>.Entry ResolveType(RootSymbolTable root, string[] path);

        /// <summary>
        /// Resolve all scopes accessible from this scope by its path.
        /// A Scope can be a namespace, a program or a function.
        /// </summary>
        /// <param name="root">The Root Symbol Table</param>
        /// <param name="path">The function's path</param>
        /// <returns>The set of scopes that match</returns>
        public abstract Container<AbstractScope>.Entry ResolveScope(RootSymbolTable root, string[] path);

        /// <summary>
        /// Lookup a program.
        /// </summary>
        /// <param name="rootScope">The top root scope</param>
        /// <param name="progName">The program name to be looked up</param>
        /// <returns></returns>
        protected virtual Container<ProgramSymbol>.Entry LookupProgram(AbstractScope rootScope, string progName, out AbstractScope currentScope, out AbstractScope stopScope)
        {
            stopScope = rootScope;
            currentScope = this;
            Container<ProgramSymbol>.Entry entry = null;
            while (currentScope != null)
            {
                var programs = currentScope.Programs;
                if (programs != null)
                {
                    entry = programs.Lookup(progName);
                    if (entry != null)
                    {
                        System.Diagnostics.Debug.Assert(entry.Count == 1);
                        currentScope = entry.Symbol;
                        stopScope = currentScope;
                        break;
                    }
                }
                if (currentScope.Owner != null && currentScope != stopScope && currentScope.Owner.HasScope)
                {
                    currentScope = currentScope.Owner as AbstractScope;
                }
                else
                {
                    currentScope = null;
                }
            }
            return entry;
        }

        /// <summary>
        /// Resolve a TypeDefinition from this current Scope.
        /// </summary>
        /// <param name="rootScope">The top rootScope</param>
        /// <param name="path">Looking path à la COBOL85 --> in Reverse order</param>
        /// <returns>The TypedefSymbol if found, null otherwise.</returns>
        public virtual Container<TypedefSymbol>.Entry ReverseResolveType(AbstractScope rootScope, string[] path)
        {
            System.Diagnostics.Debug.Assert(rootScope != null);
            System.Diagnostics.Debug.Assert(path != null);
            System.Diagnostics.Debug.Assert(path.Length > 0);

            AbstractScope stopScope = rootScope;
            AbstractScope currentScope = this;
            for (int i = path.Length - 1; i >= 0 && currentScope != null; i--)
            {
                switch(i)
                {
                    case 0://We must look for a Type
                        {
                            AbstractScope startScope = currentScope;
                            while (currentScope != null)
                            {
                                var types = currentScope.Types;
                                Container<TypedefSymbol>.Entry entry = types?.Lookup(path[i]);
                                if (entry != null)
                                {
                                    return entry;
                                }
                                if (currentScope.Owner != null && currentScope != stopScope && currentScope.Owner.HasScope)
                                {
                                    currentScope = currentScope.Owner as AbstractScope;
                                }
                                else
                                {
                                    currentScope = null;
                                }
                            }
                        }
                        break;
                    case 1://We must look for a Program
                        {
                            Container<ProgramSymbol>.Entry entry = LookupProgram(stopScope, path[i], out currentScope, out stopScope);
                        }
                        break;
                    default://We are looking for a Namepace
                        //TODO
                        break;                        
                }
            }
            return null;
        }

        /// <summary>
        /// Resolve a Function symbol from this current Scope.
        /// </summary>
        /// <param name="rootScope">The top rootScope</param>
        /// <param name="path">Looking path à la COBOL85 --> in Reverse order</param>
        /// <returns>The FunctionSymbol instance if found, null otherwise.</returns>
        public virtual Container<FunctionSymbol>.Entry ReverseResolveFunction(AbstractScope rootScope, string[] path)
        {
            System.Diagnostics.Debug.Assert(rootScope != null);
            System.Diagnostics.Debug.Assert(path != null);
            System.Diagnostics.Debug.Assert(path.Length > 0);

            AbstractScope stopScope = rootScope;
            AbstractScope currentScope = this;
            for (int i = path.Length - 1; i >= 0 && currentScope != null; i--)
            {
                switch (i)
                {
                    case 0://We must look for a Function
                        {
                            AbstractScope startScope = currentScope;
                            while (currentScope != null)
                            {
                                var functions = currentScope.Functions;
                                Container<FunctionSymbol>.Entry entry = functions?.Lookup(path[i]);
                                if (entry != null)
                                {
                                    return entry;
                                }
                                if (currentScope.Owner != null && currentScope != stopScope && currentScope.Owner.HasScope)
                                {
                                    currentScope = currentScope.Owner as AbstractScope;
                                }
                                else
                                {
                                    currentScope = null;
                                }
                            }
                        }
                        break;
                    case 1://We must look for a Program
                        {
                            Container<ProgramSymbol>.Entry entry = LookupProgram(stopScope, path[i], out currentScope, out stopScope);
                        }
                        break;
                    default://We are looking for a Namepace
                        //TODO
                        break;
                }
            }
            return null;
        }

        /// <summary>
        /// Compute the path represented by a Symbol Reference
        /// </summary>
        /// <param name="symRef">The Symbol Reference instance</param>
        /// <returns>The corresponding Path in the COBOL IN|OF ORDER. The paths are return ed in lower cases</returns>
        public static string[] SymbolReferenceToPath(SymbolReference datSymRef)
        {
            string[] paths = null;
            IList<SymbolReference> refs = null;

            if (datSymRef.IsQualifiedReference)
            {//Path in reverse order DVZF0OS3::EventList --> {EventList, DVZF0OS3}
                QualifiedSymbolReference qualifiedSymbolReference = datSymRef as QualifiedSymbolReference;
                refs = qualifiedSymbolReference.AsList();
            }
            else
            {
                refs = new List<SymbolReference>() { datSymRef };
            }

            paths = new string[refs.Count];
            for (int i = 0; i < refs.Count; i++)
            {
                paths[i] = refs[i].Name;
            }

            return paths;
        }

        /// <summary>
        /// Free all symbols or types associated to this scope?
        /// </summary>
        internal virtual void Clear()
        {
        }

        public virtual void Add(AbstractScope absScope)
        { }
        public virtual void Remove(AbstractScope absScope)
        { }
        public virtual void Add(TypedefSymbol type)
        { }
        public virtual void Remove(TypedefSymbol type)
        { }
    }
}
