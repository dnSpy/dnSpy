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
	public abstract class ResourceElementNode : FileTreeNodeData, IResourceElementNode {
		public ResourceElement ResourceElement {
			get { return resourceElement; }
		}
		ResourceElement resourceElement;	// updated by the asm editor, see UpdateData()

		public string Name {
			get { return resourceElement.Name; }
		}

		protected sealed override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.WriteFilename(resourceElement.Name);
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
			return ResourceUtils.TryGetImageReference(asm, resourceElement.Name) ?? new ImageReference(asm, "Resource");
		}

		protected virtual ImageReference GetIcon() {
			return new ImageReference();
		}

		public sealed override NodePathName NodePathName {
			get { return new NodePathName(Guid, NameUtils.CleanName(resourceElement.Name)); }
		}

		public ulong FileOffset {
			get {
				FileOffset fo;
				GetModuleOffset(out fo);
				return (ulong)fo;
			}
		}

		public ulong Length {
			get { return (ulong)(resourceElement.ResourceData.EndOffset - resourceElement.ResourceData.StartOffset); }
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

			var module = this.GetModule() as ModuleDefMD;//TODO: Support CorModuleDef
			if (module == null)
				return null;

			fileOffset = resourceElement.ResourceData.StartOffset;
			return module;
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		protected ResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement) {
			if (treeNodeGroup == null || resourceElement == null)
				throw new ArgumentNullException();
			this.treeNodeGroup = treeNodeGroup;
			this.resourceElement = resourceElement;
		}

		public virtual void WriteShort(ITextOutput output, ILanguage language, bool showOffset) {
			language.WriteCommentBegin(output, true);
			output.WriteOffsetComment(this, showOffset);
			const string LTR = "\u200E";
			output.WriteDefinition(NameUtils.CleanName(Name) + LTR, this, TextTokenKind.Comment);
			output.Write(string.Format(" = {0}", ValueString), TextTokenKind.Comment);
			language.WriteCommentEnd(output, true);
			output.WriteLine();
		}

		protected virtual string ValueString {
			get {
				switch (resourceElement.ResourceData.Code) {
				case ResourceTypeCode.Null:
					return "null";

				case ResourceTypeCode.String:
					return NumberVMUtils.ToString((string)((BuiltInResourceData)resourceElement.ResourceData).Data, false);

				case ResourceTypeCode.Boolean:
					return NumberVMUtils.ToString((bool)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Char:
					return NumberVMUtils.ToString((char)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Byte:
					return NumberVMUtils.ToString((byte)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.SByte:
					return NumberVMUtils.ToString((sbyte)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Int16:
					return NumberVMUtils.ToString((short)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.UInt16:
					return NumberVMUtils.ToString((ushort)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Int32:
					return NumberVMUtils.ToString((int)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.UInt32:
					return NumberVMUtils.ToString((uint)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Int64:
					return NumberVMUtils.ToString((long)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.UInt64:
					return NumberVMUtils.ToString((ulong)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Single:
					return NumberVMUtils.ToString((float)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Double:
					return NumberVMUtils.ToString((double)((BuiltInResourceData)resourceElement.ResourceData).Data);

				case ResourceTypeCode.Decimal:
					return ((decimal)((BuiltInResourceData)resourceElement.ResourceData).Data).ToString();

				case ResourceTypeCode.DateTime:
					return ((DateTime)((BuiltInResourceData)resourceElement.ResourceData).Data).ToString();

				case ResourceTypeCode.TimeSpan:
					return ((TimeSpan)((BuiltInResourceData)resourceElement.ResourceData).Data).ToString();

				case ResourceTypeCode.ByteArray:
				case ResourceTypeCode.Stream:
					var ary = (byte[])((BuiltInResourceData)resourceElement.ResourceData).Data;
					return string.Format(dnSpy_Shared_Resources.NumberOfBytes, ary.Length);

				default:
					var binData = resourceElement.ResourceData as BinaryResourceData;
					if (binData != null)
						return string.Format(dnSpy_Shared_Resources.NumberOfBytesAndType, binData.Data.Length, binData.TypeName);
					return resourceElement.ResourceData.ToString();
				}
			}
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

		public virtual string CheckCanUpdateData(ResourceElement newResElem) {
			if (resourceElement.ResourceData.Code.FixUserType() != newResElem.ResourceData.Code.FixUserType())
				return dnSpy_Shared_Resources.ResourceTypeCantBeChanged;

			return string.Empty;
		}

		public virtual void UpdateData(ResourceElement newResElem) {
			resourceElement = newResElem;
		}

		public sealed override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			return filter.GetResult(this).FilterType;
		}
	}
}
