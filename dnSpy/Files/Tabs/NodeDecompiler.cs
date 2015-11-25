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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using ICSharpCode.Decompiler;

namespace dnSpy.Files.Tabs {
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
		PEFileNode,
		References,
		UnknownFileNode,
		MessageNode,
	}

	struct NodeDecompiler {
		readonly Func<Func<object>, object> execInThread;
		readonly ITextOutput output;
		readonly ILanguage language;
		readonly DecompilationOptions decompilationOptions;
		readonly IFileTreeNodeData node;

		public NodeDecompiler(Func<Func<object>, object> execInThread, ITextOutput output, ILanguage language, DecompilationOptions decompilationOptions, IFileTreeNodeData node) {
			this.execInThread = execInThread;
			this.output = output;
			this.language = language;
			this.decompilationOptions = decompilationOptions;
			this.node = node;
		}

		static readonly object lockObj = new object();
		static readonly Dictionary<Type, NodeType> toNodeType = new Dictionary<Type, NodeType>();

		public void Decompile() {
			var nodeType = GetNodeType(node);
			switch (nodeType) {
			case NodeType.Unknown:
				language.WriteCommentLine(output, node.ToString());
				break;

			case NodeType.Assembly:
				language.DecompileAssembly(((IAssemblyFileNode)node).DnSpyFile, output, decompilationOptions, decompilationOptions.ProjectOptions.Directory != null ? DecompileAssemblyFlags.AssemblyAndModule : DecompileAssemblyFlags.Assembly);
				break;

			case NodeType.Module:
				language.DecompileAssembly(((IModuleFileNode)node).DnSpyFile, output, decompilationOptions, decompilationOptions.ProjectOptions.Directory != null ? DecompileAssemblyFlags.AssemblyAndModule : DecompileAssemblyFlags.Module);
				break;

			case NodeType.Type:
				language.Decompile(((ITypeNode)node).TypeDef, output, decompilationOptions);
				break;

			case NodeType.Method:
				language.Decompile(((IMethodNode)node).MethodDef, output, decompilationOptions);
				break;

			case NodeType.Field:
				language.Decompile(((IFieldNode)node).FieldDef, output, decompilationOptions);
				break;

			case NodeType.Property:
				language.Decompile(((IPropertyNode)node).PropertyDef, output, decompilationOptions);
				break;

			case NodeType.Event:
				language.Decompile(((IEventNode)node).EventDef, output, decompilationOptions);
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

			case NodeType.PEFileNode:
				Decompile((IPEFileNode)node);
				break;

			case NodeType.References:
				Decompile((IReferencesNode)node);
				break;

			case NodeType.UnknownFileNode:
				Decompile((IUnknownFileNode)node);
				break;

			case NodeType.MessageNode:
				Decompile((IMessageNode)node);
				break;

			default:
				Debug.Fail(string.Format("Unknown NodeType: {0}", nodeType));
				goto case NodeType.Unknown;
			}
		}

		IFileTreeNodeData[] GetChildren() {
			var n = node;
			return (IFileTreeNodeData[])execInThread(() => {
				n.TreeNode.EnsureChildrenLoaded();
				return n.TreeNode.DataChildren.OfType<IFileTreeNodeData>().ToArray();
			});
		}

		void Decompile(IAssemblyReferenceNode node) {
			language.WriteCommentLine(output, node.AssemblyRef.ToString());
		}

		void Decompile(IBaseTypeFolderNode node) {
			foreach (var child in GetChildren().OfType<IBaseTypeNode>())
				Decompile(child);
		}

		void Decompile(IBaseTypeNode node) {
			language.WriteCommentLine(output, node.TypeDefOrRef.ReflectionFullName);
		}

		void Decompile(IDerivedTypeNode node) {
			language.WriteCommentLine(output, node.TypeDef.ReflectionFullName);
		}

		void Decompile(IDerivedTypesFolderNode node) {
			foreach (var child in GetChildren().OfType<IDerivedTypeNode>())
				Decompile(child);
		}

		void Decompile(IModuleReferenceNode node) {
			language.WriteCommentLine(output, node.ModuleRef.ToString());
		}

		void Decompile(INamespaceNode node) {
			language.WriteCommentLine(output, node.Name);
		}

		void Decompile(IPEFileNode node) {
			language.WriteCommentLine(output, node.DnSpyFile.Filename);
		}

		void Decompile(IReferencesNode node) {
			foreach (var child in GetChildren()) {
				if (child is IAssemblyReferenceNode)
					Decompile((IAssemblyReferenceNode)child);
				else if (child is IModuleReferenceNode)
					Decompile((IModuleReferenceNode)child);
			}
		}

		void Decompile(IUnknownFileNode node) {
			language.WriteCommentLine(output, node.DnSpyFile.Filename);
		}

		void Decompile(IMessageNode node) {
			language.WriteCommentLine(output, node.Message);
		}

		static NodeType GetNodeType(IFileTreeNodeData node) {
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

		static NodeType GetNodeTypeSlow(IFileTreeNodeData node) {
			if (node is IAssemblyFileNode)
				return NodeType.Assembly;
			if (node is IModuleFileNode)
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
			if (node is IPEFileNode)
				return NodeType.PEFileNode;
			if (node is IReferencesNode)
				return NodeType.References;
			if (node is IUnknownFileNode)
				return NodeType.UnknownFileNode;
			if (node is IMessageNode)
				return NodeType.MessageNode;

			return NodeType.Unknown;
		}
	}
}
