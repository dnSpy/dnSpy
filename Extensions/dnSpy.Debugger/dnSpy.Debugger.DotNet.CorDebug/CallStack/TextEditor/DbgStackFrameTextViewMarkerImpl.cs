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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.CallStack.TextEditor;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Code;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.DotNet.CorDebug.CallStack.TextEditor {
	[Export(typeof(DbgStackFrameTextViewMarker))]
	sealed class DbgStackFrameTextViewMarkerImpl : DbgStackFrameTextViewMarker {
		readonly IModuleIdProvider moduleIdProvider;
		Dictionary<ModuleTokenId, List<uint>> activeStatements;

		[ImportingConstructor]
		DbgStackFrameTextViewMarkerImpl(IModuleIdProvider moduleIdProvider) {
			this.moduleIdProvider = moduleIdProvider;
			activeStatements = new Dictionary<ModuleTokenId, List<uint>>();
		}

		public override IEnumerable<SnapshotSpan> GetFrameSpans(ITextView textView, NormalizedSnapshotSpanCollection spans) {
			if (activeStatements.Count == 0)
				yield break;

			var docViewer = textView.TextBuffer.TryGetDocumentViewer();
			if (docViewer is null)
				yield break;

			var methodDebugService = docViewer.TryGetMethodDebugService();
			if (methodDebugService is null)
				yield break;

			var snapshot = spans[0].Snapshot;
			MethodDef? method = null;
			List<uint>? ilOffsets = null;
			foreach (var span in spans) {
				foreach (var info in methodDebugService.GetStatementsByTextSpan(span.Span)) {
					if (info.Method != method) {
						method = info.Method;
						var moduleTokenId = new ModuleTokenId(moduleIdProvider.Create(method.Module), method.MDToken);
						if (!activeStatements.TryGetValue(moduleTokenId, out ilOffsets))
							continue;
					}
					else if (ilOffsets is null)
						continue;
					var textSpan = info.Statement.TextSpan;
					if (textSpan.End > snapshot.Length)
						yield break;// Old data, but we'll get called again
					var ilSpan = info.Statement.ILSpan;
					foreach (uint ilOffset in ilOffsets) {
						if (ilOffset >= ilSpan.Start && ilOffset < ilSpan.End)
							yield return new SnapshotSpan(snapshot, textSpan.Start, textSpan.Length);
					}
				}
			}
		}

		public override void OnNewFrames(ReadOnlyCollection<DbgStackFrame> frames) {
			var dict = new Dictionary<ModuleTokenId, List<uint>>();

			// The first statement shouldn't be marked (it's always marked by default by other code
			// since it's the current statement)
			for (int i = 1; i < frames.Count; i++) {
				switch (frames[i].Location) {
				case DbgDotNetNativeCodeLocation nativeLoc:
					switch (nativeLoc.ILOffsetMapping) {
					case DbgILOffsetMapping.Exact:
					case DbgILOffsetMapping.Approximate:
						break;

					case DbgILOffsetMapping.Prolog:
					case DbgILOffsetMapping.Epilog:
					case DbgILOffsetMapping.Unknown:
					case DbgILOffsetMapping.NoInfo:
					case DbgILOffsetMapping.UnmappedAddress:
					default:
						continue;
					}

					var key = new ModuleTokenId(nativeLoc.Module, nativeLoc.Token);
					if (!dict.TryGetValue(key, out var list))
						dict.Add(key, list = new List<uint>());
					uint offset = nativeLoc.Offset;
					// The list should be small so Contains() should be fast
					if (!list.Contains(offset))
						list.Add(offset);
					break;

				default:
					continue;
				}
			}

			activeStatements = dict;
		}
	}
}
