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
	}

	struct NodeDecompiler {
		readonly ITextOutput output;
		readonly ILanguage language;
		readonly DecompilationOptions decompilationOptions;
		readonly IFileTreeNodeData node;

		public NodeDecompiler(ITextOutput output, ILanguage language, DecompilationOptions decompilationOptions, IFileTreeNodeData node) {
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

			default:
				Debug.Fail(string.Format("Unknown NodeType: {0}", nodeType));
				goto case NodeType.Unknown;
			}
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

			return NodeType.Unknown;
		}
	}
}
