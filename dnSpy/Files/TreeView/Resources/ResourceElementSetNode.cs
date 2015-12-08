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
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Decompiler;
using dnSpy.Shared.UI.Files.TreeView.Resources;
using ICSharpCode.Decompiler;

namespace dnSpy.Files.TreeView.Resources {
	[ExportResourceNodeCreator(Order = FileTVConstants.ORDER_RSRCCREATOR_RSRCELEMSET)]
	sealed class ResourceElementSetNodeCreator : IResourceNodeCreator {
		public IResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup) {
			var er = resource as EmbeddedResource;
			if (er == null)
				return null;
			er.Data.Position = 0;
			if (!ResourceReader.CouldBeResourcesFile(er.Data))
				return null;

			er.Data.Position = 0;
			return new ResourceElementSetNode(treeNodeGroup, module, er);
		}

		public IResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup) {
			return null;
		}
	}

	sealed class ResourceElementSetNode : ResourceNode, IResourceElementSetNode {
		readonly ResourceElementSet resourceElementSet;
		readonly ModuleDef module;

		public override Guid Guid {
			get { return new Guid(FileTVConstants.RESOURCE_ELEMENT_SET_NODE_GUID); }
		}

		protected override ImageReference GetIcon() {
			return new ImageReference(GetType().Assembly, "ResourcesFile");
		}

		public ResourceElementSetNode(ITreeNodeGroup treeNodeGroup, ModuleDef module, EmbeddedResource resource)
			: base(treeNodeGroup, resource) {
			this.module = module;
			this.resourceElementSet = ResourceReader.Read(module, resource.Data);
		}

		public override void Initialize() {
			TreeNode.LazyLoading = true;
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			var treeNodeGroup = Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.ResourceElementTreeNodeGroup);
			foreach (var elem in resourceElementSet.ResourceElements)
				yield return Context.ResourceNodeFactory.Create(module, elem, treeNodeGroup);
		}

		protected override IEnumerable<ResourceData> GetDeserializedData() {
			TreeNode.EnsureChildrenLoaded();
			foreach (IResourceDataProvider node in TreeNode.DataChildren) {
				foreach (var data in node.GetResourceData(ResourceDataType.Deserialized))
					yield return data;
			}
		}

		public override void WriteShort(ITextOutput output, ILanguage language, bool showOffset) {
			base.WriteShort(output, language, showOffset);
			var so = output as ISmartTextOutput;
			if (so != null) {
				so.AddButton("Save", (s, e) => Save());
				so.WriteLine();
				so.WriteLine();
			}
		}

		public void RegenerateEmbeddedResource() {
			var module = this.GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();
			RegenerateEmbeddedResource(module);
		}

		void RegenerateEmbeddedResource(ModuleDef module) {
			TreeNode.EnsureChildrenLoaded();
			var outStream = new MemoryStream();
			var resources = new ResourceElementSet();
			foreach (IResourceElementNode child in TreeNode.DataChildren)
				resources.Add(child.ResourceElement);
			ResourceWriter.Write(module, outStream, resources);
			this.Resource = new EmbeddedResource(Resource.Name, outStream.ToArray(), Resource.Attributes);
		}
	}
}
