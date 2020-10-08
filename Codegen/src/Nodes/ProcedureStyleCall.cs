﻿

using System;
using System.Linq;
using TypeCobol.Codegen.Extensions.Compiler.CodeElements.Expressions;
using TypeCobol.Compiler.Nodes;

namespace TypeCobol.Codegen.Nodes {
    using System.Collections.Generic;
    using Tools;
    using TypeCobol.Compiler.CodeElements;
    using TypeCobol.Compiler.Text;

    /// <summary>
    ///  Class that represents the Node associated to a procedure call.
    /// </summary>
    internal class ProcedureStyleCall: Compiler.Nodes.ProcedureStyleCall, Generated {
	private Compiler.Nodes.ProcedureStyleCall Node;
	private CallStatement call;
    //The Original Staement
    private ProcedureStyleCallStatement Statement;
    //Does this call has a CALL-END ?
    private bool HasCallEnd;

    /// <summary>
    /// Arguments Mode.
    /// </summary>
    public enum ArgMode
    {
        Input,
        InOut,
        Output
    };

    /// <summary>
    /// If imported public function are call with EXTERNA POINTER or Not.
    /// </summary>
    public new static bool IsNotByExternalPointer
    {
        get;
        set;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">The AST Node of Procedure Call</param>
	public ProcedureStyleCall(Compiler.Nodes.ProcedureStyleCall node)
		: base(node.CodeElement) {
		this.Node = node;
		Statement = Node.CodeElement;       
		call = new CallStatement();
        call.ProgramOrProgramEntryOrProcedureOrFunction = new SymbolReferenceVariable(StorageDataType.ProgramName, Statement.ProcedureCall.ProcedureName);
        call.InputParameters = new List<CallSiteParameter>(Statement.ProcedureCall.Arguments);
		call.OutputParameter = null;
        //Add any optional CALL-END statement
        foreach (var child in node.Children)
        {
            this.Add(child);            
            if (child.CodeElement != null && child.CodeElement.Type == TypeCobol.Compiler.CodeElements.CodeElementType.CallStatementEnd)
            {
                HasCallEnd = true;
            }
        }
	}

    private List<ITextLine> _cache = null;

    /// <summary>
    /// TextLines to be generated by this node.
    /// Rule: TCCODEGEN-FUNCALL-PARAMS
    /// </summary>
	public override IEnumerable<ITextLine> Lines {
		get {
			if (_cache == null) {
				_cache = new List<ITextLine>();
				var hash = Node.FunctionDeclaration.Hash;

                //Rule: TCCODEGEN-FIXFOR-ALIGN-FUNCALL
                TypeCobol.Compiler.Nodes.FunctionDeclaration fun_decl = this.Node.FunctionDeclaration;
                string callString;

                //We don't need end-if anymore, but I let it for now. Because this generated code still need to be tested on production
                bool bNeedEndIf = false;
			    string func_lib_name = Hash.CalculateCobolProgramNameShortcut(fun_decl.Library);
                if ((fun_decl.CodeElement).Visibility == AccessModifier.Public && fun_decl.GetProgramNode() != this.GetProgramNode())
                {
                    if (this.Node.IsNotByExternalPointer || IsNotByExternalPointer)
                    {
                        int genIndent = 1;
                        IsNotByExternalPointer = true;
                        var ptrCheckGuardTextLine = new TextLineSnapshot(-1, string.Empty, null);
                        _cache.Add(ptrCheckGuardTextLine);

                        IsNotByExternalPointer = true;
                        string ptrCheckGuard = string.Format("{0}IF ADDRESS OF TC-{1}-{2}-Item = NULL", new string(' ', genIndent * 4), func_lib_name, hash);
                        ptrCheckGuardTextLine = new TextLineSnapshot(-1, ptrCheckGuard, null);
                        _cache.Add(ptrCheckGuardTextLine);

                        IsNotByExternalPointer = true;
                        ptrCheckGuard = string.Format("{0}  OR TC-{1}-{2}-Idt not = '{3}'", new string(' ', genIndent * 4), func_lib_name, hash, hash);
                        ptrCheckGuardTextLine = new TextLineSnapshot(-1, ptrCheckGuard, null);
                        _cache.Add(ptrCheckGuardTextLine);
                        genIndent++;

                        ptrCheckGuard = string.Format("{0}PERFORM TC-LOAD-POINTERS-{1}", new string(' ', genIndent * 4), func_lib_name);
                        ptrCheckGuardTextLine = new TextLineSnapshot(-1, ptrCheckGuard, null);
                        _cache.Add(ptrCheckGuardTextLine);
                        genIndent--;

                        ptrCheckGuard = string.Format("{0}END-IF", new string(' ', genIndent * 4));
                        ptrCheckGuardTextLine = new TextLineSnapshot(-1, ptrCheckGuard, null);
                        _cache.Add(ptrCheckGuardTextLine);

                        callString = string.Format("*{0}Equivalent to call {1} in module {2}", new string(' ', genIndent * 4), hash, fun_decl.Library);
                        var callTextLine = new TextLineSnapshot(-1, callString, null);
                        _cache.Add(callTextLine);

                        callString = string.Format("{0}CALL TC-{1}-{2}{3}", new string(' ', genIndent * 4), func_lib_name, hash, Node.FunctionCall.Arguments.Length == 0 ? "" : " USING");
                        callTextLine = new TextLineSnapshot(-1, callString, null);
                        _cache.Add(callTextLine);
                    }
                    else
                    {
                        var callTextLine = new TextLineSnapshot(-1, "", null);
                        _cache.Add(callTextLine);

                        callString = string.Format("*Equivalent to call {0} in module {1}", hash, fun_decl.Library);
                        callTextLine = new TextLineSnapshot(-1, callString, null);
                        _cache.Add(callTextLine);

                        callString = string.Format("CALL TC-{0}-{1}{2}", func_lib_name, hash, Node.FunctionCall.Arguments.Length == 0 ? "" : " USING");
                        callTextLine = new TextLineSnapshot(-1, callString, null);
                        _cache.Add(callTextLine);
                    }
                }
                else
                {
                    callString = string.Format("CALL '{0}'{1}", hash, Node.FunctionCall.Arguments.Length == 0 ? "" : " USING");
                    var callTextLine = new TextLineSnapshot(-1, callString, null);
                    _cache.Add(callTextLine);

                }

                //Find and set computed hash (if any) to each DataOrConditionStorageArea used by the CALL node
                var hashes = Node.Root.GeneratedCobolHashes;
                if (Node.QualifiedStorageAreas != null)
                {
                    foreach (var qualifiedStorageArea in Node.QualifiedStorageAreas)
                    {
                        if (qualifiedStorageArea.Value != null
                            &&
                            hashes.TryGetValue(qualifiedStorageArea.Value.ToString("."), out var indexHash)
                            &&
                            qualifiedStorageArea.Key is DataOrConditionStorageArea dataOrConditionStorageArea)
                        {
                            dataOrConditionStorageArea.Hash = indexHash;
                        }
                    }
                }

                //Rule: TCCODEGEN-FIXFOR-ALIGN-FUNCALL
                var indent = new string(' ', 13);
                //Hanle Input parameters
                //Rule: TCCODEGEN-FUNCALL-PARAMS
                TypeCobol.Compiler.CodeElements.ParameterSharingMode previousSharingMode = (TypeCobol.Compiler.CodeElements.ParameterSharingMode)(-1);
                int previousSpan = 0;
                if (Statement.ProcedureCall.InputParameters != null)
                    foreach (var parameter in Statement.ProcedureCall.InputParameters)
                    {
                        var name = ToString(parameter, Node, ArgMode.Input, ref previousSharingMode, ref previousSpan);
                        _cache.Add(new TextLineSnapshot(-1, indent + name, null));
                    }

                //Handle InOut parameters
                //Rule: TCCODEGEN-FUNCALL-PARAMS
                previousSharingMode = (TypeCobol.Compiler.CodeElements.ParameterSharingMode)(-1);
                previousSpan = 0;
                if (Statement.ProcedureCall.InoutParameters != null)
                    foreach (var parameter in Statement.ProcedureCall.InoutParameters)
                    {
                        var name = ToString(parameter, Node, ArgMode.InOut, ref previousSharingMode, ref previousSpan);
                        _cache.Add(new TextLineSnapshot(-1, indent + name, null));
                    }

                //handle Output paramaters
                //Rule: TCCODEGEN-FUNCALL-PARAMS
                previousSharingMode = (TypeCobol.Compiler.CodeElements.ParameterSharingMode)(-1);
                previousSpan = 0;
                if (Statement.ProcedureCall.OutputParameters != null)
                    foreach (var parameter in Statement.ProcedureCall.OutputParameters)
                    {
                        var name = ToString(parameter, Node, ArgMode.Output, ref previousSharingMode, ref previousSpan);
                        _cache.Add(new TextLineSnapshot(-1, indent + name, null));
                    }

                if (!HasCallEnd)
                {
                    //Rule: TCCODEGEN-FIXFOR-ALIGN-FUNCALL
                    var call_end = new TextLineSnapshot(-1, !bNeedEndIf ? "    end-call " : "        end-call ", null);
                    _cache.Add(call_end);
                }
                //We don't need end-if anymore, but I let it for now. Because this generated code still need to be tested on production
                if (bNeedEndIf)
                {
                    var end_guardTextLine = new TextLineSnapshot(-1, "    END-IF", null);
                    _cache.Add(end_guardTextLine);
                }

			}
			return _cache;
		}
	}


    /// <summary>
    /// Get the String representation of an parameter with a Sharing Mode.
    /// Rule: TCCODEGEN-FUNCALL-PARAMS
    /// </summary>
    /// <param name="parameter">The Parameter</param>
    /// <param name="node">The node</param>
    /// <param name="mode">Argument mode Input, InOut, Output, etc...</param>
    /// <param name="previousSharingMode">The previous Sharing Mode</param>
    /// <param name="previousSpan">The previous marging span</param>
    /// <returns>The String representation of the Sharing Mode paramaters</returns>
	private string ToString(TypeCobol.Compiler.CodeElements.CallSiteParameter parameter, Node node, ArgMode mode,
        ref TypeCobol.Compiler.CodeElements.ParameterSharingMode previousSharingMode, ref int previousSpan) {
        Variable variable = parameter.StorageAreaOrValue;
        bool bTypeBool = false;
        if (variable != null)
        {//We must detect a boolean variable
            if (!variable.IsLiteral)
            {
                var found = node.GetDataDefinitionFromStorageAreaDictionary(variable.StorageArea);
                if (found != null)
                {
                    var data = found as DataDescription;
                    bTypeBool = (data != null && data.DataType == DataType.Boolean);
                }
            }
        }

        var name = parameter.IsOmitted ? "omitted" : variable.ToString(true, bTypeBool);

        string share_mode = "";
        int defaultSpan = string.Intern("by reference ").Length;
        if (parameter.SharingMode.Token != null)
        {
            if (previousSharingMode != parameter.SharingMode.Value)
            {
                share_mode = "by " + parameter.SharingMode.Token.Text;
                share_mode += new string(' ', defaultSpan - share_mode.Length);
                previousSharingMode = parameter.SharingMode.Value;
            }
        }
        else
        {
            if (mode == ArgMode.InOut || mode == ArgMode.Output)
            {
                if (previousSharingMode != TypeCobol.Compiler.CodeElements.ParameterSharingMode.ByReference)
                {
                    share_mode = string.Intern("by reference ");
                    previousSharingMode = TypeCobol.Compiler.CodeElements.ParameterSharingMode.ByReference;
                }
            }
        }
        if (share_mode.Length == 0)
        {
            share_mode = new string(' ', previousSpan == 0 ? defaultSpan : previousSpan);
        }
        else
        {
            previousSpan = share_mode.Length;
        }

        if (variable != null) {
            if (variable.IsLiteral)
                return share_mode + name;
            var found = node.GetDataDefinitionFromStorageAreaDictionary(variable.StorageArea);
            if (found==null) {  //this can happens for special register : LENGTH OF, ADDRESS OF
                return share_mode + variable.ToCobol85();
            }
        }
        return share_mode + name;
	}

	public bool IsLeaf { get { return true; } }
}

}
