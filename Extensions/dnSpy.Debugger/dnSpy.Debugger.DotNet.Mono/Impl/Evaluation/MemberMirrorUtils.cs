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
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	static class MemberMirrorUtils {
		public static FieldInfoMirror GetMonoField(TypeMirror monoType, ReadOnlyCollection<DmdFieldInfo> fields, int fieldIndex) {
			var monoFields = monoType.GetFields();
			if (monoFields.Length != fields.Count)
				throw new InvalidOperationException();
			var res = monoFields[fieldIndex];
			Debug.Assert(res.Name == fields[fieldIndex].Name);
			return res;
		}

		public static FieldInfoMirror GetMonoField(TypeMirror monoType, DmdFieldInfo field) {
			var fields = field.DeclaringType!.DeclaredFields;
			int fieldIndex = GetIndex(fields, field);
			return GetMonoField(monoType, fields, fieldIndex);
		}

		static int GetIndex(ReadOnlyCollection<DmdFieldInfo> fields, DmdFieldInfo field) {
			for (int i = 0; i < fields.Count; i++) {
				var f = fields[i];
				if (f.MetadataToken == field.MetadataToken && f.Module == field.Module)
					return i;
			}
			throw new InvalidOperationException();
		}
	}
}
