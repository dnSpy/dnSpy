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
using System.ComponentModel.Composition;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Plugin;
using dnSpy.Shared.UI.Decompiler;
using ICSharpCode.Decompiler;

namespace dnSpy.Debugger {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforePlugins)]
	sealed class CodeMappingsLoader : IAutoLoaded {
		readonly ISerializedDnModuleCreator serializedDnModuleCreator;

		[ImportingConstructor]
		CodeMappingsLoader(ITextEditorUIContextManager textEditorUIContextManager, ISerializedDnModuleCreator serializedDnModuleCreator) {
			this.serializedDnModuleCreator = serializedDnModuleCreator;
			textEditorUIContextManager.Add(OnTextEditorEvent, TextEditorUIContextManagerConstants.ORDER_DEBUGGER_CODEMAPPINGSCREATOR);
		}

		void OnTextEditorEvent(TextEditorUIContextListenerEvent @event, ITextEditorUIContext uiContext, object data) {
			if (@event == TextEditorUIContextListenerEvent.NewContent)
				AddCodeMappings(uiContext, data as AvalonEditTextOutput);
		}

		void AddCodeMappings(ITextEditorUIContext uiContext, AvalonEditTextOutput output) {
			if (output == null)
				return;
			var cm = new CodeMappings(output.DebuggerMemberMappings, serializedDnModuleCreator);
			uiContext.AddOutputData(CodeMappingsKey, cm);
		}
		internal static readonly object CodeMappingsKey = new object();
	}

	static class CodeMappingsExtensions {
		public static CodeMappings GetCodeMappings(this ITextEditorUIContext self) {
			return self.TryGetCodeMappings() ?? new CodeMappings();
		}

		public static CodeMappings TryGetCodeMappings(this ITextEditorUIContext self) {
			if (self == null)
				return null;
			return (CodeMappings)self.GetOutputData(CodeMappingsLoader.CodeMappingsKey);
		}
	}

	sealed class CodeMappings {
		readonly Dictionary<SerializedDnToken, MemberMapping> dict;

		public int Count {
			get { return dict.Count; }
		}

		public CodeMappings() {
			this.dict = new Dictionary<SerializedDnToken, MemberMapping>(0);
		}

		public CodeMappings(IList<MemberMapping> mappings, ISerializedDnModuleCreator serializedDnModuleCreator) {
			this.dict = new Dictionary<SerializedDnToken, MemberMapping>(mappings.Count);

			var serDict = new Dictionary<ModuleDef, SerializedDnModule>();
			foreach (var m in mappings) {
				var module = m.MethodDef.Module;
				if (module == null)
					continue;

				SerializedDnModule serMod;
				if (!serDict.TryGetValue(module, out serMod)) {
					serMod = serializedDnModuleCreator.Create(module);
					serDict.Add(module, serMod);
				}
				var key = new SerializedDnToken(serMod, m.MethodDef.MDToken);
				MemberMapping oldMm;
				if (this.dict.TryGetValue(key, out oldMm)) {
					if (m.MemberCodeMappings.Count < oldMm.MemberCodeMappings.Count)
						continue;
				}
				this.dict[key] = m;
			}
		}

		public IList<SourceCodeMapping> Find(int line, int column) {
			if (line <= 0)
				return new SourceCodeMapping[0];
			if (dict.Count == 0)
				return new SourceCodeMapping[0];

			var bp = FindByLineColumn(line, column);
			if (bp == null && column != 0)
				bp = FindByLineColumn(line, 0);
			if (bp == null)
				bp = GetClosest(line);

			if (bp != null)
				return bp;
			return new SourceCodeMapping[0];
		}

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
			return list;
		}

		List<SourceCodeMapping> GetClosest(int line) {
			List<SourceCodeMapping> list = new List<SourceCodeMapping>();
			foreach (var entry in dict.Values) {
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

		public MemberMapping TryGetMapping(SerializedDnToken key) {
			MemberMapping mm;
			dict.TryGetValue(key, out mm);
			return mm;
		}
	}
}
