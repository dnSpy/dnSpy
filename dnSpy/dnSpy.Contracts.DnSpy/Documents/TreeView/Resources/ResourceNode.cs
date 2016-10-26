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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Properties;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Resource node base class
	/// </summary>
	public abstract class ResourceNode : DocumentTreeNodeData, IResourceNode {
		/// <inheritdoc/>
		public Resource Resource { get; set; }
		/// <inheritdoc/>
		public string Name => Resource.Name;

		/// <inheritdoc/>
		protected sealed override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) =>
			output.WriteFilename(Resource.Name);
		/// <inheritdoc/>
		protected sealed override ImageReference? GetExpandedIcon(IDotNetImageService dnImgMgr) => null;

		/// <inheritdoc/>
		protected sealed override ImageReference GetIcon(IDotNetImageService dnImgMgr) {
			var imgRef = GetIcon();
			if (!imgRef.IsDefault)
				return imgRef;
			var asm = dnImgMgr.GetType().Assembly;
			return ResourceUtilities.TryGetImageReference(asm, Resource.Name) ?? DsImages.Dialog;
		}

		/// <summary>
		/// Gets the icon
		/// </summary>
		/// <returns></returns>
		protected virtual ImageReference GetIcon() => new ImageReference();

		/// <inheritdoc/>
		public sealed override NodePathName NodePathName => new NodePathName(Guid, NameUtilities.CleanName(Resource.Name));

		/// <summary>
		/// Gets the offset of the resource
		/// </summary>
		public ulong FileOffset {
			get {
				FileOffset fo;
				GetModuleOffset(out fo);
				return (ulong)fo;
			}
		}

		/// <summary>
		/// Gets the length of the resource
		/// </summary>
		public ulong Length {
			get {
				var er = Resource as EmbeddedResource;
				return er == null ? 0 : (ulong)er.Data.Length;
			}
		}

		/// <summary>
		/// Gets the RVA of the resource
		/// </summary>
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

		/// <inheritdoc/>
		public override ITreeNodeGroup TreeNodeGroup => treeNodeGroup;
		readonly ITreeNodeGroup treeNodeGroup;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="treeNodeGroup"></param>
		/// <param name="resource"></param>
		protected ResourceNode(ITreeNodeGroup treeNodeGroup, Resource resource) {
			if (treeNodeGroup == null)
				throw new ArgumentNullException(nameof(treeNodeGroup));
			if (resource == null)
				throw new ArgumentNullException(nameof(resource));
			this.treeNodeGroup = treeNodeGroup;
			this.Resource = resource;
		}

		/// <summary>
		/// Saves the resource
		/// </summary>
		protected void Save() =>
			SaveResources.Save(new IResourceDataProvider[] { this }, false, ResourceDataType.Deserialized);

		/// <inheritdoc/>
		public virtual void WriteShort(IDecompilerOutput output, IDecompiler decompiler, bool showOffset) {
			decompiler.WriteCommentBegin(output, true);
			output.WriteOffsetComment(this, showOffset);
			const string LTR = "\u200E";
			output.Write(NameUtilities.CleanName(Name) + LTR, this, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextColor.Comment);
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
				extra = string.Format(dnSpy_Contracts_DnSpy_Resources.NumberOfBytes, ((EmbeddedResource)Resource).Data.Length);
				break;
			}
			output.Write(string.Format(" ({0}{1}, {2})", extra == null ? string.Empty : string.Format("{0}, ", extra), Resource.ResourceType, Resource.Attributes), BoxedTextColor.Comment);
			decompiler.WriteCommentEnd(output, true);
			output.WriteLine();
		}

		/// <summary>
		/// Converts the value to a string
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="canDecompile">true if the data can be decompiled</param>
		/// <returns></returns>
		public virtual string ToString(CancellationToken token, bool canDecompile) => null;

		/// <inheritdoc/>
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

		/// <summary>
		/// Gets the deserialized data
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerable<ResourceData> GetDeserializedData() => GetSerializedData();

		/// <summary>
		/// Gets the serialized data
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerable<ResourceData> GetSerializedData() {
			var er = Resource as EmbeddedResource;
			if (er != null)
				yield return new ResourceData(Resource.Name, token => new MemoryStream(er.GetResourceData()));
		}

		/// <inheritdoc/>
		public sealed override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResult(this).FilterType;
	}
}
