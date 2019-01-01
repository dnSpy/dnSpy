/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Disassembly;

namespace dnSpy.Disassembly.Viewer {
	sealed class CachedSymbolResolver {
		readonly Dictionary<ulong, (SymbolResolverResult result, bool fakeSymbol)> symbols;

		public CachedSymbolResolver() => symbols = new Dictionary<ulong, (SymbolResolverResult result, bool fakeSymbol)>();

		public void AddSymbol(ulong address, SymbolResolverResult symbol, bool fakeSymbol) => AddSymbolCore(address, symbol, fakeSymbol);

		void AddSymbolCore(ulong address, SymbolResolverResult symbol, bool fakeSymbol) =>
			symbols[address] = (new SymbolResolverResult(symbol.Kind, SymbolResolverUtils.FixSymbol(symbol.Symbol), symbol.Address), fakeSymbol);

		public void AddSymbols(ulong[] addresses, SymbolResolverResult[] symbolResolverResults, bool fakeSymbol) {
			Debug.Assert(addresses.Length == symbolResolverResults.Length);
			for (int i = 0; i < symbolResolverResults.Length; i++) {
				if (!symbolResolverResults[i].IsDefault)
					AddSymbolCore(addresses[i], symbolResolverResults[i], fakeSymbol);
			}
		}

		public bool TryResolve(ulong address, out SymbolResolverResult result, out bool fakeSymbol) {
			if (!symbols.TryGetValue(address, out var info)) {
				result = default;
				fakeSymbol = false;
				return false;
			}
			else {
				result = info.result;
				fakeSymbol = info.fakeSymbol;
				return true;
			}
		}
	}
}
