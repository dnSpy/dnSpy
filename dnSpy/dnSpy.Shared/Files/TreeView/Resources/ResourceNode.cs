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
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Properties;

namespace dnSpy.Shared.Files.TreeView.Resources {
	public abstract class ResourceNode : FileTreeNodeData, IResourceNode {
		public Resource Resource {
			get { return resource; }
			set { resource = value; }
		}
		Resource resource;

		public string Name {
			get { return resource.Name; }
		}

		protected sealed override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.WriteFilename(resource.Name);
		}

		protected sealed override void WriteToolTip(ISyntaxHighlightOutput output, ILanguage language) {
			base.WriteToolTip(output, language);
		}

		protected sealed override ImageReference? GetExpandedIcon(IDotNetImageManager dnImgMgr) {
			return null;
		}

		protected sealed override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			var imgRef = GetIcon();
			if (imgRef.Assembly != null)
				return imgRef;
			var asm = dnImgMgr.GetType().Assembly;
			return ResourceUtils.TryGetImageReference(asm, resource.Name) ?? new ImageReference(asm, "Resource");
		}

		protected virtual ImageReference GetIcon() {
			return new ImageReference();
		}

		public sealed override NodePathName NodePathName {
			get { return new NodePathName(Guid, NameUtils.CleanName(resource.Name)); }
		}

		public ulong FileOffset {
			get {
				FileOffset fo;
				GetModuleOffset(out fo);
				return (ulong)fo;
			}
		}

		public ulong Length {
			get {
				var er = resource as EmbeddedResource;
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

			var er = resource as EmbeddedResource;
			if (er == null)
				return null;

			var module = this.GetModule() as ModuleDefMD;//TODO: Support CorModuleDef
			if (module == null)
				return null;

			fileOffset = er.Data.FileOffset;
			return module;
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		protected ResourceNode(ITreeNodeGroup treeNodeGroup, Resource resource) {
			if (treeNodeGroup == null || resource == null)
				throw new ArgumentNullException();
			this.treeNodeGroup = treeNodeGroup;
			this.resource = resource;
		}

		protected void Save() {
			SaveResources.Save(new IResourceDataProvider[] { this }, false, ResourceDataType.Deserialized);
		}

		public virtual void WriteShort(ITextOutput output, ILanguage language, bool showOffset) {
			language.WriteCommentBegin(output, true);
			output.WriteOffsetComment(this, showOffset);
			const string LTR = "\u200E";
			output.WriteDefinition(NameUtils.CleanName(Name) + LTR, this, TextTokenKind.Comment);
			string extra = null;
			switch (resource.ResourceType) {
			case ResourceType.AssemblyLinked:
				extra = ((AssemblyLinkedResource)resource).Assembly.FullName;
				break;
			case ResourceType.Linked:
				var file = ((LinkedResource)resource).File;
				extra = string.Format("{0}, {1}, {2}", file.Name, file.ContainsNoMetaData ? "ContainsNoMetaData" : "ContainsMetaData", NumberVMUtils.ByteArrayToString(file.HashValue));
				break;
			case ResourceType.Embedded:
				extra = string.Format(dnSpy_Shared_Resources.NumberOfBytes, ((EmbeddedResource)resource).Data.Length);
				break;
			}
			output.Write(string.Format(" ({0}{1}, {2})", extra == null ? string.Empty : string.Format("{0}, ", extra), resource.ResourceType, resource.Attributes), TextTokenKind.Comment);
			language.WriteCommentEnd(output, true);
			output.WriteLine();
		}

		public virtual string ToString(CancellationToken token, bool canDecompile) {
			return null;
		}

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

		protected virtual IEnumerable<ResourceData> GetDeserializedData() {
			return GetSerializedData();
		}

		protected virtual IEnumerable<ResourceData> GetSerializedData() {
			var er = resource as EmbeddedResource;
			if (er != null)
				yield return new ResourceData(resource.Name, token => new MemoryStream(er.GetResourceData()));
		}

		public sealed override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			return filter.GetResult(this).FilterType;
		}
	}
}
