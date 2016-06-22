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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Tagging;
using dnSpy.Contracts.Themes;
using dnSpy.Roslyn.Shared.Classification;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Shared.Text {
	sealed class RoslynTagger : ITagger<IClassificationTag> {
		struct RoslynState {
			public SyntaxNode SyntaxRoot { get; }
			public SemanticModel SemanticModel { get; }
			public Workspace Workspace { get; }
			public bool IsValid => SyntaxRoot != null && SemanticModel != null && Workspace != null;

			public RoslynState(SyntaxNode syntaxRoot, SemanticModel semanticModel, Workspace workspace) {
				SyntaxRoot = syntaxRoot;
				SemanticModel = semanticModel;
				Workspace = workspace;
			}
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged {
			add { }
			remove { }
		}

		readonly IThemeClassificationTypes themeClassificationTypes;

		public RoslynTagger(IThemeClassificationTypes themeClassificationTypes) {
			if (themeClassificationTypes == null)
				throw new ArgumentNullException(nameof(themeClassificationTypes));
			this.themeClassificationTypes = themeClassificationTypes;
		}

		//TODO: Remove this and replace it with false when GetColorSpans() works async
		const bool continueOnCapturedContext = true;
		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				return Enumerable.Empty<ITagSpan<IClassificationTag>>();

			//TODO: Use a one-key cache (key=snapshot) stored in a weak ref?

			//TODO: Try to use async. Will need to raise TagsChanged when our async method has the result.
			var cancellationToken = CancellationToken.None;
			try {
				return GetColorSpansAsync(spans, cancellationToken).GetAwaiter().GetResult();
			}
			catch (OperationCanceledException) {
				return Enumerable.Empty<ITagSpan<IClassificationTag>>();
			}
		}

		async Task<IEnumerable<ITagSpan<IClassificationTag>>> GetColorSpansAsync(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken) {
			Debug.Assert(spans.Count != 0);
			var snapshot = spans[0].Snapshot;

			var state = await GetStateAsync(snapshot, cancellationToken).ConfigureAwait(continueOnCapturedContext);
			Debug.Assert(state.IsValid);
			if (!state.IsValid)
				return Enumerable.Empty<ITagSpan<IClassificationTag>>();

			List<ITagSpan<IClassificationTag>> result = null;
			var classifier = new RoslynClassifier(state.SyntaxRoot, state.SemanticModel, state.Workspace, themeClassificationTypes, themeClassificationTypes.GetClassificationType(ColorType.Error), cancellationToken);
			foreach (var span in spans) {
				foreach (var info in classifier.GetClassificationColors(span.Span.ToTextSpan())) {
					if (result == null)
						result = new List<ITagSpan<IClassificationTag>>();
					result.Add(new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, info.Span), new ClassificationTag(info.Type)));
				}
			}

			return result ?? Enumerable.Empty<ITagSpan<IClassificationTag>>();
		}

		async Task<RoslynState> GetStateAsync(ITextSnapshot snapshot, CancellationToken cancellationToken) {
			var doc = snapshot.GetOpenDocumentInCurrentContextWithChanges();
			if (doc == null)
				return default(RoslynState);
			var syntaxRoot = await doc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);
			var semanticModel = await doc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);
			var workspace = snapshot.TextBuffer.TryGetWorkspace();
			return new RoslynState(syntaxRoot, semanticModel, workspace);
		}
	}
}
