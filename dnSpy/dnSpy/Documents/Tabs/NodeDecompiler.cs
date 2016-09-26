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

		public void Decompile(IDocumentTreeNodeData node) {
			var nodeType = GetNodeType(node);
			switch (nodeType) {
			case NodeType.Unknown:
				DecompileUnknown(node);
				break;

			case NodeType.Assembly:
				decompiler.Decompile(((IAssemblyDocumentNode)node).Document.AssemblyDef, output, decompilationContext);
				break;

			case NodeType.Module:
				decompiler.Decompile(((IModuleDocumentNode)node).Document.ModuleDef, output, decompilationContext);
				break;

			case NodeType.Type:
				decompiler.Decompile(((ITypeNode)node).TypeDef, output, decompilationContext);
				break;

			case NodeType.Method:
				decompiler.Decompile(((IMethodNode)node).MethodDef, output, decompilationContext);
				break;

			case NodeType.Field:
				decompiler.Decompile(((IFieldNode)node).FieldDef, output, decompilationContext);
				break;

			case NodeType.Property:
				decompiler.Decompile(((IPropertyNode)node).PropertyDef, output, decompilationContext);
				break;

			case NodeType.Event:
				decompiler.Decompile(((IEventNode)node).EventDef, output, decompilationContext);
				break;

			case NodeType.AssemblyRef:
				Decompile((IAssemblyReferenceNode)node);
				break;

			case NodeType.BaseTypeFolder:
				Decompile((IBaseTypeFolderNode)node);
				break;

			case NodeType.BaseType:
				Decompile((IBaseTypeNode)node);
				break;

			case NodeType.DerivedType:
				Decompile((IDerivedTypeNode)node);
				break;

			case NodeType.DerivedTypesFolder:
				Decompile((IDerivedTypesFolderNode)node);
				break;

			case NodeType.ModuleRef:
				Decompile((IModuleReferenceNode)node);
				break;

			case NodeType.Namespace:
				Decompile((INamespaceNode)node);
				break;

			case NodeType.PEFile:
				Decompile((IPEDocumentNode)node);
				break;

			case NodeType.ReferencesFolder:
				Decompile((IReferencesFolderNode)node);
				break;

			case NodeType.ResourcesFolder:
				Decompile((IResourcesFolderNode)node);
				break;

			case NodeType.Resource:
				Decompile((IResourceNode)node);
				break;

			case NodeType.ResourceElement:
				Decompile((IResourceElementNode)node);
				break;

			case NodeType.ResourceElementSet:
				Decompile((IResourceElementSetNode)node);
				break;

			case NodeType.UnknownFile:
				Decompile((IUnknownDocumentNode)node);
				break;

			case NodeType.Message:
				Decompile((IMessageNode)node);
				break;

			default:
				Debug.Fail(string.Format("Unknown NodeType: {0}", nodeType));
				goto case NodeType.Unknown;
			}
		}

		IDocumentTreeNodeData[] GetChildren(IDocumentTreeNodeData node) {
			var n = node;
			return (IDocumentTreeNodeData[])execInThread(() => {
				n.TreeNode.EnsureChildrenLoaded();
				return n.TreeNode.DataChildren.OfType<IDocumentTreeNodeData>().ToArray();
			});
		}

		void DecompileUnknown(IDocumentTreeNodeData node) {
			var decompileSelf = node as IDecompileSelf;
			if (decompileSelf != null && decompileNodeContext != null) {
				if (decompileSelf.Decompile(decompileNodeContext))
					return;
			}
			decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.ToString(decompiler)));
		}

		void Decompile(IAssemblyReferenceNode node) => decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.AssemblyRef.ToString()));

		void Decompile(IBaseTypeFolderNode node) {
			foreach (var child in GetChildren(node).OfType<IBaseTypeNode>())
				Decompile(child);
		}

		void Decompile(IBaseTypeNode node) => decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.TypeDefOrRef.ReflectionFullName));
		void Decompile(IDerivedTypeNode node) => decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.TypeDef.ReflectionFullName));

		void Decompile(IDerivedTypesFolderNode node) {
			foreach (var child in GetChildren(node).OfType<IDerivedTypeNode>())
				Decompile(child);
		}

		void Decompile(IModuleReferenceNode node) => decompiler.WriteCommentLine(output, NameUtilities.CleanName(node.ModuleRef.ToString()));

		void Decompile(INamespaceNode node) {
			var children = GetChildren(node).OfType<ITypeNode>().Select(a => a.TypeDef).ToArray();
			decompiler.DecompileNamespace(node.Name, children, output, decompilationContext);
		}

		void Decompile(IPEDocumentNode node) => decompiler.WriteCommentLine(output, node.Document.Filename);

		void Decompile(IReferencesFolderNode node) {
			foreach (var child in GetChildren(node)) {
				if (child is IAssemblyReferenceNode)
					Decompile((IAssemblyReferenceNode)child);
				else if (child is IModuleReferenceNode)
					Decompile((IModuleReferenceNode)child);
				else
					DecompileUnknown(child);
			}
		}

		void Decompile(IResourcesFolderNode node) {
			foreach (var child in GetChildren(node)) {
				if (child is IResourceNode)
					Decompile((IResourceNode)child);
				else
					DecompileUnknown(child);
			}
		}

		void Decompile(IResourceNode node) {
			if (node is IResourceElementSetNode)
				Decompile((IResourceElementSetNode)node);
			else
				node.WriteShort(output, decompiler, decompiler.Settings.GetBoolean(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID));
		}

		void Decompile(IResourceElementNode node) =>
			node.WriteShort(output, decompiler, decompiler.Settings.GetBoolean(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID));

		void Decompile(IResourceElementSetNode node) {
			node.WriteShort(output, decompiler, decompiler.Settings.GetBoolean(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID));

			foreach (var child in GetChildren(node)) {
				if (child is IResourceElementNode)
					Decompile((IResourceElementNode)child);
				else
					DecompileUnknown(child);
			}
		}

		void Decompile(IUnknownDocumentNode node) => decompiler.WriteCommentLine(output, node.Document.Filename);
		void Decompile(IMessageNode node) => decompiler.WriteCommentLine(output, node.Message);

		static NodeType GetNodeType(IDocumentTreeNodeData node) {
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

		static NodeType GetNodeTypeSlow(IDocumentTreeNodeData node) {
			if (node is IAssemblyDocumentNode)
				return NodeType.Assembly;
			if (node is IModuleDocumentNode)
				return NodeType.Module;
			if (node is ITypeNode)
				return NodeType.Type;
			if (node is IMethodNode)
				return NodeType.Method;
			if (node is IFieldNode)
				return NodeType.Field;
			if (node is IPropertyNode)
				return NodeType.Property;
			if (node is IEventNode)
				return NodeType.Event;
			if (node is IAssemblyReferenceNode)
				return NodeType.AssemblyRef;
			if (node is IBaseTypeFolderNode)
				return NodeType.BaseTypeFolder;
			if (node is IBaseTypeNode)
				return NodeType.BaseType;
			if (node is IDerivedTypeNode)
				return NodeType.DerivedType;
			if (node is IDerivedTypesFolderNode)
				return NodeType.DerivedTypesFolder;
			if (node is IModuleReferenceNode)
				return NodeType.ModuleRef;
			if (node is INamespaceNode)
				return NodeType.Namespace;
			if (node is IPEDocumentNode)
				return NodeType.PEFile;
			if (node is IReferencesFolderNode)
				return NodeType.ReferencesFolder;
			if (node is IResourcesFolderNode)
				return NodeType.ResourcesFolder;
			if (node is IResourceNode)
				return NodeType.Resource;
			if (node is IResourceElementNode)
				return NodeType.ResourceElement;
			if (node is IResourceElementSetNode)
				return NodeType.ResourceElementSet;
			if (node is IUnknownDocumentNode)
				return NodeType.UnknownFile;
			if (node is IMessageNode)
				return NodeType.Message;

			return NodeType.Unknown;
		}
	}
}
