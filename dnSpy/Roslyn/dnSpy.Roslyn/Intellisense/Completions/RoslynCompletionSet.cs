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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Roslyn.Text;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Intellisense.Completions {
	sealed class RoslynCompletionSet : DsCompletionSet, ICompletionSetContentTypeProvider {
		readonly IMruCompletionService mruCompletionService;
		readonly CompletionService completionService;
		readonly ITextView textView;
		readonly ITextSnapshot originalSnapshot;

		RoslynCompletionSet(IMruCompletionService mruCompletionService, CompletionService completionService, ITextView textView, string moniker, string displayName, ITrackingSpan applicableTo, List<Completion> completions, List<Completion> completionBuilders, RoslynIntellisenseFilter[] filters)
			: base(moniker, displayName, applicableTo, completions, completionBuilders, filters) {
			this.mruCompletionService = mruCompletionService;
			this.completionService = completionService;
			this.textView = textView;
			originalSnapshot = applicableTo.TextBuffer.CurrentSnapshot;
			InitializeCompletions(completions);
			InitializeCompletions(completionBuilders);
		}

		void InitializeCompletions(IEnumerable<Completion> completions) {
			foreach (var c in completions) {
				var rc = c as RoslynCompletion;
				Debug2.Assert(rc is not null);
				if (rc is not null)
					rc.CompletionSet = this;
			}
		}

		public static RoslynCompletionSet Create(IMruCompletionService mruCompletionService, CompletionList completionList, CompletionService completionService, ITextView textView, string moniker, string displayName, ITrackingSpan applicableTo) {
			if (mruCompletionService is null)
				throw new ArgumentNullException(nameof(mruCompletionService));
			if (completionList is null)
				throw new ArgumentNullException(nameof(completionList));
			if (completionService is null)
				throw new ArgumentNullException(nameof(completionService));
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			if (moniker is null)
				throw new ArgumentNullException(nameof(moniker));
			if (displayName is null)
				throw new ArgumentNullException(nameof(displayName));
			if (applicableTo is null)
				throw new ArgumentNullException(nameof(applicableTo));
			var completions = new List<Completion>(completionList.Items.Length);
			var remainingFilters = new List<(RoslynIntellisenseFilter filter, int index)>(RoslynIntellisenseFilters.CreateFilters().Select((a, index) => (a, index)));
			var filters = new List<(RoslynIntellisenseFilter filter, int index)>(remainingFilters.Count);
			foreach (var item in completionList.Items) {
				if (string.IsNullOrEmpty(item.DisplayText))
					continue;
				for (int i = remainingFilters.Count - 1; i >= 0; i--) {
					var kv = remainingFilters[i];
					foreach (var tag in kv.filter.Tags) {
						if (item.Tags.Contains(tag)) {
							remainingFilters.RemoveAt(i);
							filters.Add(kv);
							break;
						}
					}
				}
				completions.Add(new RoslynCompletion(item));
			}
			filters.Sort((a, b) => a.index - b.index);
			var completionBuilders = new List<Completion>();
			return new RoslynCompletionSet(mruCompletionService, completionService, textView, moniker, displayName, applicableTo, completions, completionBuilders, filters.Select(a => a.filter).ToArray());
		}

		protected override int GetMruIndex(Completion completion) => mruCompletionService.GetMruIndex(completion.DisplayText);

		protected override void Filter(List<Completion> filteredResult, IList<Completion> completions) {
			List<string>? filteredTags = null;

			var filters = Filters;
			Debug2.Assert(filters is not null);
			if (filters is not null) {
				foreach (var tmpFilter in filters) {
					var filter = tmpFilter as RoslynIntellisenseFilter;
					Debug2.Assert(filter is not null);
					if (filter is not null && filter.IsChecked) {
						if (filteredTags is null)
							filteredTags = new List<string>();
						filteredTags.AddRange(filter.Tags);
					}
				}
			}

			if (filteredTags is null)
				base.Filter(filteredResult, completions);
			else {
				foreach (var completion in completions) {
					if (completion is RoslynCompletion roslynCompletion) {
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

		public void Commit(RoslynCompletion completion) {
			if (completion is null)
				throw new ArgumentNullException(nameof(completion));

			mruCompletionService.AddText(completion.DisplayText);

			var info = CompletionInfo.Create(ApplicableTo.TextBuffer.CurrentSnapshot);
			Debug2.Assert(info is not null);
			if (info is null)
				return;

			var change = completionService.GetChangeAsync(info.Value.Document, completion.CompletionItem, commitCharacter: null).GetAwaiter().GetResult();
			var buffer = ApplicableTo.TextBuffer;
			var currentSnapshot = buffer.CurrentSnapshot;
			using (var ed = buffer.CreateEdit()) {
				var textChange = change.TextChange;
				Debug.Assert(textChange.Span.End <= originalSnapshot.Length);
				if (textChange.Span.End > originalSnapshot.Length)
					return;
				var span = new SnapshotSpan(originalSnapshot, textChange.Span.ToSpan()).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeInclusive);
				if (!ed.Replace(span.Span, textChange.NewText))
					return;
				ed.Apply();
			}
			if (change.NewPosition is not null) {
				var snapshot = buffer.CurrentSnapshot;
				Debug.Assert(change.NewPosition.Value <= snapshot.Length);
				if (change.NewPosition.Value <= snapshot.Length) {
					textView.Caret.MoveTo(new SnapshotPoint(snapshot, change.NewPosition.Value));
					textView.Caret.EnsureVisible();
				}
			}
		}

		/// <summary>
		/// Gets the description or null if none
		/// </summary>
		/// <param name="completion">Completion</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public Task<CompletionDescription?> GetDescriptionAsync(RoslynCompletion completion, CancellationToken cancellationToken = default) {
			if (completion is null)
				throw new ArgumentNullException(nameof(completion));

			var info = CompletionInfo.Create(textView.TextSnapshot);
			if (info is null)
				return Task.FromResult<CompletionDescription?>(null);

			return completionService.GetDescriptionAsync(info.Value.Document, completion.CompletionItem, cancellationToken);
		}

		IContentType? ICompletionSetContentTypeProvider.GetContentType(IContentTypeRegistryService contentTypeRegistryService, CompletionClassifierKind kind) {
			switch (kind) {
			case CompletionClassifierKind.DisplayText:
				return contentTypeRegistryService.GetContentType(RoslynContentTypes.CompletionDisplayTextRoslyn);
			case CompletionClassifierKind.Suffix:
				return contentTypeRegistryService.GetContentType(RoslynContentTypes.CompletionSuffixRoslyn);
			default:
				Debug.Fail($"Unknown kind: {kind}");
				return null;
			}
		}
	}
}
