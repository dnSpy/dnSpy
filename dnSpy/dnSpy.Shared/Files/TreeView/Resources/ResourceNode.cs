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
using System.IO;
using System.Threading;
using dnlib.DotNet;
using dnlib.IO;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Properties;
using dnSpy.Shared.Text;

namespace dnSpy.Shared.Files.TreeView.Resources {
	public abstract class ResourceNode : FileTreeNodeData, IResourceNode {
		public Resource Resource { get; set; }
		public string Name => Resource.Name;

		protected sealed override void Write(IOutputColorWriter output, ILanguage language) =>
			output.WriteFilename(Resource.Name);
		protected sealed override void WriteToolTip(IOutputColorWriter output, ILanguage language) =>
			base.WriteToolTip(output, language);
		protected sealed override ImageReference? GetExpandedIcon(IDotNetImageManager dnImgMgr) => null;

		protected sealed override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			var imgRef = GetIcon();
			if (imgRef.Assembly != null)
				return imgRef;
			var asm = dnImgMgr.GetType().Assembly;
			return ResourceUtils.TryGetImageReference(asm, Resource.Name) ?? new ImageReference(asm, "Resource");
		}

		protected virtual ImageReference GetIcon() => new ImageReference();
		public sealed override NodePathName NodePathName => new NodePathName(Guid, NameUtils.CleanName(Resource.Name));

		public ulong FileOffset {
			get {
				FileOffset fo;
				GetModuleOffset(out fo);
				return (ulong)fo;
			}
		}

		public ulong Length {
			get {
				var er = Resource as EmbeddedResource;
				return er == null ? 0 : (ulong)er.Data.Length;
			}
		}

		public uint RVA {
			get {
				FileOffset fo;
				var module = GetModuleOffset(out fo);
				if (module == null)
					return 0;

				return (uint)module.MetaData.PEImage.ToRVA(fo);
			}
		}

		ModuleDefMD GetModuleOffset(out FileOffset fileOffset) {
			fileOffset = 0;

			var er = Resource as EmbeddedResource;
			if (er == null)
				return null;

			var module = this.GetModule() as ModuleDefMD;//TODO: Support CorModuleDef
			if (module == null)
				return null;

			fileOffset = er.Data.FileOffset;
			return module;
		}

		public override ITreeNodeGroup TreeNodeGroup => treeNodeGroup;
		readonly ITreeNodeGroup treeNodeGroup;

		protected ResourceNode(ITreeNodeGroup treeNodeGroup, Resource resource) {
			if (treeNodeGroup == null || resource == null)
				throw new ArgumentNullException();
			this.treeNodeGroup = treeNodeGroup;
			this.Resource = resource;
		}

		protected void Save() =>
			SaveResources.Save(new IResourceDataProvider[] { this }, false, ResourceDataType.Deserialized);

		public virtual void WriteShort(ITextOutput output, ILanguage language, bool showOffset) {
			language.WriteCommentBegin(output, true);
			output.WriteOffsetComment(this, showOffset);
			const string LTR = "\u200E";
			output.WriteDefinition(NameUtils.CleanName(Name) + LTR, this, BoxedOutputColor.Comment);
			string extra = null;
			switch (Resource.ResourceType) {
			case ResourceType.AssemblyLinked:
				extra = ((AssemblyLinkedResource)Resource).Assembly.FullName;
				break;
			case ResourceType.Linked:
				var file = ((LinkedResource)Resource).File;
				extra = string.Format("{0}, {1}, {2}", file.Name, file.ContainsNoMetaData ? "ContainsNoMetaData" : "ContainsMetaData", SimpleTypeConverter.ByteArrayToString(file.HashValue));
				break;
			case ResourceType.Embedded:
				extra = string.Format(dnSpy_Shared_Resources.NumberOfBytes, ((EmbeddedResource)Resource).Data.Length);
				break;
			}
			output.Write(string.Format(" ({0}{1}, {2})", extra == null ? string.Empty : string.Format("{0}, ", extra), Resource.ResourceType, Resource.Attributes), BoxedOutputColor.Comment);
			language.WriteCommentEnd(output, true);
			output.WriteLine();
		}

		public virtual string ToString(CancellationToken token, bool canDecompile) => null;

		public IEnumerable<ResourceData> GetResourceData(ResourceDataType type) {
			switch (type) {
			case ResourceDataType.Deserialized:
				return GetDeserializedData();
			case ResourceDataType.Serialized:
				return GetSerializedData();
			default:
				throw new InvalidOperationException();
			}
		}

		protected virtual IEnumerable<ResourceData> GetDeserializedData() => GetSerializedData();

		protected virtual IEnumerable<ResourceData> GetSerializedData() {
			var er = Resource as EmbeddedResource;
			if (er != null)
				yield return new ResourceData(Resource.Name, token => new MemoryStream(er.GetResourceData()));
		}

		public sealed override FilterType GetFilterType(IFileTreeNodeFilter filter) => filter.GetResult(this).FilterType;
	}
}
