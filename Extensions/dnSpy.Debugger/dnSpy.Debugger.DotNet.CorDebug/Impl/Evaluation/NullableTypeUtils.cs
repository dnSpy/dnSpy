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

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	static class NullableTypeUtils {
		const string HasValueFieldName = "hasValue";
		const string ValueFieldName = "value";

		public static (DmdFieldInfo hasValueField, DmdFieldInfo valueField) TryGetNullableFields(DmdType type) {
			Debug.Assert(type.IsNullable);

			DmdFieldInfo hasValueField = null;
			DmdFieldInfo valueField = null;
			var fields = type.DeclaredFields;
			for (int i = 0; i < fields.Count; i++) {
				var field = fields[i];
				if (field.IsStatic || field.IsLiteral)
					continue;
				switch (field.Name) {
				case HasValueFieldName:
					if ((object)hasValueField != null)
						return (null, null);
					hasValueField = field;
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

			if ((object)hasValueField == null || (object)valueField == null)
				return (null, null);
			return (hasValueField, valueField);
		}
	}
}
