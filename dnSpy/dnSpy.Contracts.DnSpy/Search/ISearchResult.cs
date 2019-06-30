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
using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView.Resources;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Provides a reference
	/// </summary>
	public interface ISearchResultReferenceProvider {
		/// <summary>
		/// Gets the reference
		/// </summary>
		object? Reference { get; }
	}

	/// <summary>
	/// Search result
	/// </summary>
	interface ISearchResult : ISearchResultReferenceProvider, IComparable<ISearchResult> {
		/// <summary>
		/// <see cref="IDsDocument"/> if it's a non-.NET file. <see cref="AssemblyDef"/>,
		/// <see cref="ModuleDef"/>, <see cref="dnlib.DotNet.AssemblyRef"/>, <see cref="ModuleRef"/>,
		/// <see cref="ResourceNode"/>, <see cref="ResourceElementNode"/>, <see cref="string"/>
		/// (namespace), <see cref="TypeDef"/>, <see cref="MethodDef"/>, <see cref="FieldDef"/>,
		/// <see cref="PropertyDef"/>, <see cref="EventDef"/>.
		/// </summary>
		object Object { get; }

		/// <summary>
		/// Owner file
		/// </summary>
		IDsDocument Document { get; }

		/// <summary>
		/// Refreshes UI fields. Should be called if the classification format map,
		/// <see cref="IDocumentSearcher.SyntaxHighlight"/> or <see cref="IDocumentSearcher.Decompiler"/>
		/// changes.
		/// </summary>
		void RefreshUI();

		/// <summary>
		/// Gets any extra info related to <see cref="Object"/>. <see cref="BodyResult"/> if the
		/// method body was searched.
		/// </summary>
		object? ObjectInfo { get; }
	}
}
