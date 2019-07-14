/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Threading;
using dnlib.DotNet;
using dnlib.IO;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.DnSpy.Properties;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Resource node base class
	/// </summary>
	public abstract class ResourceNode : DocumentTreeNodeData, IResourceNode {
		/// <summary>
		/// Gets the resource
		/// </summary>
		public Resource Resource { get; set; }

		/// <summary>
		/// Gets the name
		/// </summary>
		public string Name => Resource.Name;

		/// <inheritdoc/>
		protected sealed override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			output.WriteFilename(Resource.Name);
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.WriteLine();
				WriteFilename(output);
			}
		}

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
		public uint FileOffset {
			get {
				GetModuleOffset(out var fo);
				return (uint)fo;
			}
		}

		/// <summary>
		/// Gets the length of the resource
		/// </summary>
		public uint Length {
			get {
				var er = Resource as EmbeddedResource;
				return er is null ? 0 : er.Length;
			}
		}

		/// <summary>
		/// Gets the RVA of the resource
		/// </summary>
		public uint RVA {
			get {
				var module = GetModuleOffset(out var fo);
				if (module is null)
					return 0;

				return (uint)module.Metadata.PEImage.ToRVA(fo);
			}
		}

		ModuleDefMD? GetModuleOffset(out FileOffset fileOffset) =>
			GetModuleOffset(this, Resource, out fileOffset);

		internal static ModuleDefMD? GetModuleOffset(DocumentTreeNodeData node, Resource resource, out FileOffset fileOffset) {
			fileOffset = 0;

			var er = resource as EmbeddedResource;
			if (er is null)
				return null;

			var module = node.GetModule() as ModuleDefMD;//TODO: Support CorModuleDef
			if (module is null)
				return null;

			fileOffset = (FileOffset)er.CreateReader().StartOffset;
			return module;
		}

		/// <inheritdoc/>
		public override ITreeNodeGroup? TreeNodeGroup => treeNodeGroup;
		readonly ITreeNodeGroup treeNodeGroup;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="treeNodeGroup">Treenode group</param>
		/// <param name="resource">Resource</param>
		protected ResourceNode(ITreeNodeGroup treeNodeGroup, Resource resource) {
			this.treeNodeGroup = treeNodeGroup ?? throw new ArgumentNullException(nameof(treeNodeGroup));
			Resource = resource ?? throw new ArgumentNullException(nameof(resource));
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
			string? extra = null;
			switch (Resource.ResourceType) {
			case ResourceType.AssemblyLinked:
				extra = ((AssemblyLinkedResource)Resource).Assembly.FullName;
				break;
			case ResourceType.Linked:
				var file = ((LinkedResource)Resource).File;
				extra = $"{file.Name}, {(file.ContainsNoMetadata ? "ContainsNoMetaData" : "ContainsMetaData")}, {SimpleTypeConverter.ByteArrayToString(file.HashValue)}";
				break;
			case ResourceType.Embedded:
				extra = string.Format(dnSpy_Contracts_DnSpy_Resources.NumberOfBytes, ((EmbeddedResource)Resource).Length);
				break;
			}
			output.Write($" ({(extra is null ? string.Empty : $"{extra}, ")}{Resource.ResourceType}, {Resource.Attributes})", BoxedTextColor.Comment);
			decompiler.WriteCommentEnd(output, true);
			output.WriteLine();
		}

		/// <summary>
		/// Converts the value to a string
		/// </summary>
		/// <param name="token">Cancellation token</param>
		/// <param name="canDecompile">true if the data can be decompiled</param>
		/// <returns></returns>
		public virtual string? ToString(CancellationToken token, bool canDecompile) => null;

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
			if (Resource is EmbeddedResource er)
				yield return new ResourceData(Resource.Name, token => er.CreateReader().AsStream());
		}

		/// <inheritdoc/>
		public sealed override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResult(this).FilterType;

		sealed class Data {
			public readonly Resource Resource;
			public Data(Resource resource) => Resource = resource;
		}

		/// <summary>
		/// Gets the resource or null
		/// </summary>
		/// <param name="node">Node</param>
		/// <returns></returns>
		public static Resource? GetResource(DocumentTreeNodeData node) {
			if (node is ResourceNode resourceNode)
				return resourceNode.Resource;
			if (node.TryGetData(out Data? data))
				return data.Resource;
			return null;
		}

		/// <summary>
		/// Adds the resource to a resource node
		/// </summary>
		/// <param name="node">Node</param>
		/// <param name="resource">Resource</param>
		public static void AddResource(DocumentTreeNodeData node, Resource resource) {
			if (node is ResourceNode resourceNode) {
				if (resourceNode.Resource != resource)
					throw new InvalidOperationException();
			}
			else {
				if (node.TryGetData<Data>(out _))
					throw new InvalidOperationException();
				node.AddData(new Data(resource));
			}
		}
	}
}
