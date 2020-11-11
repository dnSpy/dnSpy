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

using System.Diagnostics;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters {
	static class NullableTypeUtils {
		public static (DmdFieldInfo? hasValueField, DmdFieldInfo? valueField) TryGetNullableFields(DmdType type) {
			Debug.Assert(type.IsNullable);

			DmdFieldInfo? hasValueField = null;
			DmdFieldInfo? valueField = null;
			var fields = type.DeclaredFields;
			for (int i = 0; i < fields.Count; i++) {
				var field = fields[i];
				if (field.IsStatic || field.IsLiteral)
					continue;
				switch (field.Name) {
				case KnownMemberNames.Nullable_HasValue_FieldName:
				case KnownMemberNames.Nullable_HasValue_FieldName_Mono:
					if (hasValueField is not null)
						return (null, null);
					hasValueField = field;
					break;
				case KnownMemberNames.Nullable_Value_FieldName:
					if (valueField is not null)
						return (null, null);
					valueField = field;
					break;
				default:
					return (null, null);
				}
			}

			if (hasValueField is null || valueField is null)
				return (null, null);
			return (hasValueField, valueField);
		}
	}
}
