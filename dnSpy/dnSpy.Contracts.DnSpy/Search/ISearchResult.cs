/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView.Resources;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Search result
	/// </summary>
	public interface ISearchResult : IComparable<ISearchResult> {
		/// <summary>
		/// <see cref="IDnSpyFile"/> if it's a non-.NET file. <see cref="AssemblyDef"/>,
		/// <see cref="ModuleDef"/>, <see cref="dnlib.DotNet.AssemblyRef"/>, <see cref="ModuleRef"/>,
		/// <see cref="IResourceNode"/>, <see cref="IResourceElementNode"/>, <see cref="string"/>
		/// (namespace), <see cref="TypeDef"/>, <see cref="MethodDef"/>, <see cref="FieldDef"/>,
		/// <see cref="PropertyDef"/>, <see cref="EventDef"/>.
		/// </summary>
		object Object { get; }

		/// <summary>
		/// Owner file
		/// </summary>
		IDnSpyFile DnSpyFile { get; }

		/// <summary>
		/// Refreshes UI fields. Should be called if the theme,
		/// <see cref="IFileSearcher.SyntaxHighlight"/> or <see cref="IFileSearcher.Decompiler"/>
		/// changes.
		/// </summary>
		void RefreshUI();

		/// <summary>
		/// Gets the reference
		/// </summary>
		object Reference { get; }

		/// <summary>
		/// Gets any extra info related to <see cref="Object"/>. <see cref="BodyResult"/> if the
		/// method body was searched.
		/// </summary>
		object ObjectInfo { get; }
	}
}
