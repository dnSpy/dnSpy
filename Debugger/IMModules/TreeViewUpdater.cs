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

using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.Debugger.IMModules {
	struct TreeViewUpdater {
		readonly CorModuleDefFile CorModuleDefFile;
		readonly AssemblyTreeNode ModuleNode;
		readonly HashSet<uint> modifiedTypes;
		readonly HashSet<uint> loadedClassTokens;
		readonly HashSet<TypeDef> checkedTypes;
		AssemblyTreeNode modNode;

		DnSpyFileListTreeNode DnSpyFileListTreeNode {
			get { return MainWindow.Instance.DnSpyFileListTreeNode; }
		}

		public TreeViewUpdater(CorModuleDefFile cmdf, AssemblyTreeNode node, HashSet<uint> modifiedTypes, HashSet<uint> loadedClassTokens) {
			Debug.Assert(node.DnSpyFile == cmdf);
			this.CorModuleDefFile = cmdf;
			this.ModuleNode = node;
			this.modifiedTypes = new HashSet<uint>(modifiedTypes);
			this.loadedClassTokens = loadedClassTokens;
			this.checkedTypes = new HashSet<TypeDef>();
			this.modNode = node;
		}

		public void Update() {
			// If none of its children have been loaded, we can safely return without doing anything
			if (ModuleNode.LazyLoading)
				return;

			if (loadedClassTokens != null)
				modifiedTypes.AddRange(loadedClassTokens);
			var tokensList = new List<uint>(modifiedTypes);
			tokensList.Sort();

			bool needRedecompile = false;
			foreach (uint token in tokensList) {
				var td = CorModuleDefFile.DnModule.CorModuleDef.ResolveToken(token) as TypeDef;
				Debug.Assert(td != null);
				if (td == null)
					continue;
				Update(td);
				needRedecompile = true;
			}

			if (needRedecompile) {
				// Force a re-decompile of every view that references this module. This could be
				// optimized if necessary
				MainWindow.Instance.ModuleModified(CorModuleDefFile);
			}
		}

		List<TypeDef> GetNonCheckedTypeAndDeclaringTypes(TypeDef td) {
			var list = new List<TypeDef>();
			while (td != null && !checkedTypes.Contains(td)) {
				list.Add(td);
				checkedTypes.Add(td);
				td = td.DeclaringType;
			}
			// Enclosing types before enclosed types
			list.Reverse();
			return list;
		}

		void Update(TypeDef td) {
			var list = GetNonCheckedTypeAndDeclaringTypes(td);
			CreateTypeTreeNodes(list);
		}

		void CreateTypeTreeNodes(List<TypeDef> types) {
			TypeTreeNode parentNode = null;
			foreach (var type in types) {
				bool wasLoaded = loadedClassTokens == null ? false : loadedClassTokens.Contains(type.MDToken.Raw);

				TypeTreeNode typeNode;
				if (type.DeclaringType == null)
					typeNode = modNode.GetOrCreateNonNestedTypeTreeNode(type);
				else {
					if (parentNode == null)
						parentNode = DnSpyFileListTreeNode.FindTypeNode(type.DeclaringType);
					if (parentNode == null || parentNode.LazyLoading)
						break;
					typeNode = parentNode.GetOrCreateNestedTypeTreeNode(type);
				}
				Debug.Assert(typeNode != null);

				if (wasLoaded || modifiedTypes.Contains(type.MDToken.Raw))
					UpdateMemberNodes(typeNode);

				parentNode = typeNode;
			}
		}

		void UpdateMemberNodes(TypeTreeNode typeNode) {
			// If it's not been loaded, we've got nothing to do
			if (typeNode.LazyLoading)
				return;

			var existing = new HashSet<object>();
			foreach (var child in typeNode.Children) {
				var memberNode = child as IMemberTreeNode;
				if (memberNode != null)
					existing.Add(memberNode.Member);
			}

			foreach (var fd in typeNode.TypeDef.Fields) {
				if (existing.Contains(fd))
					continue;
				typeNode.AddToChildren(new FieldTreeNode(fd));
			}

			foreach (var pd in typeNode.TypeDef.Properties) {
				if (existing.Contains(pd))
					continue;
				typeNode.AddToChildren(new PropertyTreeNode(pd, typeNode));
			}

			foreach (var ed in typeNode.TypeDef.Events) {
				if (existing.Contains(ed))
					continue;
				typeNode.AddToChildren(new EventTreeNode(ed));
			}

			var accessorMethods = typeNode.TypeDef.GetAccessorMethods();
			foreach (var md in typeNode.TypeDef.Methods) {
				if (existing.Contains(md))
					continue;
				if (!accessorMethods.Contains(md))
					typeNode.AddToChildren(new MethodTreeNode(md));
			}
		}
	}
}
