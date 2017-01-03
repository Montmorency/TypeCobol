﻿using System;
using System.Collections.Generic;
using System.Text;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.CodeElements.Expressions;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Scanner;
using TypeCobol.Compiler.Text;
using TypeCobol.Tools;

namespace TypeCobol.Compiler.Nodes {
    /// <summary>
    ///     Tree node, including:
    ///     - strongly-typed CodeElement data
    ///     - parent/children relations
    ///     - unique identification accross the tree
    /// </summary>
    public abstract class Node : IVisitable{
        protected List<Node> children = new List<Node>();

        /// <summary>TODO: Codegen should do its stuff without polluting this class.</summary>
        public bool? Comment = null;

        protected Node(CodeElement CodeElement) {
            this.CodeElement = CodeElement;
        }

        /// <summary>CodeElement data (weakly-typed)</summary>
        public virtual CodeElement CodeElement { get; private set; }

        /// <summary>Parent node (weakly-typed)</summary>
        public Node Parent { get; private set; }

        /// <summary>List of children  (weakly-typed, read-only).</summary>
        /// If you want to modify this list, use the
        /// <see cref="Add" />
        /// and
        /// <see cref="Remove" />
        /// methods.
        public IReadOnlyList<Node> Children {
            get { return children.AsReadOnly(); }
        }

        /// <summary>
        /// This is usefull durring Code generation phase in order to create un Array of Node.
        /// Each Node will have it Node Index in the Array.
        /// </summary>
        public int NodeIndex { get; set; }

        public virtual string Name {
            get { return ID; }
        }

        public virtual QualifiedName QualifiedName {
            get { return URI != null ? new URI(URI) : null; }
        }

        /// <summary>Non-unique identifier of this node. Depends on CodeElement type and name (if applicable).</summary>
        public virtual string ID {
            get { return null; }
        }

        /// <summary>Node unique identifier (scope: tree this Node belongs to)</summary>
        public string URI {
            get {
                if (ID == null) return null;
                var puri = Parent == null ? null : Parent.URI;
                if (puri == null) return ID;
                return puri + '.' + ID;
            }
        }


        /// <summary>First Node with null Parent among the parents of this Node.</summary>
        public Node Root {
            get {
                var current = this;
                while (current.Parent != null) current = current.Parent;
                return current;
            }
        }

        /// <summary>
        ///     How far removed from Root is this Node?
        ///     Values are 0 if Root is this, 1 of Root is this.Parent,
        ///     2 if Root is this.Parent.Parent, and so on.
        /// </summary>
        public int Generation {
            get {
                var generation = 0;
                var parent = Parent;
                while (parent != null) {
                    generation++;
                    parent = parent.Parent;
                }
                return generation;
            }
        }

        //TODO move this method to DataDefinition
        /// <summary>If this node a subordinate of a TYPEDEF entry?</summary>
        public bool IsPartOfATypeDef {
            get {
                var parent = Parent;
                while (parent != null) {
                    if (!(parent is DataDefinition)) return false;
                    if (parent is TypeDefinition) return true;
                    parent = parent.Parent;
                }
                return false;
            }
        }


        public SymbolTable SymbolTable { get; set; }

        public object this[string attribute] {
            get { return Attributes.Get(this, attribute); }
        }

        /// <summary>
        /// Get the From and To Positions of this Node based on the consumed Token, if no ConsumedTokens the return value is NULL.
        /// In the consumed tokens span over several lines then the size of the newline sequence is included for each line.
        /// The method also calculate the ending span offset from the beginning of the last line.
        /// It also get the list of Line numbers occupated by this node.
        /// </summary>
        public Tuple<int, int, int, List<int>> FromToPositions
        {
            get
            {
                if (CodeElement == null || CodeElement.ConsumedTokens == null)
                    return null;
                if (CodeElement.ConsumedTokens.Count > 0)
                {
                    int ln_size = System.Environment.NewLine.Length;
                    int from = CodeElement.ConsumedTokens[0].Column;
                    int to = 0;
                    int i = 0;
                    int delta_ln = 0;
                    int span = 0;
                    List<int> lineNumbers = new List<int>();
                    do
                    {
                        var token = CodeElement.ConsumedTokens[i];
                        if (!(token is TypeCobol.Compiler.Preprocessor.ImportedToken))
                        {//Don't take in account imported tokens -> This avoid including lines that come from COPYs files.
                            int curLineIndex = CodeElement.ConsumedTokens[i].Line;
                            lineNumbers.Add(curLineIndex);
                            to += delta_ln;
                            span = 0;
                            while (i < CodeElement.ConsumedTokens.Count && curLineIndex == CodeElement.ConsumedTokens[i].Line)
                            {
                                span = CodeElement.ConsumedTokens[i].EndColumn;
                                i++;
                            }
                            to += span;
                            delta_ln = ln_size;
                        }
                        else
                        {
                            i++;
                        }
                    } while (i < CodeElement.ConsumedTokens.Count);
                    lineNumbers.TrimExcess();
                    return new Tuple<int, int, int, List<int>>(from, to, span, lineNumbers);
                }
                return null;
            }
        }

        public virtual IEnumerable<ITextLine> Lines {
            get {
                var lines = new List<ITextLine>();
                if (CodeElement == null || CodeElement.ConsumedTokens == null) return lines;
                foreach (var token in CodeElement.ConsumedTokens) {//JCM: Don't take in account imported token.
                    if (!(token is TypeCobol.Compiler.Preprocessor.ImportedToken)) {
                        if (!lines.Contains(token.TokensLine))
                            lines.Add(token.TokensLine);
                    }
                }
                return lines;
            }
        }

        /// <summary>
        /// Marker for Code Generation to know if this Node will generate code.
        /// TODO this method should be in CodeGen project
        /// </summary>
        public bool NeedGeneration { get; set; }

        public IList<CodeElementHolder<T>> GetChildren<T>() where T : CodeElement {
            var results = new List<CodeElementHolder<T>>();
            foreach (var child in children) {
                if (child.CodeElement == null) continue;
                if (Reflection.IsTypeOf(child.CodeElement.GetType(), typeof(T)))
                    results.Add((CodeElementHolder<T>) child);
            }
            return results;
        }

        /// <summary>Search for all children of a specific Name</summary>
        /// <param name="name">Name we search for</param>
        /// <param name="deep">true for deep search, false for shallow search</param>
        /// <returns>List of all children with the proper name ; empty list if there is none</returns>
        public IList<Node> GetChildren(string name, bool deep) {
            var results = new List<Node>();
            foreach (var child in children) {
                if (name.Equals(child.Name, StringComparison.InvariantCultureIgnoreCase)) results.Add(child);
                if (deep) results.AddRange(child.GetChildren(name, true));
            }
            return results;
        }


        /// <summary>Adds a node as a children of this one.</summary>
        /// <param name="child">Child to-be.</param>
        /// <param name="index">Child position in children list.</param>
        public virtual void Add(Node child, int index = -1) {
            if (index < 0) children.Add(child);
            else children.Insert(index, child);
            child.Parent = this;
        }

        /// <summary>Removes a child from this node.</summary>
        /// <param name="node">Child to remove. If this is not one of this Node's current children, nothing happens.</param>
        public void Remove(Node child) {
            children.Remove(child);
            child.Parent = null;
        }

        /// <summary>Removes this node from its Parent's children list and set this.Parent to null.</summary>
        public void Remove() {
            if (Parent != null) Parent.Remove(this);
        }

        /// <summary>Position of a specific child among its siblings.</summary>
        /// <param name="child">Child to be searched for.</param>
        /// <returns>Position in the children list.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">As List</exception>
        public int IndexOf(Node child) {
            return children.IndexOf(child);
        }

        /// <summary>Delete all childrens of this node.</summary>
        public void Clear() {
            foreach (var child in children) child.Parent = null;
            children.Clear();
        }

        /// <summary>
        /// Get All Childrens.
        /// </summary>
        /// <param name="lines">A List to store all children.</param>
        public void ListChildren(List<Node> list)
        {
            if (list == null) return;
            foreach (var child in children)
            {
                list.Add(child);
                child.ListChildren(list);
            }
        }

        /// <summary>Get this node or one of its children that has a given URI.</summary>
        /// <param name="uri">Node unique identifier to search for</param>
        /// <returns>Node n for which n.URI == uri, or null if no such Node was found</returns>
        public Node Get(string uri) {
            if (URI != null && URI.EndsWith(uri)) return this;
            foreach (var child in Children) {
                var found = child.Get(uri);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>As <see cref="Get" /> method, but can specify the type of Node to retrieve.</summary>
        /// <typeparam name="N"></typeparam>
        /// <param name="uri"></param>
        /// <returns>null if a node with the given URI is found but is not of the proper type</returns>
        public N Get<N>(string uri) where N : Node {
            var node = Get(uri);
            try {
                return (N) node;
            } catch (InvalidCastException) {
                return default(N);
            }
        }


        public override string ToString() {
            var str = new StringBuilder();
            Dump(str, 0);
            return str.ToString();
        }

        public bool AcceptASTVisitor(IASTVisitor astVisitor) {
            //TODO
            //astVisitor.Visit(this);
            if (CodeElement != null) {
                if (astVisitor.BeginCodeElement(CodeElement)) {
                    CodeElement.AcceptASTVisitor(astVisitor);
                }
                astVisitor.EndCodeElement(CodeElement);
            }
            foreach (Node child in Children) {
                if (astVisitor.BeginNode(child)) {
                    child.AcceptASTVisitor(astVisitor);
                }
                astVisitor.EndNode(child);
            }
            //no sense here
            return true;
        }


        private void Dump(StringBuilder str, int i)
        {
            for (var c = 0; c < i; c++) str.Append("  ");
            if (Comment == true) str.Append('*');
            if (Name != null) str.AppendLine(Name);
            else if (CodeElement == null) str.AppendLine("?");
            else str.AppendLine(CodeElement.ToString());
            foreach (var child in Children) child.Dump(str, i + 1);
        }

        /// <summary>TODO: Codegen should do its stuff without polluting this class.</summary>
        public void RemoveAllChildren() {
            children.Clear();
        }

        /// <summary>TODO#364 hack ; this method exists because preprocessor directive tokens are not seeable in AST</summary>
        private int TryToExtendLineFrom(int index, Token token, out bool endsLine) {
            var tokens = token.TokensLine.SourceTokens;
            var c = tokens.IndexOf(token);
            if (c == -1) {
                endsLine = true;
                return index;
            } // something was probably REPLACEd
            var takeall = false;
            c++;
            for (; c < tokens.Count; c++) {
                var t = tokens[c];
                if (!takeall)
                    if (t.TokenFamily == TokenFamily.Whitespace) ; // take this token
                    else if (t.TokenFamily == TokenFamily.CompilerDirectiveStartingKeyword
                             || t.TokenFamily == TokenFamily.CompilerDirective
                             || t.TokenFamily == TokenFamily.Comments) takeall = true; // take all tokens until end of line
                    else break; // don't take any more tokens
                token = t;
                index = t.StopIndex;
            }
            endsLine = token == tokens[tokens.Count - 1];
            return index;
        }

        /// <summary>Implementation of the GoF Visitor pattern.</summary>
        public void Accept(NodeVisitor visitor) {
            visitor.Visit(this);
        }

        /// <summary>
        ///     Return true if this Node is inside a COPY
        ///     TODO To discuss: make a rule: a Node CAN'T have token in two different source file
        /// </summary>
        /// <returns></returns>
        public bool IsInsideCopy() {
            return CodeElement != null && CodeElement.IsInsideCopy();
        }
    }

// --- Temporary base classes for data definition noes ---

    public interface ITypedNode {
        DataType DataType { get; }
        int Length { get; }
    }

    /// <summary>Implementation of the GoF Visitor pattern.</summary>
    public interface NodeVisitor {
        void Visit(Node node);
    }


    public interface CodeElementHolder<T> where T : CodeElement {}

    public static class CodeElementHolderExtension {
        /// <summary>CodeElement data (strongly-typed)</summary>
        /// <typeparam name="T">Class (derived from <see cref="CodeElement" />) of the data.</typeparam>
        /// <param name="holder">We want this <see cref="Node" />'s data.</param>
        /// <returns>This <see cref="Node" />'s CodeElement data, but strongly-typed.</returns>
        public static T CodeElement<T>(this CodeElementHolder<T> holder) where T : CodeElement {
            var node = holder as Node;
            if (node == null) throw new ArgumentException("CodeElementHolder must be a Node.");
            return (T) node.CodeElement;
        }
    }

    /// <summary>A <see cref="Node" /> who can type its parent more strongly should inherit from this.</summary>
    /// <typeparam name="C">Class (derived from <see cref="Node{T}" />) of the parent node.</typeparam>
    public interface Child<P> where P : Node {}

    /// <summary>Extension method to get a more strongly-typed parent than just Node.</summary>
    public static class ChildExtension {
        /// <summary>Returns this node's parent in as strongly-typed.</summary>
        /// <typeparam name="P">Class (derived from <see cref="Node{T}" />) of the parent.</typeparam>
        /// <param name="child">We want this <see cref="Node" />'s parent.</param>
        /// <returns>This <see cref="Node" />'s parent, but strongly-typed.</returns>
        public static P Parent<P>(this Child<P> child) where P : Node {
            var node = child as Node;
            if (node == null) throw new ArgumentException("Child must be a Node.");
            return (P) node.Parent;
        }
    }

    /// <summary>A <see cref="Node" /> who can type its children more strongly should inherit from this.</summary>
    /// <typeparam name="C">Class (derived from <see cref="Node{T}" />) of the children nodes.</typeparam>
    public interface Parent<C> where C : Node {}

    /// <summary>Extension method to get children more strongly-typed than just Node.</summary>
    public static class ParentExtension {
        /// <summary>
        ///     Returns a read-only list of strongly-typed children of a <see cref="Node" />.
        ///     The children are more strongly-typed than the ones in the Node.Children property.
        ///     The list is read-only because the returned list is a copy of the Node.Children list property ;
        ///     thus, writing node.StrongChildren().Add(child) will be a compilation error.
        ///     Strongly-typed children are to be iterated on, but to modify a Node's children list you have
        ///     to use the (weakly-typed) Node.Children property.
        /// </summary>
        /// <typeparam name="C">Class (derived from <see cref="Node{T}" />) of the children.</typeparam>
        /// <param name="parent">We want this <see cref="Node" />'s children.</param>
        /// <returns>Strongly-typed list of a <see cref="Node" />'s children.</returns>
        public static IReadOnlyList<C> Children<C>(this Parent<C> parent) where C : Node {
            var node = parent as Node;
            if (node == null) throw new ArgumentException("Parent must be a Node.");
            //TODO? maybe use ConvertAll or Cast from LINQ, but only
            // if the performance is better or if it avoids a copy.
            var result = new List<C>();
            foreach (var child in node.Children) result.Add((C) child);
            return result.AsReadOnly();
        }
    }


    /// <summary>Root of any Node tree, with null CodeElement.</summary>
    public class Root : Node, CodeElementHolder<CodeElement> {
        public Root() : base(null) {}
    }

    public class Program : Node, CodeElementHolder<ProgramIdentification> {
        public Program(ProgramIdentification identification) : base(identification) {}

        public override string ID {
            get {
                if (Parent is Root) return "program";
                return this.CodeElement().ProgramName.Name;
            }
        }
    }

    public class LibraryCopy : Node, CodeElementHolder<LibraryCopyCodeElement>, Child<Program> {
        public LibraryCopy(LibraryCopyCodeElement ce) : base(ce) {}

        public override string ID {
            get { return "copy"; }
        }
    }

    public class Class : Node, CodeElementHolder<ClassIdentification> {
        public Class(ClassIdentification identification) : base(identification) {}

        public override string ID {
            get { return this.CodeElement().ClassName.Name; }
        }
    }

    public class Factory : Node, CodeElementHolder<FactoryIdentification> {
        public Factory(FactoryIdentification identification) : base(identification) {}

        public override string ID {
            get { return "TODO#248"; }
        }
    }

    public class Method : Node, CodeElementHolder<MethodIdentification> {
        public Method(MethodIdentification identification) : base(identification) {}

        public override string ID {
            get { return this.CodeElement().MethodName.Name; }
        }
    }

    public class Object : Node, CodeElementHolder<ObjectIdentification> {
        public Object(ObjectIdentification identification) : base(identification) {}

        public override string ID {
            get { return "TODO#248"; }
        }
    }

    public class End : Node, CodeElementHolder<CodeElementEnd> {
        public End(CodeElementEnd end) : base(end) {}

        public override string ID {
            get { return "end"; }
        }
    }
} // end of namespace TypeCobol.Compiler.Nodes