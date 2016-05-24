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
using dnSpy.Roslyn.Shared.Classification;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Shared.Text {
	sealed class RoslynTextSnapshotColorizer : ITextSnapshotColorizer {
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

		//TODO: Remove this and replace it with false when GetColorSpans() works async
		const bool continueOnCapturedContext = true;
		public IEnumerable<ColorSpan> GetColorSpans(SnapshotSpan snapshotSpan) {
			//TODO: Use a one-key cache (key=snapshot) stored in a weak ref?

			//TODO: Try to use async. Will need to notify caller when our async method has the result.
			var cancellationToken = CancellationToken.None;
			try {
				return GetColorSpansAsync(snapshotSpan, cancellationToken).GetAwaiter().GetResult();
			}
			catch (OperationCanceledException) {
				return Enumerable.Empty<ColorSpan>();
			}
		}

		async Task<IEnumerable<ColorSpan>> GetColorSpansAsync(SnapshotSpan snapshotSpan, CancellationToken cancellationToken) {
			var state = await GetStateAsync(snapshotSpan.Snapshot, cancellationToken).ConfigureAwait(continueOnCapturedContext);
			Debug.Assert(state.IsValid);
			if (!state.IsValid)
				return Enumerable.Empty<ColorSpan>();

			List<ColorSpan> colorSpans = null;
			var classifier = new RoslynClassifier(state.SyntaxRoot, state.SemanticModel, state.Workspace, OutputColor.Error, cancellationToken);
			foreach (var info in classifier.GetClassificationColors(snapshotSpan.Span.ToTextSpan())) {
				if (colorSpans == null)
					colorSpans = new List<ColorSpan>();
				colorSpans.Add(new ColorSpan(info.Span, new Color(info.Color.ToColorType()), ColorPriority.Default));
			}

			return colorSpans ?? Enumerable.Empty<ColorSpan>();
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
