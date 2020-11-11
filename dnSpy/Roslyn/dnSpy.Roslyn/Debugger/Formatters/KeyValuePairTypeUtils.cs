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
	static class KeyValuePairTypeUtils {
		public static bool IsKeyValuePair(DmdType type) {
			if (type.MetadataName != "KeyValuePair`2" || type.MetadataNamespace != "System.Collections.Generic")
				return false;
			if (!type.IsConstructedGenericType)
				return false;
			type = type.GetGenericTypeDefinition();
			return type == type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Collections_Generic_KeyValuePair_T2, isOptional: true);
		}

		public static (DmdFieldInfo? keyField, DmdFieldInfo? valueField) TryGetFields(DmdType type) {
			Debug.Assert(IsKeyValuePair(type));
			return TryGetFields(type, KnownMemberNames.KeyValuePair_Key_FieldName, KnownMemberNames.KeyValuePair_Value_FieldName);
		}

		public static (DmdFieldInfo? keyField, DmdFieldInfo? valueField) TryGetFields(DmdType type, string keyFieldName, string valueFieldName) {
			DmdFieldInfo? keyField = null;
			DmdFieldInfo? valueField = null;
			var fields = type.DeclaredFields;
			for (int i = 0; i < fields.Count; i++) {
				var field = fields[i];
				if (field.IsStatic || field.IsLiteral)
					continue;
				if (field.Name == keyFieldName) {
					if (keyField is not null)
						return (null, null);
					keyField = field;
				}
				else if (field.Name == valueFieldName) {
					if (valueField is not null)
						return (null, null);
					valueField = field;
				}
				else
					return (null, null);
			}

			if (keyField is null || valueField is null)
				return (null, null);
			return (keyField, valueField);
		}
	}
}
