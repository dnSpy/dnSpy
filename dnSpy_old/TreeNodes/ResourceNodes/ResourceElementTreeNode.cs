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
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnlib.IO;
using dnSpy.Images;
using dnSpy.MVVM;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.TreeNodes {
	public abstract class ResourceElementTreeNode : ILSpyTreeNode, IResourceNode {
		protected ResourceElement resElem;

		public ResourceElement ResourceElement {
			get { return resElem; }
		}

		public sealed override object Icon {
			get { return ResourceUtils.GetIcon(GetType().Assembly, IconName, BackgroundType.TreeNode); }
		}

		public virtual string IconName {
			get { return ResourceUtils.GetIconName(resElem.Name); }
		}

		public string Name {
			get { return resElem.Name; }
		}

		public virtual string ValueString {
			get {
				switch (resElem.ResourceData.Code) {
				case ResourceTypeCode.Null:
					return "null";

				case ResourceTypeCode.String:
					return NumberVMUtils.ToString((string)((BuiltInResourceData)resElem.ResourceData).Data, false);

				case ResourceTypeCode.Boolean:
					return NumberVMUtils.ToString((bool)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.Char:
					return NumberVMUtils.ToString((char)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.Byte:
					return NumberVMUtils.ToString((byte)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.SByte:
					return NumberVMUtils.ToString((sbyte)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.Int16:
					return NumberVMUtils.ToString((short)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.UInt16:
					return NumberVMUtils.ToString((ushort)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.Int32:
					return NumberVMUtils.ToString((int)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.UInt32:
					return NumberVMUtils.ToString((uint)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.Int64:
					return NumberVMUtils.ToString((long)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.UInt64:
					return NumberVMUtils.ToString((ulong)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.Single:
					return NumberVMUtils.ToString((float)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.Double:
					return NumberVMUtils.ToString((double)((BuiltInResourceData)resElem.ResourceData).Data);

				case ResourceTypeCode.Decimal:
					return ((decimal)((BuiltInResourceData)resElem.ResourceData).Data).ToString();

				case ResourceTypeCode.DateTime:
					return ((DateTime)((BuiltInResourceData)resElem.ResourceData).Data).ToString();

				case ResourceTypeCode.TimeSpan:
					return ((TimeSpan)((BuiltInResourceData)resElem.ResourceData).Data).ToString();

				case ResourceTypeCode.ByteArray:
				case ResourceTypeCode.Stream:
					var ary = (byte[])((BuiltInResourceData)resElem.ResourceData).Data;
					return string.Format("{0} bytes", ary.Length);

				default:
					var binData = resElem.ResourceData as BinaryResourceData;
					if (binData != null)
						return string.Format("{0} bytes, Type = {1}", binData.Data.Length, binData.TypeName);
					return resElem.ResourceData.ToString();
				}
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

		public ulong FileOffset {
			get {
				FileOffset fo;
				GetModuleOffset(out fo);
				return (ulong)fo;
			}
		}

		public ulong Length {
			get { return (ulong)(resElem.ResourceData.EndOffset - resElem.ResourceData.StartOffset); }
		}

		ModuleDefMD GetModuleOffset(out FileOffset fileOffset) {
			fileOffset = 0;

			var module = GetModule(this) as ModuleDefMD;//TODO: Support CorModuleDef
			if (module == null)
				return null;

			fileOffset = resElem.ResourceData.StartOffset;
			return module;
		}

		protected ResourceElementTreeNode(ResourceElement resElem) {
			this.resElem = resElem;
		}

		public override FilterResult Filter(FilterSettings settings) {
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}

		protected sealed override void Write(ITextOutput output, Language language) {
			output.WriteFilename(resElem.Name);
		}

		public sealed override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			Decompile(language, output);
		}

		public virtual void Decompile(Language language, ITextOutput output) {
			language.WriteComment(output, string.Empty);
			output.WriteOffsetComment(this);
			output.WriteDefinition(UIUtils.CleanUpName(Name), this, TextTokenType.Comment);
			output.Write(string.Format(" = {0}", ValueString), TextTokenType.Comment);
			output.WriteLine();
		}

		public IEnumerable<ResourceData> GetResourceData(ResourceDataType type) {
			switch (type) {
			case ResourceDataType.Deserialized:
				return GetDeserialized();
			case ResourceDataType.Serialized:
				return GetSerialized();
			default:
				throw new InvalidOperationException();
			}
		}

		protected abstract IEnumerable<ResourceData> GetDeserialized();

		IEnumerable<ResourceData> GetSerialized() {
			var outStream = new MemoryStream();
			var writer = new BinaryWriter(outStream);

			var builtin = resElem.ResourceData as BuiltInResourceData;
			var bin = resElem.ResourceData as BinaryResourceData;
			switch (resElem.ResourceData.Code) {
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
			yield return new ResourceData(resElem.Name, () => outStream);
		}

		public virtual string CheckCanUpdateData(ResourceElement newResElem) {
			if (resElem.ResourceData.Code.FixUserType() != newResElem.ResourceData.Code.FixUserType())
				return "Resource type can't be changed";

			return string.Empty;
		}

		public virtual void UpdateData(ResourceElement newResElem) {
			resElem = newResElem;
		}

		// Used by the searcher. Should only return a string if the data is text or compiled text.
		// I.e., null should be returned if it's an Int32, but a string if it's eg. an XML doc.
		public virtual string GetStringContents() {
			return null;
		}
	}
}
