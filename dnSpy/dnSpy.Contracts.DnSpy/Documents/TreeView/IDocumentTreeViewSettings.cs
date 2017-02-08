/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using System.ComponentModel;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// <see cref="IDocumentTreeView"/> settings
	/// </summary>
	public interface IDocumentTreeViewSettings : INotifyPropertyChanged {
		/// <summary>
		/// true to syntax highlight the treeview
		/// </summary>
		bool SyntaxHighlight { get; }

		/// <summary>
		/// true causes single clicks to expand children, false requires a double click
		/// </summary>
		bool SingleClickExpandsTreeViewChildren { get; }

		/// <summary>
		/// true to show assembly version when printing assembly nodes
		/// </summary>
		bool ShowAssemblyVersion { get; }

		/// <summary>
		/// true to show assembly public key token when printing assembly nodes
		/// </summary>
		bool ShowAssemblyPublicKeyToken { get; }

		/// <summary>
		/// true to show tokens
		/// </summary>
		bool ShowToken { get; }

		/// <summary>
		/// true to deserialize resources
		/// </summary>
		bool DeserializeResources { get; }

		/// <summary>Gets 0th member</summary>
		MemberKind MemberKind0 { get; }

		/// <summary>Gets 1st member</summary>
		MemberKind MemberKind1 { get; }

		/// <summary>Gets 2nd member</summary>
		MemberKind MemberKind2 { get; }

		/// <summary>Gets 3rd member</summary>
		MemberKind MemberKind3 { get; }

		/// <summary>Gets 4th member</summary>
		MemberKind MemberKind4 { get; }

		/// <summary>How to filter dragged items</summary>
		DocumentFilterType FilterDraggedItems { get; }
	}
}
