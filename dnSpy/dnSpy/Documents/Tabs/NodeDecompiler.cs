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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Text;

namespace dnSpy.Documents.Tabs {
	enum NodeType {
		Unknown,
		Assembly,
		Module,
		Type,
		Method,
		Field,
		Property,
		Event,
		AssemblyRef,
		BaseTypeFolder,
		BaseType,
		DerivedType,
		DerivedTypesFolder,
		ModuleRef,
		Namespace,
		PEFile,
		ReferencesFolder,
		ResourcesFolder,
		Resource,
		ResourceElement,
		ResourceElementSet,
		UnknownFile,
		Message,
	}

	struct NodeDecompiler {
		readonly Func<Func<object>, object> execInThread;
		readonly IDecompilerOutput output;
		readonly IDecompiler decompiler;
		readonly DecompilationContext decompilationContext;
		readonly IDecompileNodeContext decompileNodeContext;

		public NodeDecompiler(Func<Func<object>, object> execInThread, IDecompilerOutput output, IDecompiler decompiler, DecompilationContext decompilationContext, IDecompileNodeContext decompileNodeContext = null) {
			this.execInThread = execInThread;
			this.output = output;
			this.decompiler = decompiler;
			this.decompilationContext = decompilationContext;
			this.decompileNodeContext = decompileNodeContext;
			this.decompileNodeContext.ContentTypeString = decompiler.ContentTypeString;
		}

		static readonly object lockObj = new object();
		static readonly Dictionary<Type, NodeType> toNodeType = new Dictionary<Type, NodeType>();

		public void Decompile(DocumentTreeNodeData node) {
			var nodeType = GetNodeType(node);
			switch (nodeType) {
			case NodeType.Unknown:
				DecompileUnknown(node);
				break;

			case NodeType.Assembly:
				decompiler.Decompile(((AssemblyDocumentNode)node).Document.AssemblyDef, output, decompilationContext);
				break;

			case NodeType.Module:
				decompiler.Decompile(((ModuleDocumentNode)node).Document.ModuleDef, output, decompilationContext);
				break;

			case NodeType.Type:
				decompiler.Decompile(((TypeNode)node).TypeDef, output, decompilationContext);
				break;

			case NodeType.Method:
				decompiler.Decompile(((MethodNode)node).MethodDef, output, decompilationContext);
				break;

			case NodeType.Field:
				decompiler.Decompile(((FieldNode)node).FieldDef, output, decompilationContext);
				break;

			case NodeType.Property:
				decompiler.Decompile(((PropertyNode)node).PropertyDef, output, decompilationContext);
				break;

			case NodeType.Event:
				decompiler.Decompile(((EventNode)node).EventDef, output, decompilationContext);
				break;

			case NodeType.AssemblyRef:
				Decompile((AssemblyReferenceNode)node);
				break;

			case NodeType.BaseTypeFolder:
				Decompile((BaseTypeFolderNode)node);
				break;

			case NodeType.BaseType:
				Decompile((BaseTypeNode)node);
				break;

			case NodeType.DerivedType:
				Decompile((DerivedTypeNode)node);
				break;

			case NodeType.DerivedTypesFolder:
				Decompile((DerivedTypesFolderNode)node);
				break;

			case NodeType.ModuleRef:
				Decompile((ModuleReferenceNode)node);
				break;

			case NodeType.Namespace:
				Decompile((NamespaceNode)node);
				break;

			case NodeType.PEFile:
				Decompile((PEDocumentNode)node);
				break;

			case NodeType.ReferencesFolder:
				Decompile((ReferencesFolderNode)node);
				break;

			case NodeType.ResourcesFolder:
				Decompile((ResourcesFolderNode)node);
				break;

			case NodeType.Resource:
				Decompile((ResourceNode)node);
				break;

			case NodeType.ResourceElement:
				Decompile((ResourceElementNode)node);
				break;

			case NodeType.ResourceElementSet:
				Decompile((ResourceElementSetNode)node);
				break;

			case NodeType.UnknownFile:
				Decompile((UnknownDocumentNode)node);
				break;

			case NodeType.Message:
				Decompile((MessageNode)node);
				break;

			default:
				Debug.Fail(string.Format("Unknown NodeType: {0}", nodeType));
				goto case NodeType.Unknown;
			}
		}

		DocumentTreeNodeData[] GetChildren(DocumentTreeNodeData node) {
			var n = node;
			return (DocumentTreeNodeData[])execInThread(() => {
				n.TreeNode.EnsureChildrenLoaded();
				return n.TreeNode.DataChildren.OfType<DocumentTreeNodeData>().ToArray();
			});
		}

		void DecompileUnknown(DocumentTreeNodeData node) {
			var decompileSelf = node as IDecompileSelf;
			if (decompileSelf != null && decompileNodeContext != null) {
				if (decompileSelf.Decompile(decompileNodeContext))
					return;
			}
			decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.ToString(decompiler)));
		}

		void Decompile(AssemblyReferenceNode node) => decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.AssemblyRef.ToString()));

		void Decompile(BaseTypeFolderNode node) {
			foreach (var child in GetChildren(node).OfType<BaseTypeNode>())
				Decompile(child);
		}

		void Decompile(BaseTypeNode node) => decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.TypeDefOrRef.ReflectionFullName));
		void Decompile(DerivedTypeNode node) => decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.TypeDef.ReflectionFullName));

		void Decompile(DerivedTypesFolderNode node) {
			foreach (var child in GetChildren(node).OfType<DerivedTypeNode>())
				Decompile(child);
		}

		void Decompile(ModuleReferenceNode node) => decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.ModuleRef.ToString()));

		void Decompile(NamespaceNode node) {
			var children = GetChildren(node).OfType<TypeNode>().Select(a => a.TypeDef).ToArray();
			decompiler.DecompileNamespace(node.Name, children, output, decompilationContext);
		}

		void Decompile(PEDocumentNode node) => decompiler.WriteCommentLine(output, node.Document.Filename);

		void Decompile(ReferencesFolderNode node) {
			foreach (var child in GetChildren(node)) {
				if (child is AssemblyReferenceNode)
					Decompile((AssemblyReferenceNode)child);
				else if (child is ModuleReferenceNode)
					Decompile((ModuleReferenceNode)child);
				else
					DecompileUnknown(child);
			}
		}

		void Decompile(ResourcesFolderNode node) {
			foreach (var child in GetChildren(node)) {
				if (child is ResourceNode)
					Decompile((ResourceNode)child);
				else
					DecompileUnknown(child);
			}
		}

		void Decompile(ResourceNode node) {
			if (node is ResourceElementSetNode)
				Decompile((ResourceElementSetNode)node);
			else
				node.WriteShort(output, decompiler, decompiler.Settings.GetBoolean(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID));
		}

		void Decompile(ResourceElementNode node) =>
			node.WriteShort(output, decompiler, decompiler.Settings.GetBoolean(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID));

		void Decompile(ResourceElementSetNode node) {
			node.WriteShort(output, decompiler, decompiler.Settings.GetBoolean(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID));

			foreach (var child in GetChildren(node)) {
				if (child is ResourceElementNode)
					Decompile((ResourceElementNode)child);
				else
					DecompileUnknown(child);
			}
		}

		void Decompile(UnknownDocumentNode node) => decompiler.WriteCommentLine(output, node.Document.Filename);
		void Decompile(MessageNode node) => decompiler.WriteCommentLine(output, node.Message);

		static NodeType GetNodeType(DocumentTreeNodeData node) {
			NodeType nodeType;
			var type = node.GetType();
			lock (lockObj) {
				if (toNodeType.TryGetValue(type, out nodeType))
					return nodeType;

				nodeType = GetNodeTypeSlow(node);
				toNodeType.Add(type, nodeType);
			}
			return nodeType;
		}

		static NodeType GetNodeTypeSlow(DocumentTreeNodeData node) {
			if (node is AssemblyDocumentNode)
				return NodeType.Assembly;
			if (node is ModuleDocumentNode)
				return NodeType.Module;
			if (node is TypeNode)
				return NodeType.Type;
			if (node is MethodNode)
				return NodeType.Method;
			if (node is FieldNode)
				return NodeType.Field;
			if (node is PropertyNode)
				return NodeType.Property;
			if (node is EventNode)
				return NodeType.Event;
			if (node is AssemblyReferenceNode)
				return NodeType.AssemblyRef;
			if (node is BaseTypeFolderNode)
				return NodeType.BaseTypeFolder;
			if (node is BaseTypeNode)
				return NodeType.BaseType;
			if (node is DerivedTypeNode)
				return NodeType.DerivedType;
			if (node is DerivedTypesFolderNode)
				return NodeType.DerivedTypesFolder;
			if (node is ModuleReferenceNode)
				return NodeType.ModuleRef;
			if (node is NamespaceNode)
				return NodeType.Namespace;
			if (node is PEDocumentNode)
				return NodeType.PEFile;
			if (node is ReferencesFolderNode)
				return NodeType.ReferencesFolder;
			if (node is ResourcesFolderNode)
				return NodeType.ResourcesFolder;
			if (node is ResourceNode)
				return NodeType.Resource;
			if (node is ResourceElementNode)
				return NodeType.ResourceElement;
			if (node is ResourceElementSetNode)
				return NodeType.ResourceElementSet;
			if (node is UnknownDocumentNode)
				return NodeType.UnknownFile;
			if (node is MessageNode)
				return NodeType.Message;

			return NodeType.Unknown;
		}
	}
}
