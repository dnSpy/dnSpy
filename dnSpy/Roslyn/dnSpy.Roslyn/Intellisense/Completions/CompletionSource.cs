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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Utilities;
using dnSpy.Roslyn.Properties;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Intellisense.Completions {
	[Export(typeof(ICompletionSourceProvider))]
	[Name(PredefinedDsCompletionSourceProviders.Roslyn)]
	[ContentType(ContentTypes.RoslynCode)]
	sealed class CompletionSourceProvider : ICompletionSourceProvider {
		readonly IMruCompletionService mruCompletionService;

		[ImportingConstructor]
		CompletionSourceProvider(IMruCompletionService mruCompletionService) =>
			this.mruCompletionService = mruCompletionService;

		public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer) => new CompletionSource(mruCompletionService);
	}

	sealed class CompletionSource : ICompletionSource {
		const string DefaultCompletionSetMoniker = "All";
		readonly IMruCompletionService mruCompletionService;

		public CompletionSource(IMruCompletionService mruCompletionService) =>
			this.mruCompletionService = mruCompletionService ?? throw new ArgumentNullException(nameof(mruCompletionService));

		public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets) {
			var snapshot = session.TextView.TextSnapshot;
			var triggerPoint = session.GetTriggerPoint(snapshot);
			if (triggerPoint == null)
				return;
			var info = CompletionInfo.Create(snapshot);
			if (info == null)
				return;

			// This helps a little to speed up the code
			ProfileOptimizationHelper.StartProfile("roslyn-completion-" + info.Value.CompletionService.Language);

			session.Properties.TryGetProperty(typeof(CompletionTrigger), out CompletionTrigger completionTrigger);

			var completionList = info.Value.CompletionService.GetCompletionsAsync(info.Value.Document, triggerPoint.Value.Position, completionTrigger).GetAwaiter().GetResult();
			if (completionList == null)
				return;
			Debug.Assert(completionList.Span.End <= snapshot.Length);
			if (completionList.Span.End > snapshot.Length)
				return;
			var trackingSpan = snapshot.CreateTrackingSpan(completionList.Span.Start, completionList.Span.Length, SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Forward);
			var completionSet = RoslynCompletionSet.Create(mruCompletionService, completionList, info.Value.CompletionService, session.TextView, DefaultCompletionSetMoniker, dnSpy_Roslyn_Resources.CompletionSet_All, trackingSpan);
			completionSets.Add(completionSet);
		}

		public void Dispose() { }
	}
}
