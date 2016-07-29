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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Themes;
using dnSpy.Roslyn.Shared.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Roslyn.Shared.Text.Tagging {
	sealed class RoslynTagger : AsyncTagger<IClassificationTag, RoslynTaggerAsyncState> {
		readonly IClassificationType defaultClassificationType;
		readonly RoslynClassifierColors roslynClassifierColors;

		public RoslynTagger(IThemeClassificationTypes themeClassificationTypes) {
			if (themeClassificationTypes == null)
				throw new ArgumentNullException(nameof(themeClassificationTypes));
			this.defaultClassificationType = themeClassificationTypes.GetClassificationType(ColorType.Error);
			this.roslynClassifierColors = RoslynClassifierColors.GetClassificationTypeInstance(themeClassificationTypes);
		}

		protected override async Task GetTagsAsync(GetTagsState state, NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				return;

			var snapshot = spans[0].Snapshot;
			if (!state.UserAsyncState.IsInitialized)
				await Initialize(state.UserAsyncState, snapshot, state.CancellationToken).ConfigureAwait(false);
			if (!state.UserAsyncState.IsValid)
				return;

			var classifier = new RoslynClassifier(state.UserAsyncState.SyntaxRoot, state.UserAsyncState.SemanticModel, state.UserAsyncState.Workspace, roslynClassifierColors, defaultClassificationType, state.CancellationToken);
			state.UserAsyncState.TagsList.Clear();
			foreach (var span in spans) {
				foreach (var info in classifier.GetClassificationColors(span.Span.ToTextSpan()))
					state.UserAsyncState.TagsList.Add(new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, info.Span), new ClassificationTag((IClassificationType)info.Type)));
				if (state.UserAsyncState.TagsList.Count != 0) {
					state.AddResult(new TagsResult(span, state.UserAsyncState.TagsList.ToArray()));
					state.UserAsyncState.TagsList.Clear();
				}
			}
		}

		async Task Initialize(RoslynTaggerAsyncState state, ITextSnapshot snapshot, CancellationToken cancellationToken) {
			var doc = snapshot.GetOpenDocumentInCurrentContextWithChanges();
			if (doc == null)
				return;
			var syntaxRoot = await doc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var semanticModel = await doc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			var workspace = snapshot.TextBuffer.TryGetWorkspace();
			Debug.Assert(!state.IsInitialized);
			state.Initialize(syntaxRoot, semanticModel, workspace);
		}
	}
}
