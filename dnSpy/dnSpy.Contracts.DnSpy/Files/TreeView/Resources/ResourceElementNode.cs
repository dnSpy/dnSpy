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
using dnlib.DotNet.Resources;
using dnlib.IO;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Properties;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Contracts.Files.TreeView.Resources {
	/// <summary>
	/// Resource element node base class
	/// </summary>
	public abstract class ResourceElementNode : FileTreeNodeData, IResourceElementNode {
		/// <inheritdoc/>
		public ResourceElement ResourceElement => resourceElement;
		ResourceElement resourceElement;	// updated by the asm editor, see UpdateData()

		/// <inheritdoc/>
		public string Name => resourceElement.Name;

		/// <inheritdoc/>
		protected sealed override void Write(IOutputColorWriter output, ILanguage language) =>
			output.WriteFilename(resourceElement.Name);
		/// <inheritdoc/>
		protected sealed override void WriteToolTip(IOutputColorWriter output, ILanguage language) =>
			base.WriteToolTip(output, language);
		/// <inheritdoc/>
		protected sealed override ImageReference? GetExpandedIcon(IDotNetImageManager dnImgMgr) => null;

		/// <inheritdoc/>
		protected sealed override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			var imgRef = GetIcon();
			if (imgRef.Assembly != null)
				return imgRef;
			var asm = dnImgMgr.GetType().Assembly;
			return ResourceUtilities.TryGetImageReference(asm, resourceElement.Name) ?? new ImageReference(asm, "Resource");
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
		public ulong Length => (ulong)(resourceElement.ResourceData.EndOffset - resourceElement.ResourceData.StartOffset);

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

			var module = this.GetModule() as ModuleDefMD;//TODO: Support CorModuleDef
			if (module == null)
				return null;

			fileOffset = resourceElement.ResourceData.StartOffset;
			return module;
		}

		/// <inheritdoc/>
		public override ITreeNodeGroup TreeNodeGroup => treeNodeGroup;
		readonly ITreeNodeGroup treeNodeGroup;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="treeNodeGroup"></param>
		/// <param name="resourceElement"></param>
		protected ResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement) {
			if (treeNodeGroup == null)
				throw new ArgumentNullException(nameof(treeNodeGroup));
			if (resourceElement == null)
				throw new ArgumentNullException(nameof(resourceElement));
			this.treeNodeGroup = treeNodeGroup;
			this.resourceElement = resourceElement;
		}

		/// <inheritdoc/>
		public virtual void WriteShort(IDecompilerOutput output, ILanguage language, bool showOffset) {
			language.WriteCommentBegin(output, true);
			output.WriteOffsetComment(this, showOffset);
			const string LTR = "\u200E";
			output.Write(NameUtilities.CleanName(Name) + LTR, this, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedOutputColor.Comment);
			output.Write($" = {ValueString}", BoxedOutputColor.Comment);
			language.WriteCommentEnd(output, true);
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
					return string.Format(dnSpy_Contracts_DnSpy.NumberOfBytes, ary.Length);

				default:
					var binData = resourceElement.ResourceData as BinaryResourceData;
					if (binData != null)
						return string.Format(dnSpy_Contracts_DnSpy.NumberOfBytesAndType, binData.Data.Length, binData.TypeName);
					return resourceElement.ResourceData.ToString();
				}
			}
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
		protected abstract IEnumerable<ResourceData> GetDeserializedData();

		IEnumerable<ResourceData> GetSerializedData() {
			var outStream = new MemoryStream();
			var writer = new BinaryWriter(outStream);

			var builtin = resourceElement.ResourceData as BuiltInResourceData;
			var bin = resourceElement.ResourceData as BinaryResourceData;
			switch (resourceElement.ResourceData.Code) {
			case ResourceTypeCode.Null:
				break;

			case ResourceTypeCode.String:
				writer.Write((string)builtin.Data);
				break;

			case ResourceTypeCode.Boolean:
				writer.Write((bool)builtin.Data);
				break;

			case ResourceTypeCode.Char:
				writer.Write((ushort)(char)builtin.Data);
				break;

			case ResourceTypeCode.Byte:
				writer.Write((byte)builtin.Data);
				break;

			case ResourceTypeCode.SByte:
				writer.Write((sbyte)builtin.Data);
				break;

			case ResourceTypeCode.Int16:
				writer.Write((short)builtin.Data);
				break;

			case ResourceTypeCode.UInt16:
				writer.Write((ushort)builtin.Data);
				break;

			case ResourceTypeCode.Int32:
				writer.Write((int)builtin.Data);
				break;

			case ResourceTypeCode.UInt32:
				writer.Write((uint)builtin.Data);
				break;

			case ResourceTypeCode.Int64:
				writer.Write((long)builtin.Data);
				break;

			case ResourceTypeCode.UInt64:
				writer.Write((ulong)builtin.Data);
				break;

			case ResourceTypeCode.Single:
				writer.Write((float)builtin.Data);
				break;

			case ResourceTypeCode.Double:
				writer.Write((double)builtin.Data);
				break;

			case ResourceTypeCode.Decimal:
				writer.Write((decimal)builtin.Data);
				break;

			case ResourceTypeCode.DateTime:
				writer.Write(((DateTime)builtin.Data).ToBinary());
				break;

			case ResourceTypeCode.TimeSpan:
				writer.Write(((TimeSpan)builtin.Data).Ticks);
				break;

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				// Don't write array length, just the data
				writer.Write((byte[])builtin.Data);
				break;

			default:
				writer.Write(bin.Data);
				break;
			}

			outStream.Position = 0;
			yield return new ResourceData(resourceElement.Name, token => outStream);
		}

		/// <summary>
		/// Checks whether the data can be updated. Returns an error string if or null / empty string.
		/// </summary>
		/// <param name="newResElem"></param>
		/// <returns></returns>
		public virtual string CheckCanUpdateData(ResourceElement newResElem) {
			if (resourceElement.ResourceData.Code.FixUserType() != newResElem.ResourceData.Code.FixUserType())
				return dnSpy_Contracts_DnSpy.ResourceTypeCantBeChanged;

			return string.Empty;
		}

		/// <summary>
		/// Updates the old data with the new data. Only gets called if <see cref="CheckCanUpdateData(ResourceElement)"/>
		/// didn't return an error
		/// </summary>
		/// <param name="newResElem">New data</param>
		public virtual void UpdateData(ResourceElement newResElem) => resourceElement = newResElem;

		/// <inheritdoc/>
		public sealed override FilterType GetFilterType(IFileTreeNodeFilter filter) => filter.GetResult(this).FilterType;
	}
}
