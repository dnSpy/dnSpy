/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
		const string KeyFieldName = "key";
		const string ValueFieldName = "value";

		public static bool IsKeyValuePair(DmdType type) {
			if (!type.IsConstructedGenericType)
				return false;
			type = type.GetGenericTypeDefinition();
			return type == type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Collections_Generic_KeyValuePair_T2, isOptional: true);
		}

		public static (DmdFieldInfo keyField, DmdFieldInfo valueField) TryGetFields(DmdType type) {
			Debug.Assert(IsKeyValuePair(type));

			DmdFieldInfo keyField = null;
			DmdFieldInfo valueField = null;
			var fields = type.DeclaredFields;
			for (int i = 0; i < fields.Count; i++) {
				var field = fields[i];
				if (field.IsStatic || field.IsLiteral)
					continue;
				switch (field.Name) {
				case KeyFieldName:
					if ((object)keyField != null)
						return (null, null);
					keyField = field;
					break;
				case ValueFieldName:
					if ((object)valueField != null)
						return (null, null);
					valueField = field;
					break;
				default:
					return (null, null);
				}
			}

			if ((object)keyField == null || (object)valueField == null)
				return (null, null);
			return (keyField, valueField);
		}
	}
}
