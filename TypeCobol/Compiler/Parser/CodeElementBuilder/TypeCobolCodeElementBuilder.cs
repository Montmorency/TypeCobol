﻿using System;

namespace TypeCobol.Compiler.Parser
{

    using System.Collections.Generic;
    using TypeCobol.Compiler.AntlrUtils;
    using TypeCobol.Compiler.CodeElements;
    using TypeCobol.Compiler.CodeElements.Expressions;
    using TypeCobol.Compiler.Parser.Generated;
    using TypeCobol.Compiler.Scanner;
    using System.Linq;



    internal partial class CodeElementBuilder : CodeElementsBaseListener
    {

        public override void EnterLibraryCopy(CodeElementsParser.LibraryCopyContext context)
        {
            var copy = new LibraryCopyCodeElement();
            if (context.qualifiedTextName() != null)
            {
                // TCRFUN_LIBRARY_COPY
                copy.Name = CobolWordsBuilder.CreateQualifiedTextName(context.qualifiedTextName()); //TODO#278 eww!
            }
            Context = context;
            CodeElement = copy;
        }

        public override void EnterFunctionDeclarationHeader(CodeElementsParser.FunctionDeclarationHeaderContext context)
        {
            var type = FunctionType.Undefined;
            if (context.PROCEDURE() != null) type = FunctionType.Procedure;
            if (context.FUNCTION() != null) type = FunctionType.Function;

            // TCRFUN_NO_DEFAULT_ACCESS_MODIFIER
            // As the grammar enforces that there must be one least one of the PUBLIC or PRIVATE keywords,
            // there will be a syntax error if there is neither of these two keywords.
            // So, the fact of considering a function PRIVATE by default does not break this rule.
            var visibility = context.PUBLIC() != null ? AccessModifier.Public : AccessModifier.Private;

            SymbolDefinition name = null;
            if (context.functionNameDefinition() != null)
            {
                name = CobolWordsBuilder.CreateFunctionNameDefinition(context.functionNameDefinition());
            }
            Context = context;
            CodeElement = new FunctionDeclarationHeader(name, visibility, type);
        }

        public override void EnterInputPhrase(CodeElementsParser.InputPhraseContext context)
        {
            var ce = (FunctionDeclarationHeader) CodeElement;
            ce.Input = new SyntaxProperty<ParameterPassingDirection>(ParameterPassingDirection.Input,
                ParseTreeUtils.GetTokenFromTerminalNode(context.INPUT()));
            ce.Profile.InputParameters = CreateParameters(context.parameterDescription());
        }

        public override void EnterOutputPhrase(CodeElementsParser.OutputPhraseContext context)
        {
            var ce = (FunctionDeclarationHeader) CodeElement;
            ce.Output = new SyntaxProperty<ParameterPassingDirection>(ParameterPassingDirection.Output,
                ParseTreeUtils.GetTokenFromTerminalNode(context.OUTPUT()));
            ce.Profile.OutputParameters = CreateParameters(context.parameterDescription());
        }

        public override void EnterInoutPhrase(CodeElementsParser.InoutPhraseContext context)
        {
            var ce = (FunctionDeclarationHeader) CodeElement;
            ce.Inout = new SyntaxProperty<ParameterPassingDirection>(ParameterPassingDirection.InOut,
                ParseTreeUtils.GetTokenFromTerminalNode(context.IN_OUT()));
            ce.Profile.InoutParameters = CreateParameters(context.parameterDescription());
        }

        public override void EnterFunctionReturningPhrase(CodeElementsParser.FunctionReturningPhraseContext context)
        {
            var ce = (FunctionDeclarationHeader) CodeElement;
            ce.Returning = new SyntaxProperty<ParameterPassingDirection>(ParameterPassingDirection.Returning,
                ParseTreeUtils.GetTokenFromTerminalNode(context.RETURNING()));
            if (context.parameterDescription().functionDataParameter() != null)
            {
                var entry = CreateFunctionDataParameter(context.parameterDescription().functionDataParameter());
                ce.Profile.ReturningParameter = entry;
            }
        }

        private IList<ParameterDescriptionEntry> CreateParameters(
            CodeElementsParser.ParameterDescriptionContext[] contexts)
        {
            var parameters = new List<ParameterDescriptionEntry>();
            foreach (var context in contexts)
            {
                if (context.functionDataParameter() != null)
                {
                    var data = CreateFunctionDataParameter(context.functionDataParameter());
                    parameters.Add(data);
                }
                else if (context.functionConditionParameter() != null)
                {
                    var condition = CreateFunctionConditionParameter(context.functionConditionParameter());
                    if (parameters.Count < 1)
                    {
                        var data = CreateFunctionDataParameter(condition);
                        parameters.Add(data);
                    }
                    else
                    {
                        var parameter = parameters[parameters.Count - 1];
                        if (parameter.DataConditions == null) parameter.DataConditions = new List<DataConditionEntry>();
                        parameter.DataConditions.Add(condition);
                    }
                }
            }
            return parameters;
        }

        private ParameterDescriptionEntry CreateFunctionDataParameter(DataConditionEntry condition)
        {
            var data = new ParameterDescriptionEntry();
            data.LevelNumber = condition.LevelNumber;
            data.DataName = condition.DataName;
            data.DataType = DataType.Unknown;
            return data;
        }

        public ParameterDescriptionEntry CreateFunctionDataParameter(
            CodeElementsParser.FunctionDataParameterContext context)
        {
            var parameter = new ParameterDescriptionEntry();
            parameter.LevelNumber = new GeneratedIntegerValue(1);
            parameter.DataName = CobolWordsBuilder.CreateDataNameDefinition(context.dataNameDefinition());
            if (context.pictureClause() != null)
            {
                parameter.Picture =
                    CobolWordsBuilder.CreateAlphanumericValue(context.pictureClause().pictureCharacterString);
                parameter.DataType = DataType.Create(parameter.Picture.Value);
            }
            else if (context.cobol2002TypeClause() != null)
            {
                parameter.UserDefinedDataType =
                    CobolWordsBuilder.CreateDataTypeNameReference(context.cobol2002TypeClause().dataTypeNameReference());
                parameter.DataType = DataType.CreateCustom(parameter.UserDefinedDataType.Name);
            }
            //TODO#245: subphrases
            return parameter;
        }

        private DataConditionEntry CreateFunctionConditionParameter(
            CodeElementsParser.FunctionConditionParameterContext context)
        {
            var parameter = new DataConditionEntry();
            parameter.LevelNumber = CobolWordsBuilder.CreateIntegerValue(context.levelNumber);
            parameter.DataName = CobolWordsBuilder.CreateConditionNameDefinition(context.conditionNameDefinition());
            SetConditionValues(parameter, context.valueClauseForCondition());
            return parameter;
        }

        public override void ExitFunctionDeclarationHeader(CodeElementsParser.FunctionDeclarationHeaderContext context)
        {
            // Register call parameters (shared storage areas) information at the CodeElement level
            var function = (FunctionDeclarationHeader) CodeElement;
            var target = new CallTarget() {Name = function.FunctionName};
            int parametersCount = function.Profile.InputParameters.Count
                                  + function.Profile.InoutParameters.Count
                                  + function.Profile.OutputParameters.Count
                                  + (function.Profile.ReturningParameter != null ? 1 : 0);
            target.Parameters = new CallTargetParameter[parametersCount];
            int i = 0;
            foreach (var param in function.Profile.InputParameters)
            {
                target.Parameters[i++] = CreateCallTargetParameter(param);
            }
            foreach (var param in function.Profile.OutputParameters)
            {
                target.Parameters[i++] = CreateCallTargetParameter(param);
            }
            foreach (var param in function.Profile.InoutParameters)
            {
                target.Parameters[i++] = CreateCallTargetParameter(param);
            }
            if (function.Profile.ReturningParameter != null)
            {
                target.Parameters[i++] = CreateCallTargetParameter(function.Profile.ReturningParameter);
            }
            function.CallTarget = target;

            Context = context;
            CodeElement = function;
        }

        private static CallTargetParameter CreateCallTargetParameter(ParameterDescriptionEntry param)
        {
            var symbolReference = new SymbolReference(param.DataName);
            var storageArea = new DataOrConditionStorageArea(symbolReference);
            var callParameter = new CallTargetParameter {StorageArea = storageArea};
            return callParameter;
        }

        public override void EnterFunctionDeclarationEnd(CodeElementsParser.FunctionDeclarationEndContext context)
        {
            Context = context;
            CodeElement = new FunctionDeclarationEnd();
        }



        ////////////////////
        // PROCEDURE CALL //
        ////////////////////

        public override void EnterTcCallStatement(CodeElementsParser.TcCallStatementContext context)
        {
            if (context.functionNameReference() != null)
            {
                throw new NotImplementedException("should not pass here");
            }

            var cbCallProc = context.programNameOrProgramEntryOrProcedurePointerOrFunctionPointerVariable();

            var inputs = new List<CallSiteParameter>();
            var inouts = new List<CallSiteParameter>();
            var outputs = new List<CallSiteParameter>();

            // Register call parameters (shared storage areas) information at the CodeElement level
            CallSite callSite = new CallSite();
            Context = context;



            //Here ambiguousSymbolReference with either CandidatesType:
            // - ProgramNameOrProgramEntry
            // - data, condition, UPSISwitch, TCFunctionName
            var ambiguousSymbolReference =
                CobolExpressionsBuilder
                    .CreateProgramNameOrProgramEntryOrProcedurePointerOrFunctionPointerVariableOrTCFunctionProcedure(cbCallProc);
            //If (inputs, inouts ou outputs).Count > 0, then it's a procedure call
                //Check temp.Type or CandidatesTypes to see if a TCFunctionName is possible
                //If so, create ProcedureStyleCallStatement with a ProcedureCall and fix SymbolReference so it's not ambiguous 
                var statement = new ProcedureStyleCallStatement(new ProcedureCall(ambiguousSymbolReference.MainSymbolReference, inputs, inouts, outputs))
                    {
                        ProgramOrProgramEntryOrProcedureOrFunctionOrTCProcedureFunction = ambiguousSymbolReference
                    };
                //else it's an error
                    //Create codeElement with all information but tag it as "in error"

            //Else no argument, then check temp.Type or CandidatesTypes
                //if CandidatesTypes = Program or program Entry
                    //new ProcedureStyleCallStatement() { ProgramNameOrProgramEntry= temp}
                //else if CandidatesTypes = data or TCFunctionName
                //new ProcedureStyleCallStatement() {ProcdurePointerOrTCProcedureFunction = temp}
                //else error, but store temp in ProgramOrProgramEntry or DataOrTCProcedureFunction ?


            callSite.CallTarget = statement.ProgramOrProgramEntryOrProcedureOrFunctionOrTCProcedureFunction.MainSymbolReference;
           

            SyntaxProperty<ParameterSharingMode> mode = null;
            foreach (var p in context.callInputParameter())
            {
                CreateSharingMode(p, ref mode); // TCRFUN_INPUT_BY
                inputs.Add(new CallSiteParameter
                {
                    SharingMode = mode,
                    StorageAreaOrValue =
                        CobolExpressionsBuilder.CreateSharedVariableOrFileName(p.sharedVariableOrFileName()),
                });
            }
            
            foreach (var p in context.callInoutParameter())
            {
                inouts.Add(new CallSiteParameter
                {
                    // TCRFUN_CALL_INOUT_AND_OUTPUT_BY_REFERENCE
                    SharingMode = new SyntaxProperty<ParameterSharingMode>(ParameterSharingMode.ByReference, null),
                    StorageAreaOrValue =
                        new Variable(CobolExpressionsBuilder.CreateSharedStorageArea(p.sharedStorageArea1())),
                });
            }
          
            foreach (var p in context.callOutputParameter())
            {
                outputs.Add(new CallSiteParameter
                {
                    // TCRFUN_CALL_INOUT_AND_OUTPUT_BY_REFERENCE
                    SharingMode = new SyntaxProperty<ParameterSharingMode>(ParameterSharingMode.ByReference, null),
                    StorageAreaOrValue =
                        new Variable(CobolExpressionsBuilder.CreateSharedStorageArea(p.sharedStorageArea1())),
                });
            }

            int parametersCount = inputs.Count + outputs.Count + inouts.Count;
            callSite.Parameters = new CallSiteParameter[parametersCount];
            int i = 0;

            //Add inputs to global callsites parameters
            if (inputs.Count > 0)
            {
                foreach (var param in inputs)
                {
                    callSite.Parameters[i] = param;
                    i++;
                }
            }

            //Add outputs to global callsites parameters
            if (outputs.Count > 0)
            {
                foreach (var param in outputs)
                {
                    callSite.Parameters[i] = param;
                    i++;
                }
            }

            //Add inouts to global callsites parameters
            if (inouts.Count > 0)
            {
                foreach (var param in inouts)
                {
                    callSite.Parameters[i] = param;
                    i++;
                }
            }

            if (statement.CallSites == null) statement.CallSites = new List<CallSite>();
            statement.CallSites.Add(callSite);

            CodeElement = statement;
            
        }

        private void CreateSharingMode(CodeElementsParser.CallInputParameterContext parameter,
            ref SyntaxProperty<ParameterSharingMode> mode)
        {
            if (parameter.REFERENCE() != null)
            {
                mode = CobolStatementsBuilder.CreateSyntaxProperty(ParameterSharingMode.ByReference,
                    parameter.REFERENCE());
            }
            else if (parameter.CONTENT() != null)
            {
                mode = CobolStatementsBuilder.CreateSyntaxProperty(ParameterSharingMode.ByContent, parameter.CONTENT());
            }
            else if (parameter.VALUE() != null)
            {
                mode = CobolStatementsBuilder.CreateSyntaxProperty(ParameterSharingMode.ByValue, parameter.VALUE());
            }
            else
            {
                var by = ParameterSharingMode.ByReference;
                if (mode != null) by = mode.Value;
                mode = new SyntaxProperty<ParameterSharingMode>(by, null);
            }
        }

    }

}
