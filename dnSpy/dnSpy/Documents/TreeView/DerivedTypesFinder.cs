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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;
using dnSpy.Properties;

namespace dnSpy.Documents.TreeView {
	sealed class DerivedTypesFinder : AsyncNodeProvider {
		readonly WeakReference[] weakModules;
		readonly TypeDef type;
		readonly ITreeNodeGroup msgNodeGroup;
		readonly ITreeNodeGroup derivedTypesGroup;

		public DerivedTypesFinder(DocumentTreeNodeData targetNode, TypeDef type)
			: base(targetNode) {
			this.msgNodeGroup = targetNode.Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MessageTreeNodeGroupDerivedTypes);
			this.derivedTypesGroup = targetNode.Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.DerivedTypeTreeNodeGroupDerivedTypes);
			this.weakModules = targetNode.Context.DocumentTreeView.DocumentService.GetDocuments().Where(a => a.ModuleDef != null).SelectMany(a => a.AssemblyDef != null ? (IEnumerable<ModuleDef>)a.AssemblyDef.Modules : new[] { a.ModuleDef }).Select(a => new WeakReference(a)).ToArray();
			this.type = type;
			Start();
		}

		public static bool QuickCheck(TypeDef type) {
			if (type == null)
				return false;
			if (!type.IsInterface && type.IsSealed)
				return false;
			return true;
		}

		protected override void ThreadMethod() {
			if (!QuickCheck(type))
				return;

			//TODO: If it's not a public type, only check modules in this assembly and any friend assemblies

			AddMessageNode(() => new MessageNode(msgNodeGroup, new Guid(DocumentTreeViewConstants.MESSAGE_NODE_GUID), DsImages.Search, dnSpy_Resources.Searching));
			foreach (var weakMod in weakModules) {
				cancellationToken.ThrowIfCancellationRequested();
				var mod = (ModuleDef)weakMod.Target;
				if (mod == null)
					continue;

				foreach (var td in FindDerivedTypes(mod))
					AddNode(new DerivedTypeNode(derivedTypesGroup, td));
			}
		}

		IEnumerable<TypeDef> FindDerivedTypes(ModuleDef module) {
			if (type.IsInterface) {
				foreach (var td in module.GetTypes()) {
					foreach (var iface in td.Interfaces) {
						if (new SigComparer().Equals(type, iface.Interface.ScopeType))
							yield return td;
					}
				}
			}
			else {
				foreach (var td in module.GetTypes()) {
					var bt = td.BaseType;
					if (bt != null && new SigComparer().Equals(type, bt.ScopeType))
						yield return td;
				}
			}
		}
	}
}
