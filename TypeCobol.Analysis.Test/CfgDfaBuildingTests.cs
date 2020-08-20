﻿using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeCobol.Compiler.Symbols;
using System.IO;
using TypeCobol.Analysis.Dfa;
using TypeCobol.Analysis.Graph;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.CupParser.NodeBuilder;
using TypeCobol.Compiler.Directives;
using TypeCobol.Compiler.Nodes;
using TypeCobol.Compiler.Text;

using static TypeCobol.Analysis.Test.CfgTestUtils;

namespace TypeCobol.Analysis.Test
{
    /// <summary>
    /// These are simple tests to validate Cfg/Dfa constructions with miscellaneous bugs.
    /// </summary>
    [TestClass]
    public class CfgDfaBuildTests
    {
        /// <summary>
        /// Class to listen that and ExecSqlStatement has been seen.
        /// </summary>
        class ExecNodeListener : SyntaxDrivenAnalyzerBase
        {
            internal bool ExecSqlSeen;
			internal ExecNodeListener() : base("CfgBuildTest.ExecNodeListener")
            {
                ExecSqlSeen = false;
            }
            public override void OnNode(Node node, Program program)
            {
				if (node?.CodeElement != null && node.CodeElement.Type == Compiler.CodeElements.CodeElementType.ExecStatement)
					ExecSqlSeen = true;
            }

            public override object GetResult()
            {
                return null;
            }
        }

        /// <summary>
        /// Test that EXEC SQL statement outside a PROCEDURE DIVISION does not generate basic blocks.
        /// </summary>
        [TestMethod]
        [TestCategory("CfgDfaBuildTest")]
        public void ExecSqlOutsideProc()
        {
            ExecNodeListener listener = null;
            ISyntaxDrivenAnalyzer CreateExecNodeListener(TypeCobolOptions o, TextSourceInfo t) => listener = new ExecNodeListener();

            string path = Path.Combine(CfgTestUtils.CfgDfaBuildTests, "ExecSqlOutsideProc.cbl");
            var graphs = ParseCompareDiagnostics<DfaBasicBlockInfo<Symbol>>(path, CfgBuildingMode.WithDfa, null, CreateExecNodeListener);
            Assert.IsTrue(graphs.Count == 1);

            //Check generated graph has not been initialized
            Assert.IsFalse(graphs[0].IsInitialized);

            //Check that Exec SQL has been detected
            Assert.IsNotNull(listener);
            Assert.IsTrue(listener.ExecSqlSeen);
        }

        /// <summary>
        /// Simple execution test of some DFA algorithms.
        /// </summary>
        [TestMethod]
        [TestCategory("CfgDfaBuildTest")]
        public void PrgWithNoProcDiv()
        {
            string path = Path.Combine(CfgTestUtils.CfgDfaBuildTests, "PrgWithNoProcDiv.cbl");
            IList<ControlFlowGraph<Node, DfaBasicBlockInfo<Symbol>>> cfg = ParseCompareDiagnosticsForDfa(path);
            Assert.IsTrue(cfg.Count == 1);

            //Try to compute predecessor edges.
            cfg[0].SetupPredecessorEdges();

            //Test Empty Cfg Generated.
            string expectedPath = Path.Combine(CfgTestUtils.CfgDfaBuildTests, "EmptyCfg.dot");
            GenDotCfgAndCompare(cfg[0], path, expectedPath, true);

            //Test DFA algorithms.
            DefaultDataFlowGraphBuilder dfaBuilder = new DefaultDataFlowGraphBuilder(cfg[0]);
            dfaBuilder.ComputeUseList();
            dfaBuilder.ComputeDefList();
            dfaBuilder.ComputeGenSet();
            dfaBuilder.ComputeKillSet();
            dfaBuilder.ComputeInOutSet();
            dfaBuilder.ComputeUseDefSet();
        }
    }
}
