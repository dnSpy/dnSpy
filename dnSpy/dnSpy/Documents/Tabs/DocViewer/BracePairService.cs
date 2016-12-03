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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	interface IBracePairTag : ITextMarkerTag {
	}

	sealed class BracePairTag : IBracePairTag {
		public static readonly BracePairTag Instance = new BracePairTag();
		BracePairTag() { }
		public string Type => ThemeClassificationTypeNameKeys.BraceMatching;
	}

	[Export(typeof(IViewTaggerProvider))]
	[ContentType(ContentTypes.Text)]
	[TagType(typeof(IBracePairTag))]
	sealed class BracePairViewTaggerProvider : IViewTaggerProvider {
		readonly IBracePairServiceProvider bracePairServiceProvider;

		[ImportingConstructor]
		BracePairViewTaggerProvider(IBracePairServiceProvider bracePairServiceProvider) {
			this.bracePairServiceProvider = bracePairServiceProvider;
		}

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			if (textView.TextBuffer != buffer)
				return null;
			return textView.Properties.GetOrCreateSingletonProperty(typeof(BracePairViewTagger), () => new BracePairViewTagger(textView, bracePairServiceProvider.GetBracePairService(textView))) as ITagger<T>;
		}
	}

	sealed class BracePairViewTagger : ITagger<IBracePairTag> {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		readonly IBracePairService bracePairService;

		public BracePairViewTagger(ITextView textView, IBracePairService bracePairService) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.bracePairService = bracePairService;
			bracePairService.SetBracePairViewTagger(this);
		}

		public IEnumerable<ITagSpan<IBracePairTag>> GetTags(NormalizedSnapshotSpanCollection spans) =>
			bracePairService.GetTags(spans);

		public void RaiseTagsChanged(SnapshotSpan span) => TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
	}

	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.DocumentViewer - 100)]
	sealed class BracePairCommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly Lazy<IBracePairServiceProvider> bracePairServiceProvider;

		[ImportingConstructor]
		BracePairCommandTargetFilterProvider(Lazy<IBracePairServiceProvider> bracePairServiceProvider) {
			this.bracePairServiceProvider = bracePairServiceProvider;
		}

		public ICommandTargetFilter Create(object target) {
			var textView = target as ITextView;
			if (textView?.Roles.Contains(PredefinedDsTextViewRoles.DocumentViewer) != true)
				return null;

			return new BracePairCommandTargetFilter(textView, bracePairServiceProvider.Value.GetBracePairService(textView));
		}
	}

	sealed class BracePairCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;
		readonly IBracePairService bracePairService;

		public BracePairCommandTargetFilter(ITextView textView, IBracePairService bracePairService) {
			this.textView = textView;
			this.bracePairService = bracePairService;
		}

		IDocumentViewer TryGetInstance() =>
			__documentViewer ?? (__documentViewer = DocumentViewerExtensions.TryGetDocumentViewer(textView.TextBuffer));
		IDocumentViewer __documentViewer;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (TryGetInstance() == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.GOTOBRACE:
				case TextEditorIds.GOTOBRACE_EXT:
					return CommandTargetStatus.Handled;
				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			var documentViewer = TryGetInstance();
			if (documentViewer == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.GOTOBRACE:
					MoveToMatchingBrace(documentViewer, false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.GOTOBRACE_EXT:
					MoveToMatchingBrace(documentViewer, true);
					return CommandTargetStatus.Handled;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		static void MoveToMatchingBrace(IDocumentViewer documentViewer, bool select) {
			var t = GetMatchingBracePosition(documentViewer);
			if (t == null)
				return;
			var pos = t.Item1;
			if (pos == null)
				return;
			var snapshot = documentViewer.TextView.TextSnapshot;
			if (pos.Value > snapshot.Length)
				return;
			var bpResult = t.Item2;
			if (bpResult.Left.End > snapshot.Length || bpResult.Right.End > snapshot.Length)
				return;
			if (bpResult.Left.Start > bpResult.Right.Start)
				return;
			if (select) {
				bool reverse = pos.Value == bpResult.Left.Start;
				documentViewer.Selection.Mode = TextSelectionMode.Stream;
				documentViewer.Selection.Select(new SnapshotSpan(snapshot, Span.FromBounds(bpResult.Left.Start, bpResult.Right.End)), reverse);
			}
			else
				documentViewer.Selection.Clear();
			documentViewer.Caret.MoveTo(new SnapshotPoint(snapshot, pos.Value));
			documentViewer.Caret.EnsureVisible();
		}

		static Tuple<int?, BracePairResult> GetMatchingBracePosition(IDocumentViewer documentViewer) {
			var caretPos = documentViewer.TextView.Caret.Position;
			if (caretPos.VirtualSpaces > 0)
				return null;
			var coll = documentViewer.Content.GetCustomData<BracePairCollection>(DocumentViewerContentDataIds.BracePair);
			if (coll == null)
				return null;
			int pos = caretPos.BufferPosition.Position;
			var pairColl = coll.GetBracePairs(pos);
			if (pairColl == null)
				return null;
			var pair = pairColl.Value.First;
			if (pair.Left.Start == pos)
				return Tuple.Create<int?, BracePairResult>(pair.Right.End, pair);
			if (pair.Right.End == pos)
				return Tuple.Create<int?, BracePairResult>(pair.Left.Start, pair);
			if (pair.Right.Start == pos) {
				var pair2 = coll.GetBracePairs(pos - 1);
				if (pair2 != null && pair2.Value.First.Right.End == pos)
					return Tuple.Create<int?, BracePairResult>(pair2.Value.First.Left.Start, pair2.Value.First);
			}
			if (pair.Left.Start <= pos && pos <= pair.Left.End)
				return Tuple.Create<int?, BracePairResult>(pair.Right.End, pair);
			if (pair.Right.Start <= pos && pos <= pair.Right.End)
				return Tuple.Create<int?, BracePairResult>(pair.Left.Start, pair);
			return null;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}

	[ExportDocumentViewerListener(DocumentViewerListenerConstants.ORDER_BRACEPAIRSERVICE)]
	sealed class BracePairDocumentViewerListener : IDocumentViewerListener {
		readonly IBracePairServiceProvider bracePairServiceProvider;

		[ImportingConstructor]
		BracePairDocumentViewerListener(IBracePairServiceProvider bracePairServiceProvider) {
			this.bracePairServiceProvider = bracePairServiceProvider;
		}

		public void OnEvent(DocumentViewerEventArgs e) {
			if (e.EventType == DocumentViewerEvent.GotNewContent)
				bracePairServiceProvider.GetBracePairService(e.DocumentViewer.TextView).SetBracePairCollection(e.DocumentViewer.Content.GetCustomData<BracePairCollection>(DocumentViewerContentDataIds.BracePair));
		}
	}

	interface IBracePairServiceProvider {
		IBracePairService GetBracePairService(ITextView textView);
	}

	[Export(typeof(IBracePairServiceProvider))]
	sealed class BracePairServiceProvider : IBracePairServiceProvider {
		[ImportingConstructor]
		BracePairServiceProvider() {
		}

		public IBracePairService GetBracePairService(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.GetOrCreateSingletonProperty(typeof(BracePairService), () => new BracePairService(textView));
		}
	}

	interface IBracePairService {
		void SetBracePairCollection(BracePairCollection bracePairCollection);
		void SetBracePairViewTagger(BracePairViewTagger tagger);
		IEnumerable<ITagSpan<IBracePairTag>> GetTags(NormalizedSnapshotSpanCollection spans);
	}

	sealed class BracePairService : IBracePairService {
		readonly ITextView textView;
		BracePairViewTagger tagger;
		BracePairCollection bracePairCollection;
		bool canHighlightBraces;
		BracePairResultCollection? currentBracePair;

		public BracePairService(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.textView = textView;
			bracePairCollection = BracePairCollection.Empty;
			textView.Closed += TextView_Closed;
			textView.Options.OptionChanged += Options_OptionChanged;
			UpdateBraceMatching();
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (textView.IsClosed)
				return;
			if (e.OptionId == DefaultDsTextViewOptions.BraceMatchingName)
				UpdateBraceMatching();
		}

		void UpdateBraceMatching() {
			canHighlightBraces = textView.Options.IsBraceMatchingEnabled();
			var oldValue = currentBracePair;
			if (canHighlightBraces) {
				textView.Caret.PositionChanged += Caret_PositionChanged;
				currentBracePair = GetCurrentBracePair();
			}
			else {
				textView.Caret.PositionChanged -= Caret_PositionChanged;
				currentBracePair = null;
			}
			RefreshTags(oldValue, currentBracePair);
		}

		BracePairResultCollection? GetCurrentBracePair() {
			var caretPos = textView.Caret.Position;
			if (caretPos.VirtualSpaces > 0)
				return null;
			int pos = caretPos.BufferPosition.Position;
			var res = bracePairCollection.GetBracePairs(pos);
			if (res == null)
				return null;
			if (res.Value.First.Left.Start == pos || res.Value.First.Right.End == pos)
				return res;
			if (res.Value.First.Right.Start == pos) {
				res = bracePairCollection.GetBracePairs(pos - 1);
				if (res != null && res.Value.First.Right.End == pos)
					return new BracePairResultCollection(res.Value.First, null);
			}
			return null;
		}

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) => UpdateBracePairs();

		void UpdateBracePairs(bool refresh = true) {
			var newBracePair = GetCurrentBracePair();
			if (!IsSamePair(currentBracePair, newBracePair)) {
				var oldPair = currentBracePair;
				currentBracePair = newBracePair;
				if (refresh)
					RefreshTags(oldPair, currentBracePair);
			}
		}

		static bool IsSamePair(BracePairResultCollection? a, BracePairResultCollection? b) {
			if (a == null && b == null)
				return true;
			if (a == null || b == null)
				return false;
			return a.Value.Equals(b.Value);
		}

		void RefreshTags(BracePairResultCollection? a, BracePairResultCollection? b) {
			if (a != null) {
				RefreshTags(a.Value.First);
				RefreshTags(a.Value.First);
				RefreshTags(a.Value.Second);
				RefreshTags(a.Value.Second);
			}
			if (b != null) {
				RefreshTags(b.Value.First);
				RefreshTags(b.Value.First);
				RefreshTags(b.Value.Second);
				RefreshTags(b.Value.Second);
			}
		}

		void RefreshTags(BracePairResult? a) {
			if (a != null) {
				RefreshTags(a.Value.Left);
				RefreshTags(a.Value.Right);
			}
		}

		void RefreshTags(Span span) {
			if (tagger != null) {
				var snapshot = textView.TextSnapshot;
				if (span.End <= snapshot.Length)
					tagger?.RaiseTagsChanged(new SnapshotSpan(snapshot, span));
			}
		}

		void RefreshAllTags() {
			if (tagger != null) {
				UpdateBracePairs(false);
				var snapshot = textView.TextSnapshot;
				tagger?.RaiseTagsChanged(new SnapshotSpan(snapshot, 0, snapshot.Length));
			}
		}

		public void SetBracePairCollection(BracePairCollection bracePairCollection) {
			this.bracePairCollection = bracePairCollection ?? BracePairCollection.Empty;
			RefreshAllTags();
		}

		public void SetBracePairViewTagger(BracePairViewTagger tagger) {
			if (tagger == null)
				throw new ArgumentNullException(nameof(tagger));
			if (this.tagger != null)
				throw new InvalidOperationException();
			this.tagger = tagger;
		}

		public IEnumerable<ITagSpan<IBracePairTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (currentBracePair == null)
				yield break;
			var snapshot = textView.TextSnapshot;
			if (currentBracePair.Value.First.Left.End <= snapshot.Length)
				yield return new TagSpan<IBracePairTag>(new SnapshotSpan(snapshot, currentBracePair.Value.First.Left), BracePairTag.Instance);
			if (currentBracePair.Value.First.Right.End <= snapshot.Length)
				yield return new TagSpan<IBracePairTag>(new SnapshotSpan(snapshot, currentBracePair.Value.First.Right), BracePairTag.Instance);
			if (currentBracePair.Value.Second != null) {
				if (currentBracePair.Value.Second.Value.Left.End <= snapshot.Length)
					yield return new TagSpan<IBracePairTag>(new SnapshotSpan(snapshot, currentBracePair.Value.Second.Value.Left), BracePairTag.Instance);
				if (currentBracePair.Value.Second.Value.Right.End <= snapshot.Length)
					yield return new TagSpan<IBracePairTag>(new SnapshotSpan(snapshot, currentBracePair.Value.Second.Value.Right), BracePairTag.Instance);
			}
		}

		void TextView_Closed(object sender, EventArgs e) {
			currentBracePair = null;
			bracePairCollection = BracePairCollection.Empty;
			textView.Closed -= TextView_Closed;
			textView.Options.OptionChanged -= Options_OptionChanged;
			textView.Caret.PositionChanged -= Caret_PositionChanged;
		}
	}
}
