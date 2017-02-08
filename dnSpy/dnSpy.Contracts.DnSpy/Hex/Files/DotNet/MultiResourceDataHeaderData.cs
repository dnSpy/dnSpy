/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using System.Text;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// Multi file resource element info
	/// </summary>
	public struct MultiResourceInfo {
		/// <summary>
		/// Gets the resource name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the type code
		/// </summary>
		public ResourceTypeCode TypeCode { get; }

		/// <summary>
		/// Gets the user type name if <see cref="TypeCode"/> is &gt;= <see cref="ResourceTypeCode.UserTypes"/>
		/// </summary>
		public string UserTypeName { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="typeCode">Type code</param>
		/// <param name="userTypeName">User type or null if it's not a <see cref="ResourceTypeCode.UserTypes"/></param>
		public MultiResourceInfo(string name, ResourceTypeCode typeCode, string userTypeName) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			Name = name;
			TypeCode = typeCode;
			UserTypeName = userTypeName ?? string.Empty;
		}
	}

	/// <summary>
	/// Multi-file resource data header base class
	/// </summary>
	public abstract class MultiResourceDataHeaderData : StructureData {
		const string NAME = "MultiResourceDataHeader";

		/// <summary>
		/// Gets the owner <see cref="DotNetMultiFileResources"/> instance
		/// </summary>
		public DotNetMultiFileResources ResourceProvider { get; }

		/// <summary>
		/// Gets resource info
		/// </summary>
		public MultiResourceInfo ResourceInfo { get; }

		/// <summary>Type code</summary>
		public abstract StructField<ResourceTypeCodeData> TypeCode { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resourceProvider">Owner</param>
		/// <param name="resourceInfo">Resource info</param>
		/// <param name="span">Span</param>
		protected MultiResourceDataHeaderData(DotNetMultiFileResources resourceProvider, MultiResourceInfo resourceInfo, HexBufferSpan span)
			: base(NAME, span) {
			if (resourceProvider == null)
				throw new ArgumentNullException(nameof(resourceProvider));
			ResourceProvider = resourceProvider;
			ResourceInfo = resourceInfo;
		}
	}

	/// <summary>
	/// Multi-file resource data header (everything that's not a string, byte array, stream)
	/// </summary>
	public class MultiResourceSimplDataHeaderData : MultiResourceDataHeaderData {
		/// <summary>Type code</summary>
		public override StructField<ResourceTypeCodeData> TypeCode { get; }
		/// <summary>Content</summary>
		public StructField Content { get; }

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resourceProvider">Owner</param>
		/// <param name="resourceInfo">Resource info</param>
		/// <param name="span">Span</param>
		/// <param name="dataPosition">Position of data which immediately follows the 7-bit encoded type code</param>
		public MultiResourceSimplDataHeaderData(DotNetMultiFileResources resourceProvider, MultiResourceInfo resourceInfo, HexBufferSpan span, HexPosition dataPosition)
			: base(resourceProvider, resourceInfo, span) {
			// Don't use Contains() since data length could be 0
			if (dataPosition < span.Start || dataPosition > span.End)
				throw new ArgumentOutOfRangeException(nameof(dataPosition));
			var typeCodeSpan = new HexBufferSpan(span.Buffer, HexSpan.FromBounds(span.Start, dataPosition));
			TypeCode = new StructField<ResourceTypeCodeData>("TypeCode", ResourceTypeCodeData.Create(typeCodeSpan));

			var pos = typeCodeSpan.Span.Start;
			var typeCode = (ResourceTypeCode)(Utils.Read7BitEncodedInt32(span.Buffer, ref pos) ?? -1);
			var dataSpan = new HexBufferSpan(span.Buffer, HexSpan.FromBounds(dataPosition, span.End));
			switch (typeCode) {
			case ResourceTypeCode.String:
				Debug.Fail($"Use {nameof(MultiResourceStringDataHeaderData)}");
				goto default;

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				Debug.Fail($"Use {nameof(MultiResourceArrayDataHeaderData)}");
				goto default;

			case ResourceTypeCode.Boolean:
				Content = new StructField<BooleanData>("Content", new BooleanData(dataSpan));
				break;

			case ResourceTypeCode.Char:
				Content = new StructField<CharData>("Content", new CharData(dataSpan));
				break;

			case ResourceTypeCode.Byte:
				Content = new StructField<ByteData>("Content", new ByteData(dataSpan));
				break;

			case ResourceTypeCode.SByte:
				Content = new StructField<SByteData>("Content", new SByteData(dataSpan));
				break;

			case ResourceTypeCode.Int16:
				Content = new StructField<Int16Data>("Content", new Int16Data(dataSpan));
				break;

			case ResourceTypeCode.UInt16:
				Content = new StructField<UInt16Data>("Content", new UInt16Data(dataSpan));
				break;

			case ResourceTypeCode.Int32:
				Content = new StructField<Int32Data>("Content", new Int32Data(dataSpan));
				break;

			case ResourceTypeCode.UInt32:
				Content = new StructField<UInt32Data>("Content", new UInt32Data(dataSpan));
				break;

			case ResourceTypeCode.Int64:
				Content = new StructField<Int64Data>("Content", new Int64Data(dataSpan));
				break;

			case ResourceTypeCode.UInt64:
				Content = new StructField<UInt64Data>("Content", new UInt64Data(dataSpan));
				break;

			case ResourceTypeCode.Single:
				Content = new StructField<SingleData>("Content", new SingleData(dataSpan));
				break;

			case ResourceTypeCode.Double:
				Content = new StructField<DoubleData>("Content", new DoubleData(dataSpan));
				break;

			case ResourceTypeCode.Decimal:
				Content = new StructField<DecimalData>("Content", new DecimalData(dataSpan));
				break;

			case ResourceTypeCode.DateTime:
				Content = new StructField<DateTimeData>("Content", new DateTimeData(dataSpan));
				break;

			case ResourceTypeCode.TimeSpan:
				Content = new StructField<TimeSpanData>("Content", new TimeSpanData(dataSpan));
				break;

			case ResourceTypeCode.Null:
			default:
				Content = new StructField<VirtualArrayData<ByteData>>("Content", ArrayData.CreateVirtualByteArray(dataSpan));
				break;
			}

			Fields = new BufferField[] {
				TypeCode,
				Content,
			};
		}
	}

	/// <summary>
	/// Multi-file resource data header (strings)
	/// </summary>
	public class MultiResourceStringDataHeaderData : MultiResourceDataHeaderData {
		/// <summary>Type code</summary>
		public override StructField<ResourceTypeCodeData> TypeCode { get; }
		/// <summary>Content</summary>
		public StructField<Bit7EncodedStringData> Content { get; }

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resourceProvider">Owner</param>
		/// <param name="resourceInfo">Resource info</param>
		/// <param name="span">Span</param>
		/// <param name="lengthSpan">Span of 7-bit encoded string length</param>
		/// <param name="stringSpan">Span of string data (UTF-8)</param>
		public MultiResourceStringDataHeaderData(DotNetMultiFileResources resourceProvider, MultiResourceInfo resourceInfo, HexBufferSpan span, HexSpan lengthSpan, HexSpan stringSpan)
			: base(resourceProvider, resourceInfo, span) {
			if (!span.Span.Contains(lengthSpan))
				throw new ArgumentOutOfRangeException(nameof(lengthSpan));
			if (!span.Span.Contains(stringSpan))
				throw new ArgumentOutOfRangeException(nameof(stringSpan));
			if (lengthSpan.End != stringSpan.Start)
				throw new ArgumentOutOfRangeException(nameof(stringSpan));
			var typeCodeSpan = new HexBufferSpan(span.Buffer, HexSpan.FromBounds(span.Start, lengthSpan.Start));
			TypeCode = new StructField<ResourceTypeCodeData>("TypeCode", ResourceTypeCodeData.Create(typeCodeSpan));
			Content = new StructField<Bit7EncodedStringData>("Content", new Bit7EncodedStringData(span.Buffer, lengthSpan, stringSpan, Encoding.UTF8));
			Fields = new BufferField[] {
				TypeCode,
				Content,
			};
		}
	}

	/// <summary>
	/// Multi-file resource data header (byte arrays and streams)
	/// </summary>
	public class MultiResourceArrayDataHeaderData : MultiResourceDataHeaderData {
		/// <summary>Type code</summary>
		public override StructField<ResourceTypeCodeData> TypeCode { get; }
		/// <summary>Content length</summary>
		public StructField<UInt32Data> ContentLength { get; }
		/// <summary>Content</summary>
		public StructField<VirtualArrayData<ByteData>> Content { get; }

		/// <summary>
		/// Gets the fields
		/// </summary>
		protected override BufferField[] Fields { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="resourceProvider">Owner</param>
		/// <param name="resourceInfo">Resource info</param>
		/// <param name="span">Span</param>
		/// <param name="lengthPosition">Position of 32-bit content length which immediately follows the 7-bit encoded type code</param>
		public MultiResourceArrayDataHeaderData(DotNetMultiFileResources resourceProvider, MultiResourceInfo resourceInfo, HexBufferSpan span, HexPosition lengthPosition)
			: base(resourceProvider, resourceInfo, span) {
			var typeCodeSpan = new HexBufferSpan(span.Buffer, HexSpan.FromBounds(span.Start, lengthPosition));
			TypeCode = new StructField<ResourceTypeCodeData>("TypeCode", ResourceTypeCodeData.Create(typeCodeSpan));
			ContentLength = new StructField<UInt32Data>("ContentLength", new UInt32Data(span.Buffer, lengthPosition));
			var arraySpan = new HexBufferSpan(span.Buffer, HexSpan.FromBounds(lengthPosition + 4, span.End));
			Content = new StructField<VirtualArrayData<ByteData>>("Content", ArrayData.CreateVirtualByteArray(arraySpan));
			Fields = new BufferField[] {
				TypeCode,
				ContentLength,
				Content,
			};
		}
	}
}
