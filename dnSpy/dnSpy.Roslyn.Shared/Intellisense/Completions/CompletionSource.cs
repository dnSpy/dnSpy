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
using System.Diagnostics;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Shared.Intellisense.Completions {
	[Export(typeof(ICompletionSourceProvider))]
	[Name(PredefinedDnSpyCompletionSourceProviders.Roslyn)]
	[ContentType(ContentTypes.RoslynCode)]
	sealed class CompletionSourceProvider : ICompletionSourceProvider {
		public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer) => new CompletionSource();
	}

	sealed class CompletionSource : ICompletionSource {
		public void AugmentCompletionSession(ICompletionSession session, IList<CompletionCollection> completionCollections) {
			var snapshot = session.TextView.TextSnapshot;
			var triggerPoint = session.GetTriggerPoint(snapshot);
			if (triggerPoint == null)
				return;
			var info = CompletionInfo.Create(snapshot);
			if (info == null)
				return;

			CompletionTrigger completionTrigger;
			session.Properties.TryGetProperty(typeof(CompletionTrigger), out completionTrigger);

			var completionList = info.Value.CompletionService.GetCompletionsAsync(info.Value.Document, triggerPoint.Value.Position, completionTrigger).GetAwaiter().GetResult();
			if (completionList == null)
				return;
			Debug.Assert(completionList.DefaultSpan.End <= snapshot.Length);
			if (completionList.DefaultSpan.End > snapshot.Length)
				return;
			var trackingSpan = snapshot.CreateTrackingSpan(completionList.DefaultSpan.Start, completionList.DefaultSpan.Length, SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Forward);
			var completionCollection = RoslynCompletionCollection.Create(completionList, info.Value.CompletionService, session.TextView, trackingSpan);
			completionCollections.Add(completionCollection);
		}

		public void Dispose() { }
	}
}
