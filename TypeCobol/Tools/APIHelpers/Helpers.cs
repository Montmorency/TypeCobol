﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TypeCobol.Compiler;
using TypeCobol.Compiler.AntlrUtils;
using TypeCobol.Compiler.CodeElements;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Diagnostics;
using TypeCobol.Compiler.Directives;
using TypeCobol.Compiler.Scopes;
using TypeCobol.CustomExceptions;
using TypeCobol.Tools.Options_Config;

namespace TypeCobol.Tools.APIHelpers
{
    public static class Helpers
    {
        private static string[] _DependenciesExtensions = { ".tcbl", ".cbl", ".cpy" };

        public static Tuple<SymbolTable, RootSymbolTable> LoadIntrinsic(List<string> paths, DocumentFormat intrinsicDocumentFormat, EventHandler<DiagnosticsErrorEvent> diagEvent)
        {
            var parser = new Parser();
            parser.CustomRootSymbols = new RootSymbolTable();
            var diagnostics = new List<Diagnostic>();
            var table = new SymbolTable(null, SymbolTable.Scope.Intrinsic);
            var instrincicFiles = new List<string>();

            foreach (string path in paths) instrincicFiles.AddRange(FileSystem.GetFiles(path, parser.Extensions, false));

            foreach (string path in instrincicFiles)
            {
                try
                {
                    FileCompiler compiler = parser.Init(path, new TypeCobolOptions { ExecToStep = ExecutionStep.CrossCheck }, intrinsicDocumentFormat);
                    parser.Parse(path);

                    diagnostics.AddRange(parser.Results.AllDiagnostics());

                    if (diagEvent != null && diagnostics.Count > 0)
                    {
                        diagnostics.ForEach(d => diagEvent(null, new DiagnosticsErrorEvent() { Path = path, Diagnostic = d }));
                    }

                    if (parser.Results.ProgramClassDocumentSnapshot.Root.Programs == null || parser.Results.ProgramClassDocumentSnapshot.Root.Programs.Count() == 0)
                    {
                        throw new CopyLoadingException("Your Intrisic types/functions are not included into a program.", path, null, logged: true, needMail: false);
                    }

                    foreach (var program in parser.Results.ProgramClassDocumentSnapshot.Root.Programs)
                    {
                        var symbols = program.SymbolTable.GetTableFromScope(SymbolTable.Scope.Program);

                        if (symbols.Types.Count == 0 && symbols.Functions.Count == 0)
                        {
                            diagEvent?.Invoke(null, new DiagnosticsErrorEvent() { Path = path, Diagnostic = new ParserDiagnostic("No types and no procedures/functions found", 1, 1, 1, null, MessageCode.Warning) });
                            continue;
                        }

                        table.CopyAllTypes(symbols.Types);
                        table.CopyAllFunctions(symbols.Functions);
                    }
                }
                catch (CopyLoadingException copyException)
                {
                    throw copyException;
                }
                catch (Exception e)
                {
                    throw new CopyLoadingException(e.Message + "\n" + e.StackTrace, path, e, logged: true, needMail: true);
                }
            }

            return new Tuple<SymbolTable, RootSymbolTable>(table, parser.CustomRootSymbols);
        }

        /// <summary>
        /// Parse one dependency
        /// </summary>
        /// <param name="path">Path of the dependency to parse</param>
        /// <param name="config">Current configuration</param>
        /// <param name="customSymbols">Intrinsic or Namespace SymbolTable</param>
        /// <returns></returns>
        private static CompilationUnit ParseDependency(string path, [NotNull] TypeCobolConfiguration config, SymbolTable customSymbols, RootSymbolTable rootSymbolTable)
        {
            var options = new TypeCobolOptions(config) { ExecToStep = ExecutionStep.SemanticCheck };
            var parser = new Parser(customSymbols, rootSymbolTable);

            parser.Init(path, options, config.Format, config.CopyFolders);
            parser.Parse(path); //Parse the dependency file

            return parser.Results;
        }

        public static IEnumerable<string> GetDependenciesMissingCopies([NotNull] TypeCobolConfiguration config, SymbolTable intrinsicTable, RootSymbolTable rootSymbolTable, EventHandler<DiagnosticsErrorEvent> diagEvent)
        {
            List<CopyDirective> missingCopies = new List<CopyDirective>();

            // For all paths given in preferences
            foreach (var path in config.Dependencies)
            {
                // For each dependency source found in path
                foreach (string dependency in Tools.FileSystem.GetFiles(path, _DependenciesExtensions, true))
                {
                    missingCopies.AddRange(ParseDependency(dependency, config, intrinsicTable, rootSymbolTable).MissingCopies);
                }
            }

            // Return a list of name of the CopyDirective
            return missingCopies.Select(mc => mc.TextName);
        }

        public static Tuple<SymbolTable, RootSymbolTable> LoadDependencies([NotNull] TypeCobolConfiguration config, SymbolTable intrinsicTable,
            RootSymbolTable rootSymbolTable, EventHandler<DiagnosticsErrorEvent> diagEvent,
            out List<RemarksDirective.TextNameVariation> usedCopies,
            //Key : path of the dependency
            //Copy name not found
            out IDictionary<string, IEnumerable<string>> missingCopies)
        {
            usedCopies = new List<RemarksDirective.TextNameVariation>();
            missingCopies = new Dictionary<string, IEnumerable<string>>();
            var diagnostics = new List<Diagnostic>();
            var table = new SymbolTable(intrinsicTable, SymbolTable.Scope.Namespace); //Generate a table of NameSPace containing the dependencies programs based on the previously created intrinsic table. 

            var dependencies = new List<string>();
            foreach (var path in config.Dependencies)
            {
                var dependenciesFound = Tools.FileSystem.GetFiles(path, _DependenciesExtensions, true);
                //Issue #668, warn if dependencies path are invalid
                if (diagEvent != null && dependenciesFound.Count == 0)
                {
                    diagEvent(null, new DiagnosticsErrorEvent() { Path = path, Diagnostic = new ParserDiagnostic(path + ", no dependencies found", 1, 1, 1, null, MessageCode.DependenciesLoading) });
                }
                dependencies.AddRange(dependenciesFound); //Get File by name or search the directory for all files
            }

#if EUROINFO_RULES
            //Create list of inputFileName according to our naming convention in the case of an usage with RDZ
            var programsNames = new List<string>();
            foreach (var inputFile in config.InputFiles)
            {
                string PgmName = null;
                foreach (var line in File.ReadLines(inputFile))
                {
                    if (line.StartsWith("       PROGRAM-ID.", StringComparison.InvariantCultureIgnoreCase))
                    {
                        PgmName = line.Split('.')[1].Trim();
                        break;
                    }
                }

                programsNames.Add(PgmName);

            }
#endif

            foreach (string path in dependencies)
            {


#if EUROINFO_RULES
                //Issue #583, ignore a dependency if the same file will be parsed as an input file just after

                string depFileNameRaw = Path.GetFileNameWithoutExtension(path).Trim();

                if (depFileNameRaw != null)
                {
                    // substring in case of MYPGM.rdz.tcbl
                    var depFileName = depFileNameRaw.Substring(0,
                        depFileNameRaw.IndexOf(".", StringComparison.Ordinal) != -1 ?
                            depFileNameRaw.IndexOf(".", StringComparison.Ordinal) :
                            depFileNameRaw.Length
                    );
                    if (programsNames.Any(inputFileName => String.Compare(depFileName, inputFileName, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        continue;
                    }
                }
#endif
                try
                {
                    CompilationUnit parsingResult = ParseDependency(path, config, table, rootSymbolTable);

                    //Gather copies used
                    usedCopies.AddRange(parsingResult.CopyTextNamesVariations);

                    if (parsingResult.MissingCopies.Count > 0)
                    {
                        missingCopies.Add(path, parsingResult.MissingCopies.Select(mc => mc.TextName));
                        continue; //There will be diagnostics because copies are missing. Don't report diagnostic for this dependency, but load following dependencies
                    }

                    diagnostics.AddRange(parsingResult.AllDiagnostics());

                    if (diagEvent != null && diagnostics.Count > 0)
                    {
                        diagnostics.ForEach(d => diagEvent(null, new DiagnosticsErrorEvent() { Path = path, Diagnostic = d }));
                    }

                    if (parsingResult.TemporaryProgramClassDocumentSnapshot.Root.Programs == null || !parsingResult.TemporaryProgramClassDocumentSnapshot.Root.Programs.Any())
                    {
                        throw new DepedenciesLoadingException("Your dependency file is not included into a program", path, null, logged: true, needMail: false);
                    }

                    foreach (var program in parsingResult.TemporaryProgramClassDocumentSnapshot.Root.Programs)
                    {
                        var previousPrograms = table.GetPrograms();
                        foreach (var previousProgram in previousPrograms)
                        {
                            previousProgram.SymbolTable.GetTableFromScope(SymbolTable.Scope.Namespace).AddProgram(program);
                        }

                        //If there is no public types or functions, then call diagEvent
                        var programTable = program.SymbolTable.GetTableFromScope(SymbolTable.Scope.Program);
                        if (diagEvent != null
                            && !programTable.Types.Values.Any(tds => tds.Any(td => td.CodeElement.Visibility == AccessModifier.Public))       //No Public Types in Program table
                            && !programTable.Functions.Values.Any(fds => fds.Any(fd => fd.CodeElement.Visibility == AccessModifier.Public)))  //No Public Functions in Program table
                        {
                            diagEvent(null, new DiagnosticsErrorEvent() { Path = path, Diagnostic = new ParserDiagnostic(string.Format("No public types or procedures/functions found in {0}", program.Name), 1, 1, 1, null, MessageCode.Warning) });
                            continue;
                        }
                        table.AddProgram(program); //Add program to Namespace symbol table
                    }
                }
                catch (DepedenciesLoadingException depLoadingEx)
                {
                    throw depLoadingEx;
                }
                catch (Exception e)
                {
                    throw new DepedenciesLoadingException(e.Message + "\n" + e.StackTrace, path, e);
                }
            }

            //Reset symbolTable of all dependencies 

            return new Tuple<SymbolTable, RootSymbolTable>(table, rootSymbolTable);
        }
        /// <summary>
        /// Load both Intrinsic symbols and Dependencies files.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="DiagnosticsErrorEvent"></param>
        /// <param name="DependencyErrorEvent"></param>
        /// <returns></returns>
        public static Tuple<SymbolTable, RootSymbolTable> LoadIntrinsicAndDependencies(TypeCobolConfiguration config,
            EventHandler<Tools.APIHelpers.DiagnosticsErrorEvent> DiagnosticsErrorEvent, EventHandler<Tools.APIHelpers.DiagnosticsErrorEvent> DependencyErrorEvent,
            out List<RemarksDirective.TextNameVariation> usedCopies,
            //Key : path of the dependency
            //Copy name not found
            out IDictionary<string, IEnumerable<string>> missingCopies)
        {
            Tuple<SymbolTable, RootSymbolTable> customSymbols = Tools.APIHelpers.Helpers.LoadIntrinsic(config.Copies, config.Format, DiagnosticsErrorEvent); //Load intrinsic
            customSymbols = Tools.APIHelpers.Helpers.LoadDependencies(config, customSymbols.Item1, customSymbols.Item2, DependencyErrorEvent, out usedCopies, out missingCopies); //Load dependencies
            return customSymbols;
        }
    }

    public class DiagnosticsErrorEvent : EventArgs
    {
        public string Path { get; set; }
        public Diagnostic Diagnostic { get; set; }
    }
}
