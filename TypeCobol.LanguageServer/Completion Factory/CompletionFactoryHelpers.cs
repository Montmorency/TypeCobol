﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Nodes;
using TypeCobol.Compiler.Scanner;
using TypeCobol.LanguageServer.SignatureHelper;
using TypeCobol.LanguageServer.VsCodeProtocol;

namespace TypeCobol.LanguageServer
{
    public static class CompletionFactoryHelpers
    {

        /// <summary>
        /// Help to resolve procedure name inside consumed tokens.
        /// Will return a string containing only a proc name or an entire qualified name for the procedure (depending on the given tokens)
        /// </summary>
        /// <param name="consumedTokens"></param>
        /// <returns></returns>
        public static string GetProcedureNameFromTokens(List<Token> consumedTokens)
        {
            //Get procedure name or qualified name
            return string.Join(".", consumedTokens
                .Skip(1) //Skip the CALL token
                .TakeWhile(t => t.TokenType != TokenType.INPUT
                                && t.TokenType != TokenType.OUTPUT
                                && t.TokenType != TokenType.IN_OUT) // Take tokens until keyword found
                .Where(t => t.TokenType == TokenType.UserDefinedWord)
                .Select(t => t.Text));
        }

        public static IEnumerable<string> AggregateTokens(IEnumerable<Token> tokensToAggregate)
        {
            var aggregatedTokens = new Stack<string>();

            Token previousToken = null;
            foreach (var token in tokensToAggregate)
            {
                if (previousToken != null && previousToken.TokenType == TokenType.UserDefinedWord)
                {
                    if (token.TokenType != TokenType.QualifiedNameSeparator)
                    {
                        aggregatedTokens.Push(token.Text);
                    }
                    else if (previousToken.TokenType == TokenType.UserDefinedWord)
                    {
                        var retainedString = aggregatedTokens.Pop();
                        aggregatedTokens.Push(retainedString + ".");
                    }
                }
                else if (previousToken != null && previousToken.TokenType == TokenType.QualifiedNameSeparator)
                {
                    var retainedString = aggregatedTokens.Pop();
                    aggregatedTokens.Push(retainedString + token.Text);
                }

                if (previousToken == null && token.TokenType == TokenType.UserDefinedWord)
                    aggregatedTokens.Push(token.Text);

                previousToken = token;
            }

            return aggregatedTokens.ToArray().Reverse();
        }

        public static IEnumerable<CompletionItem> CreateCompletionItemsForType(IEnumerable<TypeDefinition> types, Node node, bool enablePublicFlag = true)
        {
            var completionItems = new List<CompletionItem>();

            foreach (var type in types)
            {
                bool typeIsPublic = false;
                bool typeIsIntrinsic = type.IsFlagSet(Node.Flag.NodeIsIntrinsic);
                if (enablePublicFlag)
                {
                    typeIsPublic = type.CodeElement?.Visibility == AccessModifier.Public
                                   && !(type.GetProgramNode() == node.GetProgramNode()  //Ignore public if type is in the current program
                                    || typeIsIntrinsic); //Ignore public if type is in intrinsic
                }
                    

                var typeDisplayName = typeIsPublic ? type.VisualQualifiedName.ToString() : type.Name;
                var completionItem = new CompletionItem(typeDisplayName);

                if (!(node is FunctionDeclaration))
                    if (typeIsPublic)
                    {
                        completionItem.insertText =
                            //Check if last element is of type PeriodSperator (ie : a dot), so the completion does not make a duplicate
                            (node.CodeElement.ConsumedTokens.Last().TokenType == TokenType.PeriodSeparator)
                                ? $"{type.VisualQualifiedName.Tail}::{type.VisualQualifiedName.Head}" 
                                : $"{type.VisualQualifiedName.Tail}::{type.VisualQualifiedName.Head}.";
                    }
                    else
                    {
                        completionItem.insertText =
                            //Check if last element is of type PeriodSperator (ie : a dot), so the completion does not make a duplicate
                            (node.CodeElement.ConsumedTokens.Last().TokenType == TokenType.PeriodSeparator)
                                ? completionItem.insertText = type.Name
                                : completionItem.insertText = type.Name + ".";
                    }

                completionItem.kind = typeIsIntrinsic ? CompletionItemKind.IntrinsicType : CompletionItemKind.Class;
                completionItems.Add(completionItem);
            }
            return completionItems;
        }

        public static IEnumerable<CompletionItem> CreateCompletionItemsForProcedures(IEnumerable<FunctionDeclaration> procedures, Node node, Dictionary<SignatureInformation, FunctionDeclaration> functionDeclarationSignatureDictionary,  bool enablePublicFlag = true)
        {
            var completionItems = new List<CompletionItem>();

            Case textCase = GetTextCase(node.CodeElement.ConsumedTokens.First(t => t.TokenType == TokenType.CALL).Text);
            Dictionary<ParameterDescription.PassingTypes, string> paramWithCase = GetParamsWithCase(textCase);

            foreach (var proc in procedures)
            {
                string inputParams = null, outputParams = null, inoutParams = null;

                if (proc.Profile != null)
                {
                    if (proc.Profile.InputParameters != null && proc.Profile.InputParameters.Count > 0)
                        inputParams = string.Format("{0} {1}",
                            paramWithCase[ParameterDescription.PassingTypes.Input],
                            string.Join(", ",
                                proc.Profile.InputParameters.Select(
                                    p => string.Format("{0}({1})", p.DataName, p.DataType.Name))));
                    if (proc.Profile.InoutParameters != null && proc.Profile.InoutParameters.Count > 0)
                        inoutParams = string.Format("{0} {1}",
                            paramWithCase[ParameterDescription.PassingTypes.InOut],
                            string.Join(", ",
                                proc.Profile.InoutParameters.Select(
                                    p => string.Format("{0}({1})", p.DataName, p.DataType.Name))));
                    if (proc.Profile.OutputParameters != null && proc.Profile.OutputParameters.Count > 0)
                        outputParams = string.Format("{0} {1}",
                            paramWithCase[ParameterDescription.PassingTypes.Output],
                            string.Join(", ",
                                proc.Profile.OutputParameters.Select(
                                    p => string.Format("{0}({1})", p.DataName, p.DataType.Name))));

                }
                bool procIsPublic = false;
                if (enablePublicFlag)
                    procIsPublic = proc.CodeElement.Visibility == AccessModifier.Public
                                   &&
                                   !(node.SymbolTable.GetTableFromScope(SymbolTable.Scope.Program)
                                         .Functions.Values.Any(t => t.Contains(proc))
                                     //Ignore public if proc is in the current program
                                     || proc.IsFlagSet(Node.Flag.NodeIsIntrinsic)); //Ignore public if proc is in intrinsic;
                var procDisplayName = procIsPublic ? proc.VisualQualifiedName.ToString() : proc.Name;
                var completionItem =
                    new CompletionItem(string.Format("{0} {1} {2} {3}", procDisplayName, inputParams, inoutParams, outputParams));
                completionItem.insertText = procIsPublic
                    ? inputParams != null
                            ? string.Format("{0}::{1} {2}", proc.VisualQualifiedName.Tail, proc.VisualQualifiedName.Head, paramWithCase[ParameterDescription.PassingTypes.Input])
                            : inoutParams != null
                                ? string.Format("{0}::{1} {2}", proc.VisualQualifiedName.Tail, proc.VisualQualifiedName.Head, paramWithCase[ParameterDescription.PassingTypes.InOut])
                                : outputParams != null
                                    ? string.Format("{0}::{1} {2}", proc.VisualQualifiedName.Tail, proc.VisualQualifiedName.Head, paramWithCase[ParameterDescription.PassingTypes.Output])
                                    : string.Format("{0}::{1}", proc.VisualQualifiedName.Tail, proc.VisualQualifiedName.Head)
                    : inputParams != null
                        ? proc.Name + " " + paramWithCase[ParameterDescription.PassingTypes.Input]
                        : inoutParams != null
                            ? proc.Name + paramWithCase[ParameterDescription.PassingTypes.InOut]
                            : outputParams != null
                                ? proc.Name + " " + paramWithCase[ParameterDescription.PassingTypes.Output]
                                : proc.Name;
                completionItem.kind = proc.Profile != null && proc.Profile.IsFunction ? CompletionItemKind.Function : CompletionItemKind.Method;
                //Add specific data for eclipse completion & signatureHelper context
                completionItem.data = new object[3];
                var signatureInformation = ProcedureSignatureHelper.SignatureHelperSignatureFormatter(proc);
                ((object[])completionItem.data)[1] = signatureInformation;

                //Store the link between the hash and the procedure. This will help to determine the procedure parameter completion context later. 
                functionDeclarationSignatureDictionary.Add(signatureInformation, proc);
                completionItems.Add(completionItem);
            }

            return completionItems;
        }

        public static IEnumerable<CompletionItem> CreateCompletionItemsForVariables(IEnumerable<DataDefinition> variables, bool useQualifiedName = true)
        {
            var completionItems = new List<CompletionItem>();

            foreach (var variable in variables)
            {
                completionItems.Add(CreateCompletionItemForVariable(variable, useQualifiedName));
            }

            return completionItems;
        }

        public static CompletionItem CreateCompletionItemForVariable(DataDefinition variable, bool useQualifiedName = true)
        {

            var qualifiedName = variable.VisualQualifiedName.Skip(variable.VisualQualifiedName.Count > 1 ? 1 : 0); //Skip Program Name

            var finalQualifiedName = qualifiedName.ToList();

            if (variable.CodeElement != null && variable.CodeElement.IsInsideCopy())
            {
                finalQualifiedName.Clear();
               
#if EUROINFO_RULES
                var lastSplited = qualifiedName.Last().Split('-');
                if(!qualifiedName.First().Contains(lastSplited.First()))
                    finalQualifiedName.Add(qualifiedName.First());
#else
                finalQualifiedName.Add(qualifiedName.First());
                
#endif
                if (qualifiedName.First() != qualifiedName.Last())
                    finalQualifiedName.Add(qualifiedName.Last());

            }

            var variableArrangedQualifiedName = useQualifiedName ? string.Join("::", finalQualifiedName) : variable.Name;

            var variableDisplay = string.Format("{0} ({1}) ({2})", variable.Name, variable.DataType.Name, variableArrangedQualifiedName);
            return new CompletionItem(variableDisplay) { insertText = variableArrangedQualifiedName, kind = CompletionItemKind.Variable };
        }

        public static CompletionItem CreateCompletionItemForIndex(Compiler.CodeElements.SymbolInformation index, DataDefinition variable)
        {
            var display = string.Format("{0} (from {1})", index.Name, variable.Name);
            return new CompletionItem(display) {insertText = index.Name, kind = CompletionItemKind.Variable};
        }

        public static Case GetTextCase(string tokenText)
        {
            if (string.IsNullOrEmpty(tokenText)) return Case.Lower;
            // check if upper case
            bool isUpper = true;
            foreach (char c in tokenText)
            {
                if (char.IsLower(c))
                {
                    isUpper = false;
                    break;
                }
            }

            if (isUpper) return Case.Upper;
            // check if Camel case; ex. CamlSample
            bool isCamel = false;
            int iUpperMem = -1, count = tokenText.Length;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    // first letter
                    if (char.IsLower(tokenText[i])) break;
                    iUpperMem = 0;
                    isCamel = true;
                }
                else
                {
                    if (char.IsUpper(tokenText[i]))
                    {
                        if (iUpperMem == (i - 1))
                        {
                            // 2 upper case characters one behind the other
                            isCamel = false;
                            break;
                        }
                        iUpperMem = i;
                    }
                }
            }

            if (isCamel && iUpperMem == count - 1) isCamel = false; // last char is upper
            return (isCamel) ? Case.Camel : Case.Lower;
        }

        private static readonly Dictionary<ParameterDescription.PassingTypes, string> _UpperParams = new Dictionary<ParameterDescription.PassingTypes, string>()
        {
            { ParameterDescription.PassingTypes.Input, "INPUT"},
            { ParameterDescription.PassingTypes.Output, "OUTPUT" },
            { ParameterDescription.PassingTypes.InOut, "IN-OUT" }
        };
        private static readonly Dictionary<ParameterDescription.PassingTypes, string> _CamelParams = new Dictionary<ParameterDescription.PassingTypes, string>()
        {
            { ParameterDescription.PassingTypes.Input, "Input"},
            { ParameterDescription.PassingTypes.Output, "Output" },
            { ParameterDescription.PassingTypes.InOut, "In-Out" }
        };
        private static readonly Dictionary<ParameterDescription.PassingTypes, string> _LowerParams = new Dictionary<ParameterDescription.PassingTypes, string>()
        {
            { ParameterDescription.PassingTypes.Input, "input"},
            { ParameterDescription.PassingTypes.Output, "output" },
            { ParameterDescription.PassingTypes.InOut, "in-out" }
        };

        public static Dictionary<ParameterDescription.PassingTypes, string> GetParamsWithCase(Case textCase)
        {
            switch (textCase)
            {
                case Case.Upper:
                    return _UpperParams;
                case Case.Camel:
                    return _CamelParams;
                default:
                    return _LowerParams;
            }
        }
        public enum Case
        {
            Lower = 0,   // default value
            Upper,
            Camel
        }
    }
}
