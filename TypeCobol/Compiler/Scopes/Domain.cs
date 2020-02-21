﻿using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TypeCobol.Compiler.Symbols;

namespace TypeCobol.Compiler.Scopes
{
    /// <summary>
    /// A domain is a set of symbols.
    /// </summary>
    /// <typeparam name="TSymbol">Type of symbols stored in this domain.</typeparam>
    public class Domain<TSymbol> : IEnumerable<TSymbol>
        where TSymbol : Symbol
    {
        /// <summary>
        /// Represents an entry in a domain. Regroups some symbols which have the same name.
        /// </summary>
        public class Entry : IEnumerable<TSymbol>
        {
            /*
             * Basic optimization, an entry is initially using a single field to store its main symbol.
             * As soon as a second symbol is added to it, we switch to a List implementation.
             */

            public string Name { get; }
            private TSymbol _symbol;
            private List<TSymbol> _symbols;

            /// <summary>
            /// Creates an empty Entry bound to a specific symbol name.
            /// </summary>
            /// <param name="name">Expected name of all symbols in this entry.</param>
            public Entry([NotNull] string name)
            {
                System.Diagnostics.Debug.Assert(name != null);
                Name = name;
                _symbol = null;
                _symbols = null;
            }

            /// <summary>
            /// Creates a unique Entry.
            /// </summary>
            /// <param name="symbol">Symbol associated to this entry.</param>
            public Entry([NotNull] TSymbol symbol)
            {
                System.Diagnostics.Debug.Assert(symbol != null);
                System.Diagnostics.Debug.Assert(symbol.Name != null);
                Name = symbol.Name;
                _symbol = symbol;
                _symbols = null;
            }

            /// <summary>
            /// Total count of symbols in this entry.
            /// </summary>
            public int Count => _symbols?.Count ?? (_symbol != null ? 1 : 0);

            /// <summary>
            /// Convenience property to retrieve the first symbol of this entry.
            /// Returns null if this entry is empty.
            /// </summary>
            public TSymbol Symbol => _symbols?.Count > 0 ? _symbols[0] : _symbol;

            public IEnumerator<TSymbol> GetEnumerator()
            {
                if (_symbols != null)
                {
                    foreach (var symbol in _symbols)
                    {
                        yield return symbol;
                    }
                }
                else if (_symbol != null)
                {
                    yield return _symbol;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// Adds a symbol to the entry.
            /// </summary>
            /// <param name="symbol">Symbol to add.</param>
            public void Add([NotNull] TSymbol symbol)
            {
                System.Diagnostics.Debug.Assert(symbol != null);
                if (!Name.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (_symbols != null)
                {
                    //We already have a List implementation, add to it
                    _symbols.Add(symbol);
                }
                else
                {
                    if (_symbol == null)
                    {
                        //This is an empty entry, replace the main symbol with the new one
                        _symbol = symbol;
                    }
                    else
                    {
                        //Switch to List implementation to store the second symbol, original main symbol stays at first position
                        _symbols = new List<TSymbol>()
                                   {
                                       _symbol,
                                       symbol
                                   };
                        _symbol = null;
                    }
                }
            }

            /// <summary>
            /// Removes a symbol from this entry.
            /// </summary>
            /// <param name="symbol">Symbol to remove.</param>
            public void Remove([NotNull] TSymbol symbol)
            {
                System.Diagnostics.Debug.Assert(symbol != null);
                if (!Name.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (_symbols != null)
                {
                    //We already have a List implementation, remove from it
                    _symbols.Remove(symbol);
                }
                else
                {
                    if (_symbol == symbol)
                    {
                        //Entry is unique, remove the main symbol if it corresponds (otherwise it means that the entry does not contain the supplied symbol)
                        _symbol = null;
                    }
                }
            }
        }

        private readonly Dictionary<string, Entry> _symbols;
        private readonly List<TSymbol> _symbolsInOrder;

        /// <summary>
        /// Creates an empty domain.
        /// </summary>
        public Domain()
        {
            _symbols = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            _symbolsInOrder = new List<TSymbol>();
        }

        /// <summary>
        /// Creates a domain from an existing one.
        /// </summary>
        /// <param name="other">Other domain to copy symbols from.</param>
        public Domain([NotNull] Domain<TSymbol> other)
        {
            System.Diagnostics.Debug.Assert(other != null);
            _symbols = new Dictionary<string, Entry>(other._symbols, StringComparer.OrdinalIgnoreCase);
            _symbolsInOrder = new List<TSymbol>(other._symbolsInOrder);
        }

        public IEnumerator<TSymbol> GetEnumerator()
        {
            return _symbolsInOrder.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Searches an entry in this domain according to a name.
        /// </summary>
        /// <param name="name">Name of searched Symbol.</param>
        /// <param name="entry">Result of the search. If an entry has been found in this domain it is not empty.</param>
        /// <returns>True if an entry has been found, False otherwise.</returns>
        public bool TryGetValue([NotNull] string name, out Entry entry)
        {
            System.Diagnostics.Debug.Assert(name != null);
            return _symbols.TryGetValue(name, out entry);
        }

        /// <summary>
        /// Adds a Symbol to this domain and returns the corresponding entry.
        /// </summary>
        /// <param name="symbol">Symbol to add.</param>
        /// <returns>
        /// The entry associated with the symbol, it can be either a new entry or an existing
        /// one depending whether the symbol's name already exists in the domain or not.
        /// </returns>
        public Entry Add([NotNull] TSymbol symbol)
        {
            System.Diagnostics.Debug.Assert(symbol != null);
            string key = symbol.Name;
            if (_symbols.TryGetValue(key, out var entry))
            {
                //entry already exists, add inside it.
                entry.Add(symbol);
            }
            else
            {
                //create a new entry for this symbol.
                entry = new Entry(symbol);
                _symbols.Add(key, entry);
            }

            _symbolsInOrder.Add(symbol);
            return entry;
        }

        /// <summary>
        /// Removes a Symbol from this domain.
        /// </summary>
        /// <param name="symbol">Symbol to remove.</param>
        public void Remove([NotNull] TSymbol symbol)
        {
            System.Diagnostics.Debug.Assert(symbol != null);
            string key = symbol.Name;
            if (_symbols.TryGetValue(key, out var entry))
            {
                entry.Remove(symbol);
                if (entry.Count == 0)
                {
                    //Entry is no longer valid, remove it from the dictionary.
                    _symbols.Remove(key);
                }

                _symbolsInOrder.Remove(symbol);
            }
        }

        /// <summary>
        /// The count of symbols in this domain
        /// </summary>
        public int Count => _symbols.Count;

        /// <summary>
        /// Indexed accessor
        /// </summary>
        /// <param name="i">Index of the symbol to get</param>
        /// <returns>The Symbol at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">The given index is out of range</exception>
        public TSymbol this[int i]
        {
            get
            {
                if (i < 0 || i >= _symbolsInOrder.Count)
                    throw new IndexOutOfRangeException();
                return _symbolsInOrder[i];
            }
        }
    }
}
