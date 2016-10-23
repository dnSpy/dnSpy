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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class DocumentViewerControl : Grid {
		public IDsWpfTextViewHost TextViewHost => wpfTextViewHost;
		public IDsWpfTextView TextView => wpfTextViewHost.TextView;
		public DocumentViewerContent Content => currentContent.Content;
		readonly DocumentViewerContent emptyContent;
		SpanDataCollection<ReferenceAndId> spanReferenceCollection;

		readonly CachedColorsList cachedColorsList;
		readonly IContentType defaultContentType;
		readonly IDsWpfTextViewHost wpfTextViewHost;
		readonly IDocumentViewerHelper textEditorHelper;

		static readonly string[] defaultRoles = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Debuggable,
			PredefinedTextViewRoles.Document,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.Structured,
			PredefinedTextViewRoles.Zoomable,
			PredefinedDsTextViewRoles.DocumentViewer,
			PredefinedDsTextViewRoles.CanHaveBackgroundImage,
			PredefinedDsTextViewRoles.CanHaveIntellisenseControllers,
		};

		public DocumentViewerControl(ITextBufferFactoryService textBufferFactoryService, IDsTextEditorFactoryService dsTextEditorFactoryService, IDocumentViewerHelper textEditorHelper) {
			if (textBufferFactoryService == null)
				throw new ArgumentNullException(nameof(textBufferFactoryService));
			if (dsTextEditorFactoryService == null)
				throw new ArgumentNullException(nameof(dsTextEditorFactoryService));
			if (textEditorHelper == null)
				throw new ArgumentNullException(nameof(textEditorHelper));
			this.textEditorHelper = textEditorHelper;
			this.defaultContentType = textBufferFactoryService.TextContentType;
			this.cachedColorsList = new CachedColorsList();
			this.emptyContent = new DocumentViewerContent(string.Empty, CachedTextColorsCollection.Empty, SpanDataCollection<ReferenceInfo>.Empty, new Dictionary<string, object>());
			this.currentContent = new CurrentContent(emptyContent, defaultContentType);
			this.spanReferenceCollection = SpanDataCollection<ReferenceAndId>.Empty;

			var textBuffer = textBufferFactoryService.CreateTextBuffer(textBufferFactoryService.TextContentType);
			CachedColorsListTaggerProvider.AddColorizer(textBuffer, cachedColorsList);
			var roles = dsTextEditorFactoryService.CreateTextViewRoleSet(defaultRoles);
			var options = new TextViewCreatorOptions { EnableUndoHistory = false };
			var textView = dsTextEditorFactoryService.CreateTextView(textBuffer, roles, options);
			var wpfTextViewHost = dsTextEditorFactoryService.CreateTextViewHost(textView, false);
			this.wpfTextViewHost = wpfTextViewHost;
			wpfTextViewHost.TextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategoryConstants.Viewer);
			wpfTextViewHost.TextView.Options.SetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId, true);
			wpfTextViewHost.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, true);
			Children.Add(wpfTextViewHost.HostControl);
		}

		WaitAdorner CurrentWaitAdorner {
			get { return __currentWaitAdorner; }
			set {
				if (__currentWaitAdorner != null)
					Children.Remove(__currentWaitAdorner);
				__currentWaitAdorner = value;
				if (__currentWaitAdorner != null)
					Children.Add(__currentWaitAdorner);
			}
		}
		WaitAdorner __currentWaitAdorner;

		public Button CancelButton => CurrentWaitAdorner?.button;

		public void ShowCancelButton(Action onCancel, string message) {
			var newWaitAdorner = new WaitAdorner(onCancel, message);
			CurrentWaitAdorner = newWaitAdorner;

			// Prevents flickering when decompiling small classes
			newWaitAdorner.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)), FillBehavior.Stop));

			newWaitAdorner.MouseDown += (s, e) => e.Handled = true;
			newWaitAdorner.MouseUp += (s, e) => e.Handled = true;
			newWaitAdorner.button.IsVisibleChanged += (s, e) => {
				if (newWaitAdorner != CurrentWaitAdorner)
					return;
				if (newWaitAdorner.button.IsVisible && IsKeyboardFocusWithin)
					newWaitAdorner.button.Focus();
			};

			if (IsKeyboardFocusWithin)
				newWaitAdorner.button.Focus();
		}

		public void HideCancelButton() {
			var currentWaitAdorner = CurrentWaitAdorner;
			bool waitAdornerHasFocus = currentWaitAdorner?.IsKeyboardFocusWithin ?? false;
			CurrentWaitAdorner = null;
			if (waitAdornerHasFocus)
				textEditorHelper.SetFocus();
		}

		struct CurrentContent : IEquatable<CurrentContent> {
			public DocumentViewerContent Content { get; }
			readonly IContentType contentType;

			public CurrentContent(DocumentViewerContent content, IContentType contentType) {
				this.Content = content;
				this.contentType = contentType;
			}

			public bool Equals(CurrentContent other) => Content == other.Content && contentType == other.contentType;
		}

		CurrentContent currentContent;
		public bool SetContent(DocumentViewerContent content, IContentType contentType) {
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			if (contentType == null)
				contentType = defaultContentType;

			HideCancelButton();

			var newContent = new CurrentContent(content, contentType);
			if (currentContent.Equals(newContent))
				return false;
			currentContent = newContent;
			spanReferenceCollection = newContent.Content.GetCustomData<SpanDataCollection<ReferenceAndId>>(DocumentViewerContentDataIds.SpanReference) ?? SpanDataCollection<ReferenceAndId>.Empty;

			TextView.TextBuffer.ChangeContentType(contentType, null);
			cachedColorsList.Clear();
			cachedColorsList.Add(0, content.ColorCollection);

			// If it's the same text, don't update text buffer and caret. It can be the same text if
			// it's not been cached, eg. it's the resources or some other content with UI content.
			// This simulates the cached code path above which also doesn't move the caret. And as
			// an added bonus, it will use less memory and CPU.
			bool sameText = IsSameTextAsCurrentSnapshot(TextView.TextSnapshot, content.Text);
			if (!sameText) {
				TextView.TextBuffer.Replace(new Span(0, TextView.TextBuffer.CurrentSnapshot.Length), content.Text);
				TextView.Caret.MoveTo(new SnapshotPoint(TextView.TextSnapshot, 0));
				TextView.Caret.EnsureVisible();
				TextView.Selection.Clear();
			}

			return true;
		}

		static bool IsSameTextAsCurrentSnapshot(ITextSnapshot snapshot, string text) {
			if (snapshot.Length != text.Length)
				return false;
			if (text.Length == 0)
				return true;
			int mid = text.Length / 2;
			if (snapshot[mid] != text[mid])
				return false;
			var buf = new char[Math.Min(1024, text.Length)];
			int offset = 0;
			while (offset < text.Length) {
				int count = Math.Min(buf.Length, text.Length - offset);
				snapshot.CopyTo(offset, buf, 0, count);
				for (int i = 0, j = offset; i < count; i++, j++) {
					if (buf[i] != text[j])
						return false;
				}
				offset += count;
			}
			return true;
		}

		public bool GoToLocation(object reference, MoveCaretOptions options) {
			if (reference == null)
				return false;

			var member = reference as IMemberDef;
			if (member != null) {
				var spanData = currentContent.Content.ReferenceCollection.FirstOrNull(a => a.Data.IsDefinition && a.Data.Reference == member);
				return GoToTarget(spanData, false, false, options);
			}

			var pd = reference as ParamDef;
			if (pd != null) {
				var spanData = currentContent.Content.ReferenceCollection.FirstOrNull(a => a.Data.IsDefinition && (a.Data.Reference as Parameter)?.ParamDef == pd);
				return GoToTarget(spanData, false, false, options);
			}

			var textRef = reference as TextReference;
			if (textRef != null) {
				var spanData = currentContent.Content.ReferenceCollection.FirstOrNull(a => a.Data.IsLocal == textRef.IsLocal && a.Data.IsDefinition == textRef.IsDefinition && a.Data.Reference == textRef.Reference);
				return GoToTarget(spanData, false, false, options);
			}

			Debug.Fail(string.Format("Unknown type: {0} = {1}", reference.GetType(), reference));
			return false;
		}

		bool GoToTarget(SpanData<ReferenceInfo>? spanData, bool canFollowReference, bool canRecordHistory, MoveCaretOptions options) =>
			GoTo(spanData, false, true, canRecordHistory, canFollowReference, options);

		sealed class GoToHelper {
			readonly DocumentViewerControl owner;
			readonly SpanData<ReferenceInfo> spanData;
			readonly bool newTab;
			readonly bool followLocalRefs;
			readonly bool canRecordHistory;
			readonly bool canFollowReference;
			readonly MoveCaretOptions options;

			public GoToHelper(DocumentViewerControl owner, SpanData<ReferenceInfo> spanData, bool newTab, bool followLocalRefs, bool canRecordHistory, bool canFollowReference, MoveCaretOptions options) {
				this.owner = owner;
				this.spanData = spanData;
				this.newTab = newTab;
				this.followLocalRefs = followLocalRefs;
				this.canRecordHistory = canRecordHistory;
				this.canFollowReference = canFollowReference;
				this.options = options;
				owner.TextView.ViewportHeightChanged += TextView_ViewportHeightChanged;
			}

			void TextView_ViewportHeightChanged(object sender, EventArgs e) {
				Debug.Assert(owner.TextView.ViewportHeight != 0);
				owner.TextView.ViewportHeightChanged -= TextView_ViewportHeightChanged;
				owner.GoToCore(spanData, newTab, followLocalRefs, canRecordHistory, canFollowReference, options);
			}
		}

		internal bool GoTo(SpanData<ReferenceInfo>? spanData, bool newTab, bool followLocalRefs, bool canRecordHistory, bool canFollowReference, MoveCaretOptions options) {
			if (spanData == null)
				return false;

			// When opening a new tab, the textview isn't visible and has a 0 height, so wait until it's visible
			// before moving the caret.
			if (wpfTextViewHost.TextView.ViewportHeight == 0 && !wpfTextViewHost.TextView.VisualElement.IsVisible) {
				new GoToHelper(this, spanData.Value, newTab, followLocalRefs, canRecordHistory, canFollowReference, options);
				return true;
			}

			return GoToCore(spanData.Value, newTab, followLocalRefs, canRecordHistory, canFollowReference, options);
		}

		bool GoToCore(SpanData<ReferenceInfo> spanData, bool newTab, bool followLocalRefs, bool canRecordHistory, bool canFollowReference, MoveCaretOptions options) {
			Debug.Assert(spanData.Span.End <= wpfTextViewHost.TextView.TextSnapshot.Length);
			if (spanData.Span.End > wpfTextViewHost.TextView.TextSnapshot.Length)
				return false;

			if (newTab) {
				Debug.Assert(canFollowReference);
				if (!canFollowReference)
					return false;
				textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
				return true;
			}

			if (followLocalRefs) {
				if (!IsOwnerOf(spanData)) {
					if (!canFollowReference)
						return false;
					textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
					return true;
				}

				var localTarget = FindDefinition(spanData);
				if (localTarget != null)
					spanData = localTarget.Value;

				if (spanData.Data.IsDefinition) {
					if (canRecordHistory) {
						if (!canFollowReference)
							return false;
						textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
					}
					else
						MoveCaretToSpan(spanData.Span, options);
					return true;
				}

				if (spanData.Data.IsLocal)
					return false;
				if (!canFollowReference)
					return false;
				textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
				return true;
			}
			else {
				var localTarget = FindDefinition(spanData);
				if (localTarget != null)
					spanData = localTarget.Value;

				int pos = -1;
				if (!spanData.Data.IsLocal) {
					if (spanData.Data.IsDefinition)
						pos = spanData.Span.End;
				}
				if (pos >= 0) {
					if (canRecordHistory) {
						if (!canFollowReference)
							return false;
						textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
					}
					else {
						if ((options & MoveCaretOptions.Focus) != 0)
							textEditorHelper.SetFocus();
						wpfTextViewHost.TextView.Selection.Clear();
						wpfTextViewHost.TextView.Caret.MoveTo(new SnapshotPoint(wpfTextViewHost.TextView.TextSnapshot, pos));
						wpfTextViewHost.TextView.EnsureCaretVisible((options & MoveCaretOptions.Center) != 0);
					}
					return true;
				}

				if (spanData.Data.IsLocal)
					return false;	// Allow another handler to set a new caret position

				if ((options & MoveCaretOptions.Focus) != 0)
					textEditorHelper.SetFocus();
				if (!canFollowReference)
					return false;
				textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
				return true;
			}
		}

		bool IsOwnerOf(SpanData<ReferenceInfo> refInfo) {
			var other = currentContent.Content.ReferenceCollection.Find(refInfo.Span.Start);
			return other != null &&
				other.Value.Span == refInfo.Span &&
				other.Value.Data == refInfo.Data;
		}

		SpanData<ReferenceInfo>? FindDefinition(SpanData<ReferenceInfo> spanData) {
			if (spanData.Data.IsDefinition)
				return spanData;
			return currentContent.Content.ReferenceCollection.FirstOrNull(other => other.Data.IsDefinition && SpanDataReferenceInfoExtensions.CompareReferences(other.Data, spanData.Data));
		}

		public SpanData<ReferenceInfo>? GetCurrentReferenceInfo() {
			var caretPos = wpfTextViewHost.TextView.Caret.Position;
			var spanData = SpanDataCollectionUtilities.GetCurrentSpanReference(currentContent.Content.ReferenceCollection, wpfTextViewHost.TextView);
			return spanData?.Data.Reference == null ? null : spanData;
		}

		public SpanData<ReferenceInfo>? GetReferenceInfo(int position) => currentContent.Content.ReferenceCollection.Find(position);

		public IEnumerable<SpanData<ReferenceInfo>> GetSelectedTextReferences() {
			var selection = wpfTextViewHost.TextView.Selection;
			if (selection.IsEmpty)
				yield break;
			var referenceCollection = currentContent.Content.ReferenceCollection;
			foreach (var vspan in selection.VirtualSelectedSpans) {
				var span = vspan.SnapshotSpan;
				foreach (var spanData in referenceCollection.Find(span.Span))
					yield return spanData;
			}
		}

		public void MoveCaretToPosition(int position, MoveCaretOptions options) {
			var snapshot = wpfTextViewHost.TextView.TextSnapshot;
			if ((uint)position < (uint)snapshot.Length) {
				wpfTextViewHost.TextView.Caret.MoveTo(new SnapshotPoint(snapshot, position));
				wpfTextViewHost.TextView.EnsureCaretVisible((options & MoveCaretOptions.Center) != 0);
			}
			wpfTextViewHost.TextView.Selection.Clear();
			if ((options & MoveCaretOptions.Focus) != 0)
				textEditorHelper.SetFocus();
		}

		public object SaveReferencePosition(IMethodDebugService methodDebugService) => GetReferencePosition(methodDebugService);

		public bool RestoreReferencePosition(IMethodDebugService methodDebugService, object obj) {
			var referencePosition = obj as ReferencePosition;
			if (referencePosition == null)
				return false;
			return GoTo(methodDebugService, referencePosition, MoveCaretOptions.None);
		}

		sealed class ReferencePosition {
			public MethodSourceStatement? MethodSourceStatement { get; }
			public SpanData<ReferenceInfo>? SpanData { get; }

			public ReferencePosition(SpanData<ReferenceInfo> spanData) {
				this.SpanData = spanData;
			}

			public ReferencePosition(IList<MethodSourceStatement> methodSourceStatements) {
				this.MethodSourceStatement = methodSourceStatements.Count > 0 ? methodSourceStatements[0] : (MethodSourceStatement?)null;
			}
		}

		ReferencePosition GetReferencePosition(IMethodDebugService methodDebugService) {
			int caretPos = wpfTextViewHost.TextView.Caret.Position.BufferPosition.Position;
			var line = wpfTextViewHost.TextView.TextSnapshot.GetLineFromPosition(caretPos);
			var statements = methodDebugService.FindByTextPosition(caretPos, sameMethod: false).ToList();
			statements.Sort(sortDelegate);

			var spanData = currentContent.Content.ReferenceCollection.FindFrom(line.Start.Position).FirstOrDefault(r => r.Data.Reference is IMemberDef && r.Data.IsDefinition && !r.Data.IsLocal);
			if (statements.Count == 0) {
				if (spanData.Data.Reference != null)
					return new ReferencePosition(spanData);
			}
			else if (spanData.Data.Reference == null)
				return new ReferencePosition(statements);
			else {
				if (statements[0].Statement.TextSpan.Start < spanData.Span.Start)
					return new ReferencePosition(statements);
				return new ReferencePosition(spanData);
			}

			return null;
		}

		static int Sort(MethodSourceStatement a, MethodSourceStatement b) => a.Statement.TextSpan.Start - b.Statement.TextSpan.Start;
		static readonly Comparison<MethodSourceStatement> sortDelegate = Sort;

		bool GoTo(IMethodDebugService methodDebugService, ReferencePosition referencePosition, MoveCaretOptions options) {
			if (referencePosition == null)
				return false;

			if (referencePosition.MethodSourceStatement != null) {
				var methodSourceStatement = referencePosition.MethodSourceStatement.Value;
				var methodStatement = methodDebugService.FindByCodeOffset(methodSourceStatement.Method, methodSourceStatement.Statement.BinSpan.Start);
				if (methodStatement != null) {
					MoveCaretToPosition(methodStatement.Value.Statement.TextSpan.Start, options);
					return true;
				}
			}

			if (referencePosition.SpanData != null) {
				var spanData = FindReferenceInfo(referencePosition.SpanData.Value);
				if (spanData != null)
					return GoToTarget(spanData, false, false, options);
			}

			return false;
		}

		SpanData<ReferenceInfo>? FindReferenceInfo(SpanData<ReferenceInfo> spanData) {
			foreach (var other in currentContent.Content.ReferenceCollection) {
				if (other.Data.IsLocal == spanData.Data.IsLocal && other.Data.IsDefinition == spanData.Data.IsDefinition && SpanDataReferenceInfoExtensions.CompareReferences(other.Data, spanData.Data))
					return other;
			}
			return null;
		}

		public void MoveReference(bool forward) {
			// Check these references first because if the caret is at a Get declaration (VB), then there's
			// no code references to it, but there's a 'Get' and 'End Get' in these refs that should be used.
			var spanRefData = SpanDataCollectionUtilities.GetCurrentSpanReference(spanReferenceCollection, TextView);
			if (spanRefData?.Data.Reference != null) {
				foreach (var newSpanData in GetReferenceInfosFrom(spanReferenceCollection, spanRefData.Value.Span.Start, forward)) {
					if (object.Equals(newSpanData.Data.Reference, spanRefData.Value.Data.Reference)) {
						MoveCaretToSpan(newSpanData.Span, MoveCaretOptions.Focus | MoveCaretOptions.Select);
						break;
					}
				}
				return;
			}

			var spanData = GetCurrentReferenceInfo();
			if (spanData != null && !spanData.Value.Data.IsHidden) {
				foreach (var newSpanData in GetReferenceInfosFrom(spanData.Value.Span.Start, forward)) {
					if (!newSpanData.Data.IsHidden && SpanDataReferenceInfoExtensions.CompareReferences(newSpanData.Data, spanData.Value.Data)) {
						MoveCaretToSpan(newSpanData.Span, MoveCaretOptions.Focus | MoveCaretOptions.Select);
						break;
					}
				}
				return;
			}
		}

		public void MoveToNextDefinition(bool forward) {
			int offset = wpfTextViewHost.TextView.Caret.Position.BufferPosition.Position;
			foreach (var newSpanData in GetReferenceInfosFrom(offset, forward)) {
				if (newSpanData.Data.IsDefinition && newSpanData.Data.Reference is IMemberDef) {
					MoveCaretToSpan(newSpanData.Span, MoveCaretOptions.Focus | MoveCaretOptions.Select);
					break;
				}
			}
		}

		public void MoveCaretToSpan(Span span, MoveCaretOptions options) {
			var snapshot = wpfTextViewHost.TextView.TextSnapshot;
			Debug.Assert(span.End <= snapshot.Length);
			if (span.End > snapshot.Length)
				return;
			MoveCaretToPosition(span.End, options);

			bool isReversed = false;
			// If there's another reference at the caret, move caret to Start instead of End
			var nextRef = GetReferenceInfo(span.End);
			if (nextRef != null && nextRef.Value.Span != span) {
				wpfTextViewHost.TextView.Caret.MoveTo(new SnapshotPoint(snapshot, span.Start));
				isReversed = true;
			}

			if ((options & MoveCaretOptions.Select) == 0)
				wpfTextViewHost.TextView.Selection.Clear();
			else {
				wpfTextViewHost.TextView.Selection.Mode = TextSelectionMode.Stream;
				wpfTextViewHost.TextView.Selection.Select(new SnapshotSpan(snapshot, span), isReversed);
			}
		}

		IEnumerable<SpanData<ReferenceInfo>> GetReferenceInfosFrom(int position, bool forward) =>
			GetReferenceInfosFrom(currentContent.Content.ReferenceCollection, position, forward);

		static IEnumerable<SpanData<TData>> GetReferenceInfosFrom<TData>(SpanDataCollection<TData> referenceCollection, int position, bool forward) {
			if (referenceCollection.Count == 0)
				yield break;

			int startIndex = referenceCollection.GetStartIndex(position);
			// If it's between two refs, always prefer the one whose Start == position
			if (startIndex >= 0 && startIndex + 1 < referenceCollection.Count && referenceCollection[startIndex + 1].Span.Start == position)
				startIndex = startIndex + 1;
			if (forward) {
				if (startIndex < 0)
					startIndex = referenceCollection.Count - 1;

				for (int i = 0; i < referenceCollection.Count; i++) {
					int index = (startIndex + i + 1) % referenceCollection.Count;
					yield return referenceCollection[index];
				}
			}
			else {
				if (startIndex < 0)
					startIndex = 0;

				for (int i = 0; i < referenceCollection.Count; i++) {
					int index = (referenceCollection.Count + startIndex - (i + 1)) % referenceCollection.Count;
					yield return referenceCollection[index];
				}
			}
		}

		public void FollowReference() => GoToTarget(GetCurrentReferenceInfo(), true, true, MoveCaretOptions.Focus | MoveCaretOptions.Select);

		public void FollowReferenceNewTab() {
			if (textEditorHelper == null)
				return;
			GoTo(GetCurrentReferenceInfo(), true, true, true, true, MoveCaretOptions.Focus | MoveCaretOptions.Select);
		}

		public void Clear() {
			CurrentWaitAdorner = null;
			cachedColorsList.Clear();
			currentContent = new CurrentContent(emptyContent, defaultContentType);
			spanReferenceCollection = SpanDataCollection<ReferenceAndId>.Empty;
			wpfTextViewHost.TextView.TextBuffer.Replace(new Span(0, wpfTextViewHost.TextView.TextBuffer.CurrentSnapshot.Length), string.Empty);
		}

		public void Dispose() {
			Clear();
			if (!wpfTextViewHost.IsClosed)
				wpfTextViewHost.Close();
		}
	}
}
