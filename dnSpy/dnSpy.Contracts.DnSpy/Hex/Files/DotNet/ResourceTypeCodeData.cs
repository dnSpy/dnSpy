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

using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex.Text;

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// <see cref="ResourceTypeCode"/> data
	/// </summary>
	public abstract class ResourceTypeCodeData : SimpleData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		protected ResourceTypeCodeData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Creates a <see cref="ResourceTypeCodeData"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public static ResourceTypeCodeData Create(HexBufferSpan span) {
			var pos = span.Start.Position;
			var value = Utils.Read7BitEncodedInt32(span.Buffer, ref pos);
			if (value is null || value.Value < (int)ResourceTypeCode.UserTypes)
				return new KnownResourceTypeCodeData(span);
			return new UserTypeResourceTypeCodeData(span);
		}
	}

	/// <summary>
	/// Known <see cref="ResourceTypeCode"/> data (not a user type)
	/// </summary>
	public sealed class KnownResourceTypeCodeData : ResourceTypeCodeData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		public KnownResourceTypeCodeData(HexBufferSpan span)
			: base(span) {
		}

		static readonly ReadOnlyCollection<EnumFieldInfo> typeCodeEnumFieldInfos = new ReadOnlyCollection<EnumFieldInfo>(new EnumFieldInfo[] {
			new EnumFieldInfo((ulong)ResourceTypeCode.Null, "Null"),
			new EnumFieldInfo((ulong)ResourceTypeCode.String, "String"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Boolean, "Boolean"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Char, "Char"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Byte, "Byte"),
			new EnumFieldInfo((ulong)ResourceTypeCode.SByte, "SByte"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Int16, "Int16"),
			new EnumFieldInfo((ulong)ResourceTypeCode.UInt16, "UInt16"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Int32, "Int32"),
			new EnumFieldInfo((ulong)ResourceTypeCode.UInt32, "UInt32"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Int64, "Int64"),
			new EnumFieldInfo((ulong)ResourceTypeCode.UInt64, "UInt64"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Single, "Single"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Double, "Double"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Decimal, "Decimal"),
			new EnumFieldInfo((ulong)ResourceTypeCode.DateTime, "DateTime"),
			new EnumFieldInfo((ulong)ResourceTypeCode.TimeSpan, "TimeSpan"),
			new EnumFieldInfo((ulong)ResourceTypeCode.ByteArray, "ByteArray"),
			new EnumFieldInfo((ulong)ResourceTypeCode.Stream, "Stream"),
		});

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) {
			var pos = Span.Start.Position;
			var value = Utils.Read7BitEncodedInt32(Span.Buffer, ref pos);
			if (value is null)
				formatter.WriteUnknownValue();
			else
				formatter.WriteEnum((ulong)value.Value, typeCodeEnumFieldInfos);
		}
	}

	/// <summary>
	/// <see cref="ResourceTypeCode.UserTypes"/> data
	/// </summary>
	public sealed class UserTypeResourceTypeCodeData : ResourceTypeCodeData {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		public UserTypeResourceTypeCodeData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) {
			var pos = Span.Start.Position;
			var value = Utils.Read7BitEncodedInt32(Span.Buffer, ref pos);
			if (value is null || value.Value < (int)ResourceTypeCode.UserTypes)
				formatter.WriteUnknownValue();
			else
				formatter.Write("UserType" + (value.Value - (int)ResourceTypeCode.UserTypes).ToString(), PredefinedClassifiedTextTags.EnumField);
		}
	}
}
