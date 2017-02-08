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

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Document treeview
	/// </summary>
	public interface IDocumentTreeView : IDocumentTreeNodeProvider {
		/// <summary>
		/// Gets the <see cref="IDsDocumentService"/> instance
		/// </summary>
		IDsDocumentService DocumentService { get; }

		/// <summary>
		/// Gets the <see cref="ITreeView"/> instance
		/// </summary>
		ITreeView TreeView { get; }

		/// <summary>
		/// Gets the <see cref="IDotNetImageService"/> instance
		/// </summary>
		IDotNetImageService DotNetImageService { get; }

		/// <summary>
		/// Gets the <see cref="IWpfCommands"/> instance
		/// </summary>
		IWpfCommands WpfCommands { get; }

		/// <summary>
		/// Raised when the collection gets changed
		/// </summary>
		event EventHandler<NotifyDocumentTreeViewCollectionChangedEventArgs> CollectionChanged;

		/// <summary>
		/// Raised when the node's text has changed
		/// </summary>
		event EventHandler<EventArgs> NodesTextChanged;

		/// <summary>
		/// Raised when a node gets activated (eg. double clicked)
		/// </summary>
		event EventHandler<DocumentTreeNodeActivatedEventArgs> NodeActivated;

		/// <summary>
		/// Raised when selection has changed
		/// </summary>
		event EventHandler<TreeViewSelectionChangedEventArgs> SelectionChanged;

		/// <summary>
		/// Should only be called by the node that gets activated. Returns true if someone handled it.
		/// </summary>
		/// <param name="node">The activated node (should be the caller)</param>
		/// <returns></returns>
		bool RaiseNodeActivated(DocumentTreeNodeData node);

		/// <summary>
		/// Creates a new <see cref="DsDocumentNode"/> instance. This will internally call all
		/// <see cref="IDsDocumentNodeProvider"/>s it can find.
		/// </summary>
		/// <param name="owner">Owner node or null if owner is the root node</param>
		/// <param name="document">New document</param>
		/// <returns></returns>
		DsDocumentNode CreateNode(DsDocumentNode owner, IDsDocument document);

		/// <summary>
		/// Removes <paramref name="nodes"/>. They must be top nodes (eg. <see cref="AssemblyDocumentNode"/>s)
		/// </summary>
		/// <param name="nodes">Nodes</param>
		void Remove(IEnumerable<DsDocumentNode> nodes);

		/// <summary>
		/// Returns a node or null if none could be found
		/// </summary>
		/// <param name="ref">Reference, eg. a <see cref="IMemberRef"/></param>
		/// <returns></returns>
		DocumentTreeNodeData FindNode(object @ref);

		/// <summary>
		/// Returns a <see cref="DsDocumentNode"/> node or null if none could be found
		/// </summary>
		/// <param name="document">Document</param>
		/// <returns></returns>
		DsDocumentNode FindNode(IDsDocument document);

		/// <summary>
		/// Returns a <see cref="AssemblyDocumentNode"/> node or null if none could be found
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <returns></returns>
		AssemblyDocumentNode FindNode(AssemblyDef assembly);

		/// <summary>
		/// Returns a <see cref="ModuleDocumentNode"/> node or null if none could be found
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		ModuleDocumentNode FindNode(ModuleDef module);

		/// <summary>
		/// Returns a <see cref="TypeNode"/> node or null if none could be found
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		TypeNode FindNode(TypeDef type);

		/// <summary>
		/// Returns a <see cref="MethodNode"/> node or null if none could be found
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		MethodNode FindNode(MethodDef method);

		/// <summary>
		/// Returns a <see cref="FieldNode"/> node or null if none could be found
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		FieldNode FindNode(FieldDef field);

		/// <summary>
		/// Returns a <see cref="PropertyNode"/> node or null if none could be found
		/// </summary>
		/// <param name="property">Property</param>
		/// <returns></returns>
		PropertyNode FindNode(PropertyDef property);

		/// <summary>
		/// Returns a <see cref="EventNode"/> node or null if none could be found
		/// </summary>
		/// <param name="event">Event</param>
		/// <returns></returns>
		EventNode FindNode(EventDef @event);

		/// <summary>
		/// Returns a <see cref="NamespaceNode"/> node or null if none could be found
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="namespace">Namespace</param>
		/// <returns></returns>
		NamespaceNode FindNamespaceNode(IDsDocument module, string @namespace);

		/// <summary>
		/// Gets the <see cref="IDocumentTreeNodeGroups"/> instance
		/// </summary>
		IDocumentTreeNodeGroups DocumentTreeNodeGroups { get; }

		/// <summary>
		/// Gets all <see cref="ModuleDocumentNode"/>s
		/// </summary>
		/// <returns></returns>
		IEnumerable<ModuleDocumentNode> GetAllModuleNodes();

		/// <summary>
		/// Gets all created <see cref="DsDocumentNode"/>s
		/// </summary>
		/// <returns></returns>
		IEnumerable<DsDocumentNode> GetAllCreatedDocumentNodes();

		/// <summary>
		/// Adds <paramref name="documentNode"/> to the list
		/// </summary>
		/// <param name="documentNode">Node</param>
		/// <param name="index">Index or -1</param>
		void AddNode(DsDocumentNode documentNode, int index);

		/// <summary>
		/// Sets decompiler
		/// </summary>
		/// <param name="decompiler">Decompiler</param>
		void SetDecompiler(IDecompiler decompiler);

		/// <summary>
		/// Disposes this instance
		/// </summary>
		void Dispose();

		/// <summary>
		/// Sorts all documents
		/// </summary>
		void SortTopNodes();

		/// <summary>
		/// true if <see cref="SortTopNodes()"/> can be called
		/// </summary>
		bool CanSortTopNodes { get; }
	}
}
