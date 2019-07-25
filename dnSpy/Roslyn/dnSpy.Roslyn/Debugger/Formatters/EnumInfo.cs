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
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters {
	sealed class EnumInfo {
		public bool HasFlagsAttribute { get; }
		public EnumFieldInfo[] FieldInfos { get; }

		EnumInfo(bool hasFlagsAttribute, EnumFieldInfo[] fieldInfos) {
			HasFlagsAttribute = hasFlagsAttribute;
			FieldInfos = fieldInfos;
		}

		public static EnumInfo GetEnumInfo(DmdType type) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			if (!type.IsEnum)
				throw new ArgumentException();
			if (type.TryGetData(out EnumInfo? info))
				return info;
			return GetAndCreateEnumInfo(type);
		}

		static EnumInfo GetAndCreateEnumInfo(DmdType type) {
			var result = CreateEnumInfo(type);
			return type.GetOrCreateData(() => result);
		}

		static EnumInfo CreateEnumInfo(DmdType type) {
			bool hasFlagsAttribute = type.IsDefined("System.FlagsAttribute", inherit: false);
			var fields = type.DeclaredFields;
			int count = fields.Count - 1;
			if (count <= 0)
				return new EnumInfo(hasFlagsAttribute, Array.Empty<EnumFieldInfo>());

			var infos = new EnumFieldInfo[count];
			int w = 0;
			for (int i = 0; i < fields.Count; i++) {
				var field = fields[i];
				if (!field.IsLiteral || !field.IsStatic)
					continue;
				if (!NumberUtils.TryConvertIntegerToUInt64ZeroExtend(field.GetRawConstantValue(), out var value))
					continue;
				if (w >= infos.Length)
					Array.Resize(ref infos, w + 1);
				infos[w++] = new EnumFieldInfo(field, value);
			}
			if (infos.Length != w)
				Array.Resize(ref infos, w);
			return new EnumInfo(hasFlagsAttribute, infos);
		}
	}

	readonly struct EnumFieldInfo {
		public DmdFieldInfo Field { get; }
		public ulong Value { get; }
		public EnumFieldInfo(DmdFieldInfo field, ulong value) {
			Field = field ?? throw new ArgumentNullException(nameof(field));
			Value = value;
		}
	}
}
