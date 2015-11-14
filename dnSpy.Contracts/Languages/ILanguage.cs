/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using dnlib.DotNet;
using dnSpy.Contracts.Highlighting;

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// A language
	/// </summary>
	public interface ILanguage {
		/// <summary>
		/// Language name shown in UI
		/// </summary>
		string NameUI { get; }

		/// <summary>
		/// Order of language when shown in a UI
		/// </summary>
		double OrderUI { get; }

		/// <summary>
		/// Writes a type name
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="type">Type</param>
		void WriteName(ISyntaxHighlightOutput output, TypeDef type);

		/// <summary>
		/// Writes a property name
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="property">Type</param>
		/// <param name="isIndexer">true if it's an indexer</param>
		void WriteName(ISyntaxHighlightOutput output, PropertyDef property, bool? isIndexer);

		/// <summary>
		/// Writes a type name
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="type">Type</param>
		/// <param name="includeNamespace">true to include namespace</param>
		/// <param name="pd"><see cref="ParamDef"/> or null</param>
		void WriteType(ISyntaxHighlightOutput output, ITypeDefOrRef type, bool includeNamespace, ParamDef pd = null);
	}
}
