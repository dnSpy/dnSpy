/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.Collections.Generic;
using dnSpy.Files;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;

namespace dnSpy {
	static class SourceCodeMappingUtils {
		public static IList<SourceCodeMapping> Find(DecompilerTextView textView, int line, int column) {
			if (textView == null)
				return new SourceCodeMapping[0];
			return Find(textView.CodeMappings, line, column);
		}

		public static IList<SourceCodeMapping> Find(Dictionary<SerializedDnSpyToken, MemberMapping> cm, int line, int column) {
			if (line <= 0)
				return new SourceCodeMapping[0];
			if (cm == null || cm.Count == 0)
				return new SourceCodeMapping[0];

			var bp = FindByLineColumn(cm, line, column);
			if (bp == null && column != 0)
				bp = FindByLineColumn(cm, line, 0);
			if (bp == null)
				bp = GetClosest(cm, line);

			if (bp != null)
				return bp;
			return new SourceCodeMapping[0];
		}

		static List<SourceCodeMapping> FindByLineColumn(Dictionary<SerializedDnSpyToken, MemberMapping> cm, int line, int column) {
			List<SourceCodeMapping> list = null;
			foreach (var storageEntry in cm.Values) {
				var bp = storageEntry.GetInstructionByLineNumber(line, column);
				if (bp != null) {
					if (list == null)
						list = new List<SourceCodeMapping>();
					list.Add(bp);
				}
			}
			return list;
		}

		static List<SourceCodeMapping> GetClosest(Dictionary<SerializedDnSpyToken, MemberMapping> cm, int line) {
			List<SourceCodeMapping> list = new List<SourceCodeMapping>();
			foreach (var entry in cm.Values) {
				SourceCodeMapping map = null;
				foreach (var m in entry.MemberCodeMappings) {
					if (line > m.EndLocation.Line)
						continue;
					if (map == null || m.StartLocation < map.StartLocation)
						map = m;
				}
				if (map != null) {
					if (list.Count == 0)
						list.Add(map);
					else if (map.StartLocation == list[0].StartLocation)
						list.Add(map);
					else if (map.StartLocation < list[0].StartLocation) {
						list.Clear();
						list.Add(map);
					}
				}
			}

			if (list.Count == 0)
				return null;
			return list;
		}
	}
}
