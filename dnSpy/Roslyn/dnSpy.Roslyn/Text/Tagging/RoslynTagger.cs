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
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Tagging;
using dnSpy.Roslyn.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Roslyn.Text.Tagging {
	sealed class RoslynTagger : AsyncTagger<IClassificationTag, RoslynTaggerAsyncState>, ISynchronousTagger<IClassificationTag> {
		readonly ITextBuffer textBuffer;
		readonly IClassificationType defaultClassificationType;
		readonly RoslynClassificationTypes roslynClassificationTypes;
		readonly IRoslynDocumentChangedService roslynDocumentChangedService;

		public RoslynTagger(ITextBuffer textBuffer, IThemeClassificationTypeService themeClassificationTypeService, IRoslynDocumentChangedService roslynDocumentChangedService) {
			if (themeClassificationTypeService is null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
			defaultClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.Error);
			roslynClassificationTypes = RoslynClassificationTypes.GetClassificationTypeInstance(themeClassificationTypeService);
			this.roslynDocumentChangedService = roslynDocumentChangedService ?? throw new ArgumentNullException(nameof(roslynDocumentChangedService));
			roslynDocumentChangedService.DocumentChanged += RoslynDocumentChangedService_DocumentChanged;
		}

		void RoslynDocumentChangedService_DocumentChanged(object? sender, RoslynDocumentChangedEventArgs e) {
			var snapshot = e.Snapshot;
			if (textBuffer == snapshot.TextBuffer)
				RefreshAllTags(snapshot);
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken) {
			if (spans.Count == 0)
				yield break;

			var snapshot = spans[0].Snapshot;
			var asyncState = new RoslynTaggerAsyncState();
			Initialize(asyncState, snapshot, cancellationToken).Wait(cancellationToken);
			if (!asyncState.IsValid)
				yield break;
			Debug2.Assert(asyncState.SyntaxRoot is not null && asyncState.SemanticModel is not null && asyncState.Workspace is not null);

			var classifier = new RoslynClassifier(asyncState.SyntaxRoot, asyncState.SemanticModel, asyncState.Workspace, roslynClassificationTypes, defaultClassificationType, cancellationToken);
			foreach (var span in spans) {
				foreach (var info in classifier.GetColors(span.Span.ToTextSpan()))
					yield return new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, info.Span), new ClassificationTag((IClassificationType)info.Color));
			}
		}

		protected override async Task GetTagsAsync(GetTagsState state, NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				return;

			var snapshot = spans[0].Snapshot;
			if (!state.UserAsyncState.IsInitialized)
				await Initialize(state.UserAsyncState, snapshot, state.CancellationToken).ConfigureAwait(false);
			if (!state.UserAsyncState.IsValid)
				return;
			Debug2.Assert(state.UserAsyncState.SyntaxRoot is not null && state.UserAsyncState.SemanticModel is not null && state.UserAsyncState.Workspace is not null);

			var classifier = new RoslynClassifier(state.UserAsyncState.SyntaxRoot, state.UserAsyncState.SemanticModel, state.UserAsyncState.Workspace, roslynClassificationTypes, defaultClassificationType, state.CancellationToken);
			state.UserAsyncState.TagsList.Clear();
			foreach (var span in spans) {
				foreach (var info in classifier.GetColors(span.Span.ToTextSpan()))
					state.UserAsyncState.TagsList.Add(new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, info.Span), new ClassificationTag((IClassificationType)info.Color)));
				if (state.UserAsyncState.TagsList.Count != 0) {
					state.AddResult(new TagsResult(span, state.UserAsyncState.TagsList.ToArray()));
					state.UserAsyncState.TagsList.Clear();
				}
			}
		}

		async Task Initialize(RoslynTaggerAsyncState state, ITextSnapshot snapshot, CancellationToken cancellationToken) {
			var doc = snapshot.GetOpenDocumentInCurrentContextWithChanges();
			if (doc is null)
				return;
			var syntaxRoot = await doc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var semanticModel = await doc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			var workspace = snapshot.TextBuffer.TryGetWorkspace();
			Debug.Assert(!state.IsInitialized);
			state.Initialize(syntaxRoot, semanticModel, workspace);
		}

		protected override void DisposeInternal() =>
			roslynDocumentChangedService.DocumentChanged -= RoslynDocumentChangedService_DocumentChanged;
	}
}
