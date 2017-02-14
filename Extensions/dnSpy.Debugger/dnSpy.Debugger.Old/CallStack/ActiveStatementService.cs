/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.CallStack {
	//[Export(typeof(IViewTaggerProvider))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Debuggable)]
	[TagType(typeof(ITextMarkerTag))]
	sealed class ActiveStatementTaggerProvider : IViewTaggerProvider {
		readonly ActiveStatementService activeStatementService;

		[ImportingConstructor]
		ActiveStatementTaggerProvider(ActiveStatementService activeStatementService) => this.activeStatementService = activeStatementService;

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			if (textView.TextBuffer != buffer)
				return null;
			return textView.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new ActiveStatementTagger(activeStatementService, textView)) as ITagger<T>;
		}
	}

	sealed class ActiveStatementTagger : ITagger<ITextMarkerTag> {
		public ITextView TextView { get; }
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		readonly ActiveStatementService activeStatementService;

		public ActiveStatementTagger(ActiveStatementService activeStatementService, ITextView textView) {
			this.activeStatementService = activeStatementService;
			TextView = textView;
			TextView.Closed += TextView_Closed;
			activeStatementService.OnCreated(this);
		}

		public void RaiseTagsChanged(SnapshotSpan span) => TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
		public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans) =>
			activeStatementService.GetTags(this, spans);

		void TextView_Closed(object sender, EventArgs e) {
			TextView.Closed -= TextView_Closed;
			activeStatementService.OnDisposed(this);
		}
	}

	//[Export(typeof(ActiveStatementService))]
	sealed class ActiveStatementService {
		readonly IModuleIdProvider moduleIdProvider;
		readonly HashSet<ActiveStatementTagger> taggers;
		Dictionary<ModuleTokenId, List<uint>> activeStatements;

		[ImportingConstructor]
		ActiveStatementService(IModuleIdProvider moduleIdProvider) {
			this.moduleIdProvider = moduleIdProvider;
			taggers = new HashSet<ActiveStatementTagger>();
			activeStatements = new Dictionary<ModuleTokenId, List<uint>>();
		}

		public void OnCreated(ActiveStatementTagger tagger) => taggers.Add(tagger);
		public void OnDisposed(ActiveStatementTagger tagger) => taggers.Remove(tagger);

		public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(ActiveStatementTagger tagger, NormalizedSnapshotSpanCollection spans) {
			if (activeStatements.Count == 0 || spans.Count == 0)
				yield break;

			//TODO: This code shouldn't depend on IDocumentViewer
			var docViewer = tagger.TextView.TextBuffer.TryGetDocumentViewer();
			if (docViewer == null)
				yield break;

			var methodDebugService = docViewer.TryGetMethodDebugService();
			if (methodDebugService == null)
				yield break;

			var snapshot = spans[0].Snapshot;
			MethodDef method = null;
			List<uint> ilOffsets = null;
			foreach (var span in spans) {
				foreach (var info in methodDebugService.GetStatementsByTextSpan(span.Span)) {
					if (info.Method != method) {
						method = info.Method;
						var moduleTokenId = new ModuleTokenId(moduleIdProvider.Create(method.Module), method.MDToken);
						if (!activeStatements.TryGetValue(moduleTokenId, out ilOffsets))
							continue;
					}
					else if (ilOffsets == null)
						continue;
					var textSpan = info.Statement.TextSpan;
					if (textSpan.End > snapshot.Length)
						yield break;// Old data, but we'll get called again
					var binSpan = info.Statement.BinSpan;
					foreach (uint ilOffset in ilOffsets) {
						if (ilOffset >= binSpan.Start && ilOffset < binSpan.End)
							yield return new TagSpan<ITextMarkerTag>(new SnapshotSpan(snapshot, textSpan.Start, textSpan.Length), activeStatementTextMarkerTag);
					}
				}
			}
		}
		static readonly TextMarkerTag activeStatementTextMarkerTag = new TextMarkerTag(ThemeClassificationTypeNameKeys.ActiveStatementMarker);

		public void OnNewActiveStatements(List<CorFrame> frames) {
			var dict = new Dictionary<ModuleTokenId, List<uint>>();

			// The first frame is the current statement and it's always visible so there's
			// no point in adding an active statement there
			for (int i = 1; i < frames.Count; i++) {
				var frame = frames[i];
				if (!frame.IsILFrame)
					continue;
				var ip = frame.ILFrameIP;
				if (!ip.IsExact && !ip.IsApproximate && !ip.IsProlog && !ip.IsEpilog)
					continue;
				uint token = frame.Token;
				if (token == 0)
					continue;
				var mod = frame.DnModuleId;
				if (mod == null)
					continue;

				var key = new ModuleTokenId(mod.Value.ToModuleId(), frame.Token);
				if (!dict.TryGetValue(key, out var list))
					dict.Add(key, list = new List<uint>());
				uint offset = ip.Offset;
				// The list should be small so Contains() should be fast
				Debug.Assert(list.Count <= 10, "Perhaps use a hash?");
				if (!list.Contains(offset))
					list.Add(offset);
			}

			activeStatements = dict;
			RaiseTagsChanged();
		}

		public void OnCleared() {
			if (activeStatements.Count == 0)
				return;
			activeStatements = new Dictionary<ModuleTokenId, List<uint>>();
			RaiseTagsChanged();
		}

		void RaiseTagsChanged() {
			foreach (var tagger in taggers) {
				//TODO: Optimize this by only raising tags-changed if needed
				var snapshot = tagger.TextView.TextSnapshot;
				tagger.RaiseTagsChanged(new SnapshotSpan(snapshot, 0, snapshot.Length));
			}
		}
	}
}
