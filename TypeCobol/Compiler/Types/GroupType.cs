﻿using System.IO;
using TypeCobol.Compiler.Scopes;
using TypeCobol.Compiler.Symbols;

using static TypeCobol.Compiler.Symbols.Symbol;

namespace TypeCobol.Compiler.Types
{
    /// <summary>
    /// Class that represents a group type
    /// </summary>
    public class GroupType : Type
    {
        /// <summary>
        /// The fields of this GroupType.
        /// </summary>
        public Domain<VariableSymbol> Fields { get; internal set; }

        /// <summary>
        /// Scope Owner constructor
        /// </summary>
        /// <param name="owner">Owner of the group scope if any</param>
        public GroupType(Symbol owner)
            : base(Tags.Group)
        {
            Fields = new Domain<VariableSymbol>(owner);
        }

        internal override void SetFlag(Flags flag, bool value, bool propagate = false)
        {
            base.SetFlag(flag, value, propagate);
            if (propagate)
            {
                foreach (var varSym in Fields)
                {
                    varSym.SetFlag(flag, value, true);
                }
            }
        }

        /// <summary>
        /// Using Cobol some records can have a leading type
        /// which can be a USAGE type or a PICTURE Type, for instance;
        /// 
        /// 10  checkToDo-value PIC X VALUE LOW-VALUE.
        ///        88  checkToDo VALUE 'T'.
        ///        88  checkToDo-false VALUE 'F'
        ///             X'00' thru 'S'
        ///             'U' thru X'FF'.
        ///
        /// the leading type is PIC X.
        /// </summary>
        public Type LeadingType
        {
            get;
            set;
        }

        /// <summary>
        /// A Record may always expand to another records because it is related to a new Symbol owner.
        /// </summary>
        public override bool MayExpand => true;

        public override void Dump(TextWriter output, int indentLevel)
        {
            base.Dump(output, indentLevel);
            string indent = new string(' ', 2 * indentLevel);
            var level = indentLevel + 1;
            if (LeadingType != null)
            {
                output.Write(indent);
                output.WriteLine("LeadingType:");
                LeadingType.Dump(output, level);
            }

            if (Fields != null && Fields.Count > 0)
            {
                output.Write(indent);
                output.WriteLine("Fields:");
                foreach (var field in Fields)
                {
                    field.Dump(output, level);
                }
            }
        }

        public override TResult Accept<TResult, TParameter>(IVisitor<TResult, TParameter> v, TParameter arg)
        {
            return v.VisitGroupType(this, arg);
        }
    }
}