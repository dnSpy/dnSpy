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
using System.IO;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnlib.IO;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.DnSpy.Properties;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Contracts.Documents.TreeView.Resources {
	/// <summary>
	/// Resource element node base class
	/// </summary>
	public abstract class ResourceElementNode : DocumentTreeNodeData, IResourceNode {
		/// <summary>
		/// Gets the resource element
		/// </summary>
		public ResourceElement ResourceElement => resourceElement;
		ResourceElement resourceElement;    // updated by the asm editor, see UpdateData()

		/// <summary>
		/// Gets the name
		/// </summary>
		public string Name => resourceElement.Name;

		/// <inheritdoc/>
		protected sealed override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			output.WriteFilename(Uri.UnescapeDataString(resourceElement.Name));
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				if (TreeNode.Parent?.Data is ResourceNode parentNode) {
					output.WriteLine();
					output.WriteFilename(parentNode.Name);
				}
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
			return ResourceUtilities.TryGetImageReference(asm, resourceElement.Name) ?? DsImages.Dialog;
		}

		/// <summary>
		/// Gets the icon to use
		/// </summary>
		/// <returns></returns>
		protected virtual ImageReference GetIcon() => new ImageReference();

		/// <inheritdoc/>
		public sealed override NodePathName NodePathName => new NodePathName(Guid, NameUtilities.CleanName(resourceElement.Name));

		/// <summary>
		/// Gets the file offset of the resource
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
		public uint Length => resourceElement.ResourceData.EndOffset - resourceElement.ResourceData.StartOffset;

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
			GetModuleOffset(this, resourceElement, out fileOffset);

		internal static ModuleDefMD? GetModuleOffset(DocumentTreeNodeData node, ResourceElement resourceElement, out FileOffset fileOffset) {
			fileOffset = 0;

			var module = node.GetModule() as ModuleDefMD;//TODO: Support CorModuleDef
			if (module is null)
				return null;

			fileOffset = resourceElement.ResourceData.StartOffset;
			return module;
		}

		/// <inheritdoc/>
		public override ITreeNodeGroup? TreeNodeGroup => treeNodeGroup;
		readonly ITreeNodeGroup treeNodeGroup;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="treeNodeGroup">Treenode group</param>
		/// <param name="resourceElement">Resource element</param>
		protected ResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement) {
			this.treeNodeGroup = treeNodeGroup ?? throw new ArgumentNullException(nameof(treeNodeGroup));
			this.resourceElement = resourceElement ?? throw new ArgumentNullException(nameof(resourceElement));
		}

		/// <inheritdoc/>
		public virtual void WriteShort(IDecompilerOutput output, IDecompiler decompiler, bool showOffset) {
			decompiler.WriteCommentBegin(output, true);
			output.WriteOffsetComment(this, showOffset);
			const string LTR = "\u200E";
			output.Write(NameUtilities.CleanName(Name) + LTR, this, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextColor.Comment);
			output.Write($" = {ValueString}", BoxedTextColor.Comment);
			decompiler.WriteCommentEnd(output, true);
			output.WriteLine();
		}

		/// <summary>
		/// Gets the value as a string
		/// </summary>
		protected virtual string ValueString {
			get {
				switch (resourceElement.ResourceData.Code) {
				case ResourceTypeCode.Null:
					return "null";

				case ResourceTypeCode.String:
					return SimpleTypeConverter.ToString((string)((BuiltInResourceData)resourceElement.ResourceData).Data, false);

				case ResourceTypeCode.Boolean:
					return SimpleTypeConverter.ToString((bool)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Char:
					return SimpleTypeConverter.ToString((char)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Byte:
					return SimpleTypeConverter.ToString((byte)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.SByte:
					return SimpleTypeConverter.ToString((sbyte)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Int16:
					return SimpleTypeConverter.ToString((short)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.UInt16:
					return SimpleTypeConverter.ToString((ushort)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Int32:
					return SimpleTypeConverter.ToString((int)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.UInt32:
					return SimpleTypeConverter.ToString((uint)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Int64:
					return SimpleTypeConverter.ToString((long)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.UInt64:
					return SimpleTypeConverter.ToString((ulong)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Single:
					return SimpleTypeConverter.ToString((float)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Double:
					return SimpleTypeConverter.ToString((double)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Decimal:
					return ((decimal)((BuiltInResourceData)resourceElement.ResourceData).Data).ToString();

				case ResourceTypeCode.DateTime:
					return ((DateTime)((BuiltInResourceData)resourceElement.ResourceData).Data).ToString();

				case ResourceTypeCode.TimeSpan:
					return ((TimeSpan)((BuiltInResourceData)resourceElement.ResourceData).Data).ToString();

				case ResourceTypeCode.ByteArray:
				case ResourceTypeCode.Stream:
					var ary = (byte[])((BuiltInResourceData)resourceElement.ResourceData).Data;
					return string.Format(dnSpy_Contracts_DnSpy_Resources.NumberOfBytes, ary.Length);

				default:
					var binData = resourceElement.ResourceData as BinaryResourceData;
					if (!(binData is null))
						return string.Format(dnSpy_Contracts_DnSpy_Resources.NumberOfBytesAndType, binData.Data.Length, binData.TypeName);
					return resourceElement.ResourceData.ToString() ?? string.Empty;
				}
			}
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
		protected abstract IEnumerable<ResourceData> GetDeserializedData();

		IEnumerable<ResourceData> GetSerializedData() => GetSerializedData(resourceElement);

		internal static IEnumerable<ResourceData> GetSerializedData(ResourceElement resourceElement) {
			var outStream = new MemoryStream();
			var writer = new BinaryWriter(outStream);

			var builtin = resourceElement.ResourceData as BuiltInResourceData;
			var bin = resourceElement.ResourceData as BinaryResourceData;
			switch (resourceElement.ResourceData.Code) {
			case ResourceTypeCode.Null:
				break;

			case ResourceTypeCode.String:
				writer.Write((string)builtin!.Data);
				break;

			case ResourceTypeCode.Boolean:
				writer.Write((bool)builtin!.Data);
				break;

			case ResourceTypeCode.Char:
				writer.Write((ushort)(char)builtin!.Data);
				break;

			case ResourceTypeCode.Byte:
				writer.Write((byte)builtin!.Data);
				break;

			case ResourceTypeCode.SByte:
				writer.Write((sbyte)builtin!.Data);
				break;

			case ResourceTypeCode.Int16:
				writer.Write((short)builtin!.Data);
				break;

			case ResourceTypeCode.UInt16:
				writer.Write((ushort)builtin!.Data);
				break;

			case ResourceTypeCode.Int32:
				writer.Write((int)builtin!.Data);
				break;

			case ResourceTypeCode.UInt32:
				writer.Write((uint)builtin!.Data);
				break;

			case ResourceTypeCode.Int64:
				writer.Write((long)builtin!.Data);
				break;

			case ResourceTypeCode.UInt64:
				writer.Write((ulong)builtin!.Data);
				break;

			case ResourceTypeCode.Single:
				writer.Write((float)builtin!.Data);
				break;

			case ResourceTypeCode.Double:
				writer.Write((double)builtin!.Data);
				break;

			case ResourceTypeCode.Decimal:
				writer.Write((decimal)builtin!.Data);
				break;

			case ResourceTypeCode.DateTime:
				writer.Write(((DateTime)builtin!.Data).ToBinary());
				break;

			case ResourceTypeCode.TimeSpan:
				writer.Write(((TimeSpan)builtin!.Data).Ticks);
				break;

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				// Don't write array length, just the data
				writer.Write((byte[])builtin!.Data);
				break;

			default:
				writer.Write(bin!.Data);
				break;
			}

			outStream.Position = 0;
			yield return new ResourceData(resourceElement.Name, token => outStream);
		}

		/// <summary>
		/// Checks whether <see cref="UpdateData"/> can execute. Used by the
		/// assembly editor. Returns null or an empty string if the data can be updated, else an
		/// error string that can be shown to the user.
		/// </summary>
		/// <param name="newResElem">New data</param>
		/// <returns></returns>
		public virtual string? CheckCanUpdateData(ResourceElement newResElem) {
			if (resourceElement.ResourceData.Code.FixUserType() != newResElem.ResourceData.Code.FixUserType())
				return dnSpy_Contracts_DnSpy_Resources.ResourceTypeCantBeChanged;

			return string.Empty;
		}

		/// <summary>
		/// Updates the internal resource data. Must only be called if
		/// <see cref="CheckCanUpdateData"/> returned true. Used by the assembly
		/// editor.
		/// </summary>
		/// <param name="newResElem">New data</param>
		public virtual void UpdateData(ResourceElement newResElem) => resourceElement = newResElem;

		/// <inheritdoc/>
		public sealed override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResult(this).FilterType;

		sealed class Data {
			public readonly ResourceElement ResourceElement;
			public Data(ResourceElement resourceElement) => ResourceElement = resourceElement;
		}

		/// <summary>
		/// Gets the resource element or null
		/// </summary>
		/// <param name="node">Node</param>
		/// <returns></returns>
		public static ResourceElement? GetResourceElement(DocumentTreeNodeData node) {
			if (node is ResourceElementNode resourceElementNode)
				return resourceElementNode.ResourceElement;
			if (node.TryGetData(out Data? data))
				return data.ResourceElement;
			return null;
		}

		/// <summary>
		/// Adds the resource element to a resource element node
		/// </summary>
		/// <param name="node">Node</param>
		/// <param name="resourceElement">Resource element</param>
		public static void AddResourceElement(DocumentTreeNodeData node, ResourceElement resourceElement) {
			if (node is ResourceElementNode resourceElementNode) {
				if (resourceElementNode.ResourceElement != resourceElement)
					throw new InvalidOperationException();
			}
			else {
				if (node.TryGetData<Data>(out _))
					throw new InvalidOperationException();
				node.AddData(new Data(resourceElement));
			}
		}
	}
}
