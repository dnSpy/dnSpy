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

//TODO: Use CompletionService.FilterItems() when it becomes public (probably in Roslyn 2.0)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Roslyn.Shared.Text;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Shared.Intellisense {
	sealed class RoslynCompletionCollection : CompletionCollection {
		readonly CompletionService completionService;
		readonly ITextView textView;
		readonly RoslynIntellisenseFilter[] filters;
		readonly ITextSnapshot originalSnapshot;

		RoslynCompletionCollection(CompletionService completionService, ITextView textView, ITrackingSpan applicableTo, List<Completion> completions, RoslynIntellisenseFilter[] filters)
			: base(applicableTo, completions, filters) {
			if (completionService == null)
				throw new ArgumentNullException(nameof(completionService));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (applicableTo == null)
				throw new ArgumentNullException(nameof(applicableTo));
			if (filters == null)
				throw new ArgumentNullException(nameof(filters));
			this.completionService = completionService;
			this.textView = textView;
			this.filters = filters;
			this.originalSnapshot = applicableTo.TextBuffer.CurrentSnapshot;
		}

		public static RoslynCompletionCollection Create(CompletionList completionList, CompletionService completionService, ITextView textView, ITrackingSpan applicableTo) {
			if (completionList == null)
				throw new ArgumentNullException(nameof(completionList));
			if (completionService == null)
				throw new ArgumentNullException(nameof(completionService));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (applicableTo == null)
				throw new ArgumentNullException(nameof(applicableTo));
			var completions = new List<Completion>(completionList.Items.Length);
			var remainingFilters = new List<KeyValuePair<RoslynIntellisenseFilter, int>>(RoslynIntellisenseFilters.CreateFilters().Select((a, index) => new KeyValuePair<RoslynIntellisenseFilter, int>(a, index)));
			var filters = new List<KeyValuePair<RoslynIntellisenseFilter, int>>(remainingFilters.Count);
			foreach (var item in completionList.Items) {
				if (string.IsNullOrEmpty(item.DisplayText))
					continue;
				for (int i = remainingFilters.Count - 1; i >= 0; i--) {
					var kv = remainingFilters[i];
					foreach (var tag in kv.Key.Tags) {
						if (item.Tags.Contains(tag)) {
							remainingFilters.RemoveAt(i);
							filters.Add(kv);
							break;
						}
					}
				}
				completions.Add(new RoslynCompletion(item));
			}
			filters.Sort((a, b) => a.Value - b.Value);
			return new RoslynCompletionCollection(completionService, textView, applicableTo, completions, filters.Select(a => a.Key).ToArray());
		}

		protected override void Filter(List<Completion> filteredResult, IList<Completion> completions) {
			List<string> filteredTags = null;

			foreach (var filter in filters) {
				if (filter.IsChecked) {
					if (filteredTags == null)
						filteredTags = new List<string>();
					filteredTags.AddRange(filter.Tags);
				}
			}

			if (filteredTags == null)
				base.Filter(filteredResult, completions);
			else {
				foreach (var completion in completions) {
					var roslynCompletion = completion as RoslynCompletion;
					if (roslynCompletion != null) {
						foreach (var tag in roslynCompletion.CompletionItem.Tags) {
							if (filteredTags.Contains(tag))
								goto matched;
						}
						continue;
					}
matched:
					filteredResult.Add(completion);
				}
			}
		}

		public override void Commit() {
			var completion = CurrentCompletion.Completion as RoslynCompletion;
			if (completion == null) {
				base.Commit();
				return;
			}

			var info = CompletionInfo.Create(ApplicableTo.TextBuffer.CurrentSnapshot);
			Debug.Assert(info != null);
			if (info == null)
				return;

			var change = completionService.GetChangeAsync(info.Value.Document, completion.CompletionItem, commitCharacter: null).GetAwaiter().GetResult();
			var buffer = ApplicableTo.TextBuffer;
			var currentSnapshot = buffer.CurrentSnapshot;
			using (var ed = buffer.CreateEdit()) {
				foreach (var c in change.TextChanges) {
					Debug.Assert(c.Span.End <= originalSnapshot.Length);
					if (c.Span.End > originalSnapshot.Length)
						return;
					var span = new SnapshotSpan(originalSnapshot, c.Span.ToSpan()).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeInclusive);
					if (!ed.Replace(span.Span, c.NewText))
						return;
				}
				ed.Apply();
			}
			if (change.NewPosition != null) {
				var snapshot = buffer.CurrentSnapshot;
				Debug.Assert(change.NewPosition.Value <= snapshot.Length);
				if (change.NewPosition.Value <= snapshot.Length) {
					textView.Caret.MoveTo(new SnapshotPoint(snapshot, change.NewPosition.Value));
					textView.Caret.EnsureVisible();
				}
			}
		}
	}
}
