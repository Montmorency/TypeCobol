﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeCobol.Analysis.Graph
{
    /// <summary>
    /// A set of basic blocks, each of which has a list of successor blocks and some other information.
    /// Each block consists of a list of instructions, each of which can point to previous instructions that compute the operands it consumes.
    /// </summary>
    /// <typeparam name="N"></typeparam>
    /// <typeparam name="D"></typeparam>
    public class ControlFlowGraph<N, D>
    {
        /// <summary>
        /// BasicBlock calllback type.
        /// </summary>
        /// <param name="block">The accessed BasicBlock</param>
        /// <param name="edge">Edge from which to access to the block</param>
        /// <param name="adjacentBlock">The adjacent block that access to the Block by the edge</param>
        /// <param name="cfg">The Control Flow Graph in wich the basic Block belongs to.</param>
        /// <returns>true if ok, false otherwise</returns>
        public delegate bool BasicBlockCallback(BasicBlock<N, D> block, int edge, BasicBlock<N, D> adjacentBlock, ControlFlowGraph<N, D> cfg);

        /// <summary>
        /// Flag on a Cfg.
        /// </summary>
        [Flags]
        public enum Flags : uint
        {
            Compound = 0x01 << 0,       //Flag to indicate that this Cfg graph has subgraphs.
        }

        /// <summary>
        /// Symbol Flags.
        /// </summary>
        public Flags Flag
        {
            get;
            internal set;
        }

        /// <summary>
        /// Set a set of flags to true or false.
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        internal virtual void SetFlag(Flags flag, bool value)
        {
            this.Flag = value ? (Flags)(this.Flag | flag)
                              : (Flags)(this.Flag & ~flag);
        }

        /// <summary>
        /// Determines if the given flag is set.
        /// </summary>
        /// <param name="flag">The flag to be tested</param>
        /// <returns>true if yes, false otherwise.</returns>
        public bool HasFlag(Flags flag)
        {
            return (this.Flag & flag) != 0;
        }

        /// <summary>
        /// Root blocks. Usually it is a singleton it is the first block in the program, but alos on Exception handlers.
        /// </summary>
        public List<BasicBlock<N, D>> RootBlocks
        {
            get;
            internal set;
        }

        /// <summary>
        /// All blocks. A list of basic blocks in the graph.
        /// </summary>
        public List<BasicBlock<N, D>> AllBlocks
        {
            get;
            internal set;
        }

        /// <summary>
        /// A map from Node to corresponding basic block.
        /// </summary>
        public Dictionary<N, BasicBlock<N,D>> BlockFor
        {
            get;
            internal set;
        }

        /// <summary>
        /// The list of all Successor edges. The successor list for each basic block is a sublist of this list
        /// </summary>
        public List<BasicBlock<N, D>> SuccessorEdges
        {
            get;
            internal set;
        }

        /// <summary>
        /// The Node of the program for which this control Flow Graph has been created.
        /// </summary>
        public N ProgramNode
        {
            get;
            internal set;
        }

        /// <summary>
        /// The Node of the procedure for which this control Flow Graph has been created.
        /// </summary>
        public N ProcedureNode
        {
            get;
            internal set;
        }

        /// <summary>
        /// Initialize the construction of the Control Flow Graph.
        /// </summary>
        internal virtual void Initialize()
        {
            BlockFor = new Dictionary<N, BasicBlock<N, D>>();
            AllBlocks = new List<BasicBlock<N, D>>();
            RootBlocks = new List<BasicBlock<N, D>>();
            SuccessorEdges = new List<BasicBlock<N, D>>();
        }

        /// <summary>
        /// All basic blocks that can be reached via control flow out of the given basic block.
        /// </summary>
        /// <param name="basicBlock">The basic block to get the successors</param>
        /// <returns>The sublist of successors</returns>
        public List<BasicBlock<N,D>> SuccessorsFor(BasicBlock<N, D> basicBlock)
        {
            System.Diagnostics.Contracts.Contract.Requires(basicBlock != null);
            System.Diagnostics.Contracts.Contract.Assume(basicBlock.SuccessorEdges != null);
            List<BasicBlock<N, D>> result = new List<BasicBlock<N, D>>();
            foreach(var n in basicBlock.SuccessorEdges)
            {
                result.Add(SuccessorEdges[n]);
            }
            return result;
        }

        /// <summary>
        /// DFS Depth First Search implementation
        /// </summary>
        /// <param name="block">The current block</param>
        /// <param name="edgePredToBlock">Edge of the next block to the block</param>
        /// <param name="predBlock">The predecessor Block</param>
        /// <param name="discovered">Array of already discovered nodes</param>
        /// <param name="callback">CallBack function</param>
        internal void DFS(BasicBlock<N, D> block, int edgePredToBlock, BasicBlock<N, D> predBlock, System.Collections.BitArray discovered, BasicBlockCallback callback)
        {
            discovered[block.Index] = true;
            if (!callback(block, edgePredToBlock, predBlock, this))
                return;//Means stop
            foreach (var edge in block.SuccessorEdges)
            {
                if (!discovered[SuccessorEdges[edge].Index])
                {
                    DFS(SuccessorEdges[edge], edge, block, discovered, callback);
                }
            }
        }

        /// <summary>
        /// DFS Depth First Search implementation
        /// </summary>
        /// <param name="rootBlock">The root block.</param>
        /// <param name="callback">CallBack function</param>
        public void DFS(BasicBlock<N, D> rootBlock, BasicBlockCallback callback)
        {
            System.Collections.BitArray discovered = new System.Collections.BitArray(AllBlocks.Count);
            DFS(rootBlock, -1, null, discovered, callback);
        }

        /// <summary>
        /// DFS Depth First Search implementation.
        /// </summary>
        /// <param name="callback">CallBack function</param>
        public void DFS(BasicBlockCallback callback)
        {
            foreach(var root in RootBlocks)
            { 
                DFS(root, callback);
            }
        }

        /// <summary>
        /// Iterative version of DFS Depth First Search implementation
        /// </summary>
        /// <param name="callback">CallBack function</param>
        public void DFSIterative(BasicBlockCallback callback)
        {
            System.Collections.BitArray discovered = new System.Collections.BitArray(AllBlocks.Count);
            foreach (var root in RootBlocks)
            {
                Stack<Tuple<BasicBlock<N, D>, int, BasicBlock<N, D>>> stack = new Stack<Tuple<BasicBlock<N, D>, int, BasicBlock<N, D>>>();
                stack.Push(new Tuple<BasicBlock<N, D>, int, BasicBlock<N, D>>(root, -1, null));
                while (stack.Count > 0)
                {
                    Tuple < BasicBlock<N, D>, int, BasicBlock< N, D >> data = stack.Pop();
                    BasicBlock<N, D> block = data.Item1;
                    int predEdge = data.Item2;
                    BasicBlock<N, D> predBlock = data.Item3;
                    if (!discovered[block.Index])
                    {
                        if (!callback(block, predEdge, predBlock, this))
                        {   //Don't traverse edges
                            continue;
                        }
                        foreach (var edge in block.SuccessorEdges)
                        {
                            stack.Push(new Tuple<BasicBlock<N, D>, int, BasicBlock<N, D>>(SuccessorEdges[edge], edge, block));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The DFS callback static method for dumping a CFG
        /// </summary>
        /// <param name="writer">Writer function</param>
        /// <param name="block"></param>
        /// <param name="edge"></param>
        /// <param name="adjacentBlock"></param>
        /// <param name="cfg"></param>
        /// <returns>true</returns>
        private bool DumpCallback(Func<string, bool> writer, BasicBlock<N, D> block, int edge, BasicBlock<N, D> adjacentBlock, ControlFlowGraph<N, D> cfg)
        {
            writer("BLOCK["+block.Index + "] -> {");
            string sep = "";
            //Display Successor block index.
            foreach (int i in block.SuccessorEdges)
            {
                writer(sep);
                writer(this.SuccessorEdges[i].Index.ToString());
                sep = string.Intern(",");
            }
            writer("}");
            writer(System.Environment.NewLine);
            return true;
        }

        /// <summary>
        /// Dump the CFG using the System Out console.
        /// </summary>
        public void Dump()
        {
            Dump(System.Console.Out);
        }

        /// <summary>
        /// Dump the Cfg using the given writer
        /// </summary>
        /// <param name="writer">The writer</param>
        public void Dump(TextWriter writer)
        {
            DFS((b, e, a, c) => DumpCallback((s) =>
            {
                writer.Write(s);
                return true;
            }, b, e, a, c));
        }

        /// <summary>
        /// Dump the the CFG using the connect debugger output.
        /// </summary>
        public void DebugDump()
        {
            DFS((b, e, a, c) => DumpCallback((s) =>
            {
                System.Diagnostics.Trace.Write(s);
                return true;
            }, b, e, a, c));
        }
    }
}
