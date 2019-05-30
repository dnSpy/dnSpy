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

using System;

namespace dnSpy.Contracts.Disassembly {
	/// <summary>
	/// Resolves symbols
	/// </summary>
	public interface ISymbolResolver {
		/// <summary>
		/// Tries to get symbols
		/// </summary>
		/// <param name="addresses">Addresses</param>
		/// <param name="result">Elements that were resolved get updated, the other elements aren't touched.
		/// It has the same number of elements as <paramref name="addresses"/></param>
		void Resolve(ulong[] addresses, SymbolResolverResult[] result);
	}

	/// <summary>
	/// Symbol kind
	/// </summary>
	public enum SymbolKind {
		/// <summary>
		/// Unknown kind
		/// </summary>
		Unknown,

		/// <summary>
		/// Code label
		/// </summary>
		Label,

		/// <summary>
		/// Function
		/// </summary>
		Function,

		/// <summary>
		/// Data
		/// </summary>
		Data,
	}

	/// <summary>
	/// Symbol resolver result
	/// </summary>
	public readonly struct SymbolResolverResult {
		/// <summary>
		/// Checks if this is the default instance
		/// </summary>
		public bool IsDefault => Symbol is null;

		/// <summary>
		/// Symbol kind
		/// </summary>
		public SymbolKind Kind { get; }

		/// <summary>
		/// Symbol name
		/// </summary>
		public string Symbol { get; }

		/// <summary>
		/// Address of the symbol, usually identical to the address passed to <see cref="ISymbolResolver.Resolve(ulong[], SymbolResolverResult[])"/>
		/// </summary>
		public ulong Address { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Symbol kind</param>
		/// <param name="symbol">Symbol name</param>
		/// <param name="address">Address of symbol, usually the same as the address passed to <see cref="ISymbolResolver.Resolve(ulong[], SymbolResolverResult[])"/></param>
		public SymbolResolverResult(SymbolKind kind, string symbol, ulong address) {
			Kind = kind;
			Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
			Address = address;
		}
	}
}
