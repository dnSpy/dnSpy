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
using System.Text;
using System.Text.RegularExpressions;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;

namespace dnSpy.Hex.Files.DotNet {
	sealed class DotNetMultiFileResourcesImpl : DotNetMultiFileResources {
		public override DotNetMultiFileResourceHeaderData Header { get; }

		enum DataKind {
			UnicodeNameAndOffset,
			ResourceInfo,
		}

		abstract class Data {
			public DataKind Kind { get; }
			public HexSpan Span { get; }
			protected Data(DataKind kind, HexSpan span) {
				Kind = kind;
				Span = span;
			}
		}

		sealed class UnicodeNameAndOffsetData : Data {
			public ResourceInfo Owner { get; }
			public Bit7String Bit7String { get; }
			public UnicodeNameAndOffsetData(ResourceInfo owner, Bit7String bit7String)
				: base(DataKind.UnicodeNameAndOffset, HexSpan.FromBounds(bit7String.FullSpan.Start, bit7String.FullSpan.End + 4)) {
				Owner = owner;
				Bit7String = bit7String;
			}
		}

		sealed class ResourceInfoData : Data {
			public ResourceInfo Owner { get; }
			public ResourceInfoData(ResourceInfo owner, HexSpan span)
				: base(DataKind.ResourceInfo, span) {
				Owner = owner;
			}
		}

		readonly Data[] dataArray;

		sealed class DataSorter : IComparer<Data> {
			public static readonly DataSorter Instance = new DataSorter();
			public int Compare(Data x, Data y) {
				int c = x.Span.Start.CompareTo(y.Span.Start);
				if (c != 0)
					return c;
				return x.Span.Length.CompareTo(y.Span.Length);
			}
		}

		DotNetMultiFileResourcesImpl(HexBufferFile file, Bit7String? resourceTypeSpan, Bit7String? resourceSetTypeSpan, HexPosition versionPosition, HexSpan paddingSpan, Bit7String[] typeNames, int numResources, HexPosition dataSectionPosition, HexPosition nameSectionPosition)
			: base(file) {
			var headerSpan = new HexBufferSpan(file.Buffer, HexSpan.FromBounds(file.Span.Start, nameSectionPosition));
			Header = new DotNetMultiFileResourceHeaderDataImpl(headerSpan, resourceTypeSpan, resourceSetTypeSpan, versionPosition, paddingSpan, typeNames, numResources);
			ResourceInfo[] resourceInfos;
			dataArray = CreateDataArray(typeNames, numResources, paddingSpan.End, dataSectionPosition, nameSectionPosition, out resourceInfos);

			var files = new List<BufferFileOptions>(resourceInfos.Length);
			var tags = new string[] { PredefinedBufferFileTags.DotNetMultiFileResource };
			foreach (var info in resourceInfos) {
				var data = info.ResData;
				if (data == null)
					continue;
				if (data.NestedFileData.IsEmpty)
					continue;
				if (!IsNestedFile(data.TypeCode))
					continue;
				var name = Encoding.Unicode.GetString(File.Buffer.ReadBytes(info.UnicodeName.StringSpan));
				var filteredName = NameUtils.FilterName(name);
				files.Add(new BufferFileOptions(data.NestedFileData, filteredName, string.Empty, tags));
			}
			if (files.Count > 0)
				File.CreateFiles(files.ToArray());
		}

		bool IsNestedFile(ResourceTypeCode code) {
			switch (code) {
			case ResourceTypeCode.Null:
			case ResourceTypeCode.String:
			case ResourceTypeCode.Boolean:
			case ResourceTypeCode.Char:
			case ResourceTypeCode.Byte:
			case ResourceTypeCode.SByte:
			case ResourceTypeCode.Int16:
			case ResourceTypeCode.UInt16:
			case ResourceTypeCode.Int32:
			case ResourceTypeCode.UInt32:
			case ResourceTypeCode.Int64:
			case ResourceTypeCode.UInt64:
			case ResourceTypeCode.Single:
			case ResourceTypeCode.Double:
			case ResourceTypeCode.Decimal:
			case ResourceTypeCode.DateTime:
			case ResourceTypeCode.TimeSpan:
				return false;
			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				return true;
			default:
				return code >= ResourceTypeCode.UserTypes;
			}
		}

		sealed class ResourceInfo {
			public int Index { get; }
			public HexSpan UnicodeNameAndOffsetSpan => HexSpan.FromBounds(UnicodeName.FullSpan.Start, UnicodeName.FullSpan.End + 4);
			public Bit7String UnicodeName { get; }
			public HexPosition DataStart { get; }
			public ResData ResData => resData;

			public void SetData(ResData newData) => resData = newData;
			ResData resData;

			public ResourceInfo(int index, Bit7String unicodeName, HexPosition dataPos) {
				Index = index;
				UnicodeName = unicodeName;
				DataStart = dataPos;
			}
		}

		Data[] CreateDataArray(Bit7String[] typeNames, int numResources, HexPosition nameHashesPosition, HexPosition dataSectionPosition, HexPosition nameSectionPosition, out ResourceInfo[] resourceInfos) {
			var list = new List<Data>();
			var elems = new List<ResourceInfo>();

			var buffer = File.Buffer;
			var namePosition = nameHashesPosition + (ulong)numResources * 4;
			for (int i = 0; i < numResources; i++, namePosition += 4) {
				var stringPos = nameSectionPosition + buffer.ReadUInt32(namePosition);
				var bit7String = ReadBit7String(File.Buffer, ref stringPos, File.Span.End);
				if (bit7String == null)
					continue;
				if (stringPos + 4 > File.Span.End)
					continue;
				var offs = buffer.ReadInt32(stringPos);
				if (offs < 0 && (uint)-offs > dataSectionPosition)
					continue;
				var dataPos = dataSectionPosition + offs;
				if (dataPos > File.Span.End)
					continue;
				elems.Add(new ResourceInfo(i, bit7String.Value, dataPos));
			}
			elems.Sort((a, b) => a.DataStart.CompareTo(b.DataStart));
			for (int i = 0; i < elems.Count; i++) {
				var elem = elems[i];
				var endPos = i == elems.Count - 1 ? File.Span.End : elems[i + 1].DataStart;
				var resData = ReadData(typeNames, elem.DataStart, endPos);
				if (resData == null || resData.DataSpan.End > endPos)
					continue;
				elem.SetData(resData);
			}

			foreach (var elem in elems) {
				list.Add(new UnicodeNameAndOffsetData(elem, elem.UnicodeName));
				if (elem.ResData != null)
					list.Add(new ResourceInfoData(elem, elem.ResData.FullSpan));
			}

			list.Sort(DataSorter.Instance);
			resourceInfos = elems.ToArray();
			return list.ToArray();
		}

		abstract class ResData {
			public HexSpan FullSpan => HexSpan.FromBounds(CodeSpan.Start, DataSpan.End);
			public HexSpan CodeSpan { get; }
			public HexSpan DataSpan { get; }
			public HexSpan NestedFileData { get; }
			public ResourceTypeCode TypeCode { get; }
			protected ResData(ResourceTypeCode typeCode, HexSpan codeSpan, HexSpan dataSpan, HexSpan nestedFileData) {
				CodeSpan = codeSpan;
				DataSpan = dataSpan;
				TypeCode = typeCode;
				NestedFileData = nestedFileData;
			}
		}

		sealed class SimpleResData : ResData {
			public SimpleResData(ResourceTypeCode typeCode, HexSpan codeSpan, HexPosition position, uint length)
				: base(typeCode, codeSpan, new HexSpan(position, length), new HexSpan(position, length)) {
			}
		}

		sealed class StringResData : ResData {
			public Bit7String Utf8StringValue { get; }
			public StringResData(ResourceTypeCode typeCode, HexSpan codeSpan, HexSpan dataSpan, Bit7String utf8StringValue)
				: base(typeCode, codeSpan, dataSpan, utf8StringValue.StringSpan) {
				Utf8StringValue = utf8StringValue;
			}
		}

		sealed class ArrayResData : ResData {
			public ArrayResData(ResourceTypeCode typeCode, HexSpan codeSpan, HexSpan dataSpan, HexSpan nestedFileData)
				: base(typeCode, codeSpan, dataSpan, nestedFileData) {
			}
		}

		sealed class TypeResData : ResData {
			public Bit7String Utf8TypeName { get; }
			public TypeResData(ResourceTypeCode typeCode, HexSpan codeSpan, HexSpan dataSpan, Bit7String utf8TypeName)
				: base(typeCode, codeSpan, dataSpan, dataSpan) {
				Utf8TypeName = utf8TypeName;
			}
		}

		ResData ReadData(Bit7String[] typeNames, HexPosition position, HexPosition endPosition) {
			var start = position;
			var codeTmp = Read7BitEncodedInt32(File.Buffer, ref position);
			if (codeTmp == null)
				return null;
			var codeSpan = HexSpan.FromBounds(start, position);
			uint code = (uint)codeTmp.Value;
			var typeCode = (ResourceTypeCode)code;
			switch (typeCode) {
			case ResourceTypeCode.Null:		return new SimpleResData(typeCode, codeSpan, position, 0);
			case ResourceTypeCode.Boolean:	return new SimpleResData(typeCode, codeSpan, position, 1);
			case ResourceTypeCode.Char:		return new SimpleResData(typeCode, codeSpan, position, 2);
			case ResourceTypeCode.Byte:		return new SimpleResData(typeCode, codeSpan, position, 1);
			case ResourceTypeCode.SByte:	return new SimpleResData(typeCode, codeSpan, position, 1);
			case ResourceTypeCode.Int16:	return new SimpleResData(typeCode, codeSpan, position, 2);
			case ResourceTypeCode.UInt16:	return new SimpleResData(typeCode, codeSpan, position, 2);
			case ResourceTypeCode.Int32:	return new SimpleResData(typeCode, codeSpan, position, 4);
			case ResourceTypeCode.UInt32:	return new SimpleResData(typeCode, codeSpan, position, 4);
			case ResourceTypeCode.Int64:	return new SimpleResData(typeCode, codeSpan, position, 8);
			case ResourceTypeCode.UInt64:	return new SimpleResData(typeCode, codeSpan, position, 8);
			case ResourceTypeCode.Single:	return new SimpleResData(typeCode, codeSpan, position, 4);
			case ResourceTypeCode.Double:	return new SimpleResData(typeCode, codeSpan, position, 8);
			case ResourceTypeCode.Decimal:	return new SimpleResData(typeCode, codeSpan, position, 16);
			case ResourceTypeCode.DateTime: return new SimpleResData(typeCode, codeSpan, position, 8);
			case ResourceTypeCode.TimeSpan:	return new SimpleResData(typeCode, codeSpan, position, 8);

			case ResourceTypeCode.String:
				var stringPos = position;
				var bit7String = ReadBit7String(File.Buffer, ref stringPos, endPosition);
				if (bit7String == null)
					return null;
				return new StringResData(typeCode, codeSpan, bit7String.Value.FullSpan, bit7String.Value);

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				uint length = File.Buffer.ReadUInt32(position);
				if (position + length + 4UL > endPosition)
					return null;
				return new ArrayResData(typeCode, codeSpan, new HexSpan(position, length + 4UL), new HexSpan(position + 4, length));

			default:
				int userTypeIndex = (int)(code - (uint)ResourceTypeCode.UserTypes);
				if ((uint)userTypeIndex >= (uint)typeNames.Length)
					return null;
				var userType = typeNames[userTypeIndex];
				return new TypeResData(typeCode, codeSpan, HexSpan.FromBounds(position, endPosition), userType);
			}
		}

		public static DotNetMultiFileResourcesImpl TryRead(HexBufferFile file) {
			try {
				return TryReadCore(file);
			}
			catch (OutOfMemoryException) {
				return null;
			}
		}

		static DotNetMultiFileResourcesImpl TryReadCore(HexBufferFile file) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (file.Span.Length < 0x1C)
				return null;
			var buffer = file.Buffer;
			var pos = file.Span.Start;
			if (buffer.ReadUInt32(pos) != 0xBEEFCACE)
				return null;

			int resMgrHeaderVersion = buffer.ReadInt32(pos + 4);
			int headerSize = buffer.ReadInt32(pos + 8);
			if (resMgrHeaderVersion < 0 || headerSize < 0)
				return null;
			pos += 0xC;

			Bit7String? resourceTypeSpan = null;
			Bit7String? resourceSetTypeSpan = null;
			if (resMgrHeaderVersion > 1)
				pos += headerSize;
			else {
				resourceTypeSpan = ReadBit7String(buffer, ref pos, file.Span.End);
				resourceSetTypeSpan = ReadBit7String(buffer, ref pos, file.Span.End);
				if (resourceTypeSpan == null || resourceSetTypeSpan == null)
					return null;
				var resourceType = Encoding.UTF8.GetString(buffer.ReadBytes(resourceTypeSpan.Value.StringSpan));
				if (!Regex.IsMatch(resourceType, @"^System\.Resources\.ResourceReader,\s*mscorlib,"))
					return null;
			}

			var versionPosition = pos;
			if (pos + 0x0C > file.Span.End)
				return null;
			uint version = buffer.ReadUInt32(pos);
			if (version != 2)
				return null;//TODO: Support version 1
			int numResources = buffer.ReadInt32(pos + 4);
			int numTypes = buffer.ReadInt32(pos + 8);
			if (numResources < 0 || numTypes < 0)
				return null;
			pos += 0x0C;
			var typeNames = new Bit7String[numTypes];
			for (int i = 0; i < typeNames.Length; i++) {
				var info = ReadBit7String(buffer, ref pos, file.Span.End);
				if (info == null)
					return null;
				typeNames[i] = info.Value;
			}

			var paddingStart = pos;
			pos = file.AlignUp(pos, 8);
			var paddingSpan = HexSpan.FromBounds(paddingStart, pos);
			if (pos + (ulong)numResources * 8 + 4 > file.Span.End)
				return null;
			pos += (ulong)numResources * 8;
			int dataSectionOffset = buffer.ReadInt32(pos);
			pos += 4;
			if (dataSectionOffset < 0 || dataSectionOffset < (pos - file.Span.Start))
				return null;
			// Use > and not >= in case it's an empty resource
			if (dataSectionOffset > file.Span.Length)
				return null;
			var dataSectionPosition = file.Span.Start + dataSectionOffset;
			var nameSectionPosition = pos;

			return new DotNetMultiFileResourcesImpl(file, resourceTypeSpan, resourceSetTypeSpan, versionPosition, paddingSpan, typeNames, numResources, dataSectionPosition, nameSectionPosition);
		}

		static Bit7String? ReadBit7String(HexBuffer buffer, ref HexPosition position, HexPosition fileEnd) {
			var start = position;
			int? len = Read7BitEncodedInt32(buffer, ref position);
			if (len == null)
				return null;
			var lengthSpan = HexSpan.FromBounds(start, position);
			var stringEnd = lengthSpan.End + len.Value;
			if (stringEnd > fileEnd)
				return null;
			position = stringEnd;
			return new Bit7String(lengthSpan, HexSpan.FromBounds(lengthSpan.End, stringEnd));
		}

		static int? Read7BitEncodedInt32(HexBuffer buffer, ref HexPosition position) {
			uint val = 0;
			int bits = 0;
			for (int i = 0; i < 5; i++) {
				byte b = buffer.ReadByte(position++);
				val |= (uint)(b & 0x7F) << bits;
				if ((b & 0x80) == 0)
					return (int)val;
				bits += 7;
			}
			return null;
		}

		public override ComplexData GetStructure(HexPosition position) {
			if (!File.Span.Contains(position))
				return null;
			if (Header.Span.Span.Contains(position))
				return Header;

			var data = GetData(position);
			if (data != null)
				return GetStructure(data);

			return null;
		}

		ComplexData GetStructure(Data data) {
			switch (data.Kind) {
			case DataKind.UnicodeNameAndOffset:
				var tdata = (UnicodeNameAndOffsetData)data;
				return new MultiResourceUnicodeNameAndOffsetData(this, File.Buffer, tdata.Bit7String.LengthSpan, tdata.Bit7String.StringSpan);

			case DataKind.ResourceInfo:
				var rdata = (ResourceInfoData)data;
				var info = rdata.Owner;
				var resData = info.ResData;
				if (resData == null)
					return null;
				var span = new HexBufferSpan(File.Buffer, resData.FullSpan);
				string resName = Encoding.Unicode.GetString(File.Buffer.ReadBytes(info.UnicodeName.StringSpan));
				string typeName = null;
				var typeData = resData as TypeResData;
				if (typeData != null)
					typeName = Encoding.UTF8.GetString(File.Buffer.ReadBytes(typeData.Utf8TypeName.StringSpan));
				var resInfo = new MultiResourceInfo(resName, resData.TypeCode, typeName);
				switch (resData.TypeCode) {
				case ResourceTypeCode.String:
					var sdata = (StringResData)resData;
					return new MultiResourceStringDataHeaderData(this, resInfo, span, sdata.Utf8StringValue.LengthSpan, sdata.Utf8StringValue.StringSpan);

				case ResourceTypeCode.ByteArray:
				case ResourceTypeCode.Stream:
					return new MultiResourceArrayDataHeaderData(this, resInfo, span, resData.DataSpan.Start);

				default:
					return new MultiResourceSimplDataHeaderData(this, resInfo, span, resData.DataSpan.Start);
				}

			default: throw new InvalidOperationException();
			}
		}

		Data GetData(HexPosition position) {
			var array = dataArray;
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var info = array[index];
				if (position < info.Span.Start)
					hi = index - 1;
				else if (position >= info.Span.End)
					lo = index + 1;
				else
					return array[index];
			}
			return null;
		}
	}

	struct Bit7String {
		public HexSpan FullSpan => HexSpan.FromBounds(LengthSpan.Start, StringSpan.End);
		public HexSpan LengthSpan { get; }
		public HexSpan StringSpan { get; }
		public Bit7String(HexSpan lengthSpan, HexSpan stringSpan) {
			if (lengthSpan.End != stringSpan.Start)
				throw new ArgumentOutOfRangeException(nameof(stringSpan));
			LengthSpan = lengthSpan;
			StringSpan = stringSpan;
		}
	}
}
