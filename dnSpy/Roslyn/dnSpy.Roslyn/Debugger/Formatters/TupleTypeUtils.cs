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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters {
	static class TupleTypeUtils {
		public static IEnumerable<(int tupleIndex, List<DmdFieldInfo>? fields)> GetTupleFields(DmdType type, int tupleArity) {
			Debug.Assert(tupleArity == TypeFormatterUtils.GetTupleArity(type));
			if (tupleArity <= 0) {
				yield return (-1, null);
				yield break;
			}
			var currentType = type;
			var fields = ObjectCache.AllocFieldInfoList1();
			var currentFields = ObjectCache.AllocFieldInfoList2();
			int tupleIndex = 0;
			for (;;) {
				fields.Add(null);
				currentFields.Clear();
				foreach (var field in currentType.DeclaredFields) {
					if (field.IsStatic || field.IsLiteral)
						continue;
					currentFields.Add(field);
				}
				if (currentFields.Count > sortedTupleFields.Length) {
					yield return (-1, null);
					yield break;
				}
				currentFields.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));
				for (int i = 0; i < currentFields.Count; i++) {
					var field = currentFields[i];
					if (field.Name != sortedTupleFields[i]) {
						yield return (-1, null);
						yield break;
					}
					fields[fields.Count - 1] = field;
					if (i + 1 != sortedTupleFields.Length) {
						if (tupleIndex >= tupleArity) {
							yield return (-1, null);
							yield break;
						}
						yield return (tupleIndex, fields)!;
						tupleIndex++;
					}
					else
						currentType = field.FieldType;
				}
				if (tupleIndex == tupleArity)
					break;
			}
			ObjectCache.FreeFieldInfoList1(ref fields);
			ObjectCache.FreeFieldInfoList2(ref currentFields);
		}

		static readonly string[] sortedTupleFields = new string[] {
			"Item1",
			"Item2",
			"Item3",
			"Item4",
			"Item5",
			"Item6",
			"Item7",
			"Rest",
		};
	}
}
