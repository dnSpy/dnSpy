/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Linq;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Debugger {
	[ExportDocumentViewerListener(DocumentViewerListenerConstants.ORDER_DEBUGGER_CODEMAPPINGSCREATOR)]
	sealed class CodeMappingsLoader : IDocumentViewerListener {
		readonly ISerializedDnModuleCreator serializedDnModuleCreator;

		[ImportingConstructor]
		CodeMappingsLoader(IDocumentViewerService documentViewerService, ISerializedDnModuleCreator serializedDnModuleCreator) {
			this.serializedDnModuleCreator = serializedDnModuleCreator;
		}

		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.NewContent)
				AddCodeMappings(e.DocumentViewer, ((DocumentViewerNewContentEventArgs)e).OutputResult);
		}

		void AddCodeMappings(IDocumentViewer documentViewer, DnSpyTextOutputResult outputResult) {
			if (outputResult == null)
				return;
			var cm = new CodeMappings(outputResult.MemberMappings, serializedDnModuleCreator);
			documentViewer.AddOutputData(CodeMappingsKey, cm);
		}
		internal static readonly object CodeMappingsKey = new object();
	}

	static class CodeMappingsExtensions {
		public static CodeMappings GetCodeMappings(this IDocumentViewer self) => self.TryGetCodeMappings() ?? new CodeMappings();

		public static CodeMappings TryGetCodeMappings(this IDocumentViewer self) {
			if (self == null)
				return null;
			return (CodeMappings)self.GetOutputData(CodeMappingsLoader.CodeMappingsKey);
		}
	}

	sealed class CodeMappings {
		readonly Dictionary<SerializedDnToken, MemberMapping> dict;

		public int Count => dict.Count;

		public CodeMappings() {
			this.dict = new Dictionary<SerializedDnToken, MemberMapping>(0);
		}

		public CodeMappings(IList<MemberMapping> mappings, ISerializedDnModuleCreator serializedDnModuleCreator) {
			this.dict = new Dictionary<SerializedDnToken, MemberMapping>(mappings.Count);

			var serDict = new Dictionary<ModuleDef, SerializedDnModule>();
			foreach (var m in mappings) {
				var module = m.Method.Module;
				if (module == null)
					continue;

				SerializedDnModule serMod;
				if (!serDict.TryGetValue(module, out serMod)) {
					serMod = serializedDnModuleCreator.Create(module);
					serDict.Add(module, serMod);
				}
				var key = new SerializedDnToken(serMod, m.Method.MDToken);
				MemberMapping oldMm;
				if (this.dict.TryGetValue(key, out oldMm)) {
					if (m.MemberCodeMappings.Count < oldMm.MemberCodeMappings.Count)
						continue;
				}
				this.dict[key] = m;
			}
		}

		public IList<SourceCodeMapping> Find(int line, int column) {
			if (line < 0)
				return empty;
			if (dict.Count == 0)
				return empty;

			var bp = FindByLineColumn(line, column);
			if (bp == null && column >= 0)
				bp = FindByLineColumn(line, 0);
			if (bp == null)
				bp = GetClosest(line);

			if (bp != null)
				return bp;
			return empty;
		}
		static readonly SourceCodeMapping[] empty = new SourceCodeMapping[0];

		List<SourceCodeMapping> FindByLineColumn(int line, int column) {
			List<SourceCodeMapping> list = null;
			foreach (var storageEntry in dict.Values) {
				var bp = storageEntry.GetInstructionByLineNumber(line, column);
				if (bp != null) {
					if (list == null)
						list = new List<SourceCodeMapping>();
					list.Add(bp);
				}
			}
			return list == null ? null : list.Distinct().ToList();
		}

		List<SourceCodeMapping> GetClosest(int line) {
			List<SourceCodeMapping> list = new List<SourceCodeMapping>();
			foreach (var entry in dict.Values) {
				SourceCodeMapping map = null;
				foreach (var m in entry.MemberCodeMappings) {
					if (line > m.EndPosition.Line)
						continue;
					if (map == null || m.StartPosition < map.StartPosition)
						map = m;
				}
				if (map != null) {
					if (list.Count == 0)
						list.Add(map);
					else if (map.StartPosition == list[0].StartPosition)
						list.Add(map);
					else if (map.StartPosition < list[0].StartPosition) {
						list.Clear();
						list.Add(map);
					}
				}
			}

			if (list.Count == 0)
				return null;
			return list.Distinct().ToList();
		}

		public MemberMapping TryGetMapping(SerializedDnToken key) {
			MemberMapping mm;
			dict.TryGetValue(key, out mm);
			return mm;
		}
	}
}
