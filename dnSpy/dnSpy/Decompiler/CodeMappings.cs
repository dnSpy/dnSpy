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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Plugin;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Decompiler {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforePlugins)]
	sealed class CodeMappingsLoader : IAutoLoaded {
		[ImportingConstructor]
		CodeMappingsLoader(ITextEditorUIContextManager textEditorUIContextManager) {
			textEditorUIContextManager.Add(OnTextEditorEvent, TextEditorUIContextManagerConstants.ORDER_ASMEDITOR_CODEMAPPINGSCREATOR);
		}

		void OnTextEditorEvent(TextEditorUIContextListenerEvent @event, ITextEditorUIContext uiContext, object data) {
			if (@event == TextEditorUIContextListenerEvent.NewContent)
				AddCodeMappings(uiContext, data as DnSpyTextOutputResult);
		}

		void AddCodeMappings(ITextEditorUIContext uiContext, DnSpyTextOutputResult result) {
			if (result == null)
				return;
			var cm = new CodeMappings(result.MemberMappings);
			uiContext.AddOutputData(CodeMappingsConstants.CodeMappingsKey, cm);
		}
	}

	sealed class CodeMappings : ICodeMappings {
		readonly IList<MemberMapping> memberMappings;

		public int Count => memberMappings.Count;

		public CodeMappings() {
			this.memberMappings = Array.Empty<MemberMapping>();
		}

		public CodeMappings(IList<MemberMapping> mappings) {
			if (mappings == null)
				throw new ArgumentNullException(nameof(mappings));
			this.memberMappings = mappings;
		}

		public IList<SourceCodeMapping> Find(int line, int column) {
			if (line < 0)
				return Array.Empty<SourceCodeMapping>();
			if (memberMappings.Count == 0)
				return Array.Empty<SourceCodeMapping>();

			var bp = FindByLineColumn(line, column);
			if (bp == null && column >= 0)
				bp = FindByLineColumn(line, 0);
			if (bp == null)
				bp = GetClosest(line);

			if (bp != null)
				return bp;
			return Array.Empty<SourceCodeMapping>();
		}

		List<SourceCodeMapping> FindByLineColumn(int line, int column) {
			List<SourceCodeMapping> list = null;
			foreach (var storageEntry in memberMappings) {
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
			var list = new List<SourceCodeMapping>();
			foreach (var entry in memberMappings) {
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

		public SourceCodeMapping Find(MethodDef method, uint ilOffset) {
			foreach (var entry in memberMappings) {
				if (entry.Method != method)
					continue;
				foreach (var m in entry.MemberCodeMappings) {
					if (m.ILRange.From <= ilOffset && ilOffset < m.ILRange.To)
						return m;
				}
			}
			return null;
		}
	}
}
