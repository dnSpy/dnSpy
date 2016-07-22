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
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Decompiler.Shared;
using dnSpy.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Files.Tabs.DocViewer {
	sealed class DocumentViewerControl : Grid {
		public IDnSpyWpfTextViewHost TextViewHost => wpfTextViewHost;
		public IDnSpyWpfTextView TextView => wpfTextViewHost.TextView;
		public DocumentViewerContent Content => currentContent.Content;
		readonly DocumentViewerContent emptyContent;

		readonly CachedColorsList cachedColorsList;
		readonly IContentType defaultContentType;
		readonly IDnSpyWpfTextViewHost wpfTextViewHost;
		readonly IDocumentViewerHelper textEditorHelper;

		static readonly string[] defaultRoles = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Debuggable,
			PredefinedTextViewRoles.Document,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.Structured,
			PredefinedTextViewRoles.Zoomable,
			DocumentViewerConstants.TextViewRole,
		};

		public DocumentViewerControl(ITextBufferFactoryService textBufferFactoryService, IDnSpyTextEditorFactoryService dnSpyTextEditorFactoryService, IDocumentViewerHelper textEditorHelper) {
			if (textBufferFactoryService == null)
				throw new ArgumentNullException(nameof(textBufferFactoryService));
			if (dnSpyTextEditorFactoryService == null)
				throw new ArgumentNullException(nameof(dnSpyTextEditorFactoryService));
			if (textEditorHelper == null)
				throw new ArgumentNullException(nameof(textEditorHelper));
			this.textEditorHelper = textEditorHelper;
			this.defaultContentType = textBufferFactoryService.TextContentType;
			this.cachedColorsList = new CachedColorsList();
			this.emptyContent = new DocumentViewerContent(string.Empty, CachedTextTokenColors.Empty, SpanDataCollection<ReferenceInfo>.Empty, Array.Empty<MemberMapping>());
			this.currentContent = new CurrentContent(emptyContent, defaultContentType);

			var textBuffer = textBufferFactoryService.CreateTextBuffer(textBufferFactoryService.TextContentType);
			CachedColorsListTaggerProvider.AddColorizer(textBuffer, cachedColorsList);
			var roles = dnSpyTextEditorFactoryService.CreateTextViewRoleSet(defaultRoles);
			var textView = dnSpyTextEditorFactoryService.CreateTextView(textBuffer, roles, (TextViewCreatorOptions)null);
			var wpfTextViewHost = dnSpyTextEditorFactoryService.CreateTextViewHost(textView, false);
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

			TextView.TextBuffer.ChangeContentType(contentType, null);
			cachedColorsList.Clear();
			cachedColorsList.Add(0, content.ColorCollection);
			TextView.TextBuffer.Replace(new Span(0, TextView.TextBuffer.CurrentSnapshot.Length), content.Text);
			TextView.Selection.Clear();
			TextView.Caret.MoveTo(new SnapshotPoint(TextView.TextSnapshot, 0));
			return true;
		}

		public bool GoToLocation(object reference) {
			if (reference == null)
				return false;

			var member = reference as IMemberDef;
			if (member != null) {
				var spanData = currentContent.Content.ReferenceCollection.FirstOrNull(a => a.Data.IsDefinition && a.Data.Reference == member);
				return GoToTarget(spanData, false, false);
			}

			var pd = reference as ParamDef;
			if (pd != null) {
				var spanData = currentContent.Content.ReferenceCollection.FirstOrNull(a => a.Data.IsDefinition && (a.Data.Reference as Parameter)?.ParamDef == pd);
				return GoToTarget(spanData, false, false);
			}

			var textRef = reference as TextReference;
			if (textRef != null) {
				var spanData = currentContent.Content.ReferenceCollection.FirstOrNull(a => a.Data.IsLocal == textRef.IsLocal && a.Data.IsDefinition == textRef.IsDefinition && a.Data.Reference == textRef.Reference);
				return GoToTarget(spanData, false, false);
			}

			Debug.Fail(string.Format("Unknown type: {0} = {1}", reference.GetType(), reference));
			return false;
		}

		bool GoToTarget(SpanData<ReferenceInfo>? spanData, bool canJumpToReference, bool canRecordHistory) =>
			GoTo(spanData, false, true, canRecordHistory, canJumpToReference);

		sealed class GoToHelper {
			readonly DocumentViewerControl owner;
			readonly SpanData<ReferenceInfo> spanData;
			readonly bool newTab;
			readonly bool followLocalRefs;
			readonly bool canRecordHistory;
			readonly bool canJumpToReference;

			public GoToHelper(DocumentViewerControl owner, SpanData<ReferenceInfo> spanData, bool newTab, bool followLocalRefs, bool canRecordHistory, bool canJumpToReference) {
				this.owner = owner;
				this.spanData = spanData;
				this.newTab = newTab;
				this.followLocalRefs = followLocalRefs;
				this.canRecordHistory = canRecordHistory;
				this.canJumpToReference = canJumpToReference;
				owner.TextView.ViewportHeightChanged += TextView_ViewportHeightChanged;
			}

			void TextView_ViewportHeightChanged(object sender, EventArgs e) {
				Debug.Assert(owner.TextView.ViewportHeight != 0);
				owner.TextView.ViewportHeightChanged -= TextView_ViewportHeightChanged;
				owner.GoToCore(spanData, newTab, followLocalRefs, canRecordHistory, canJumpToReference);
			}
		}

		bool GoTo(SpanData<ReferenceInfo>? spanData, bool newTab, bool followLocalRefs, bool canRecordHistory, bool canJumpToReference) {
			if (spanData == null)
				return false;

			// When opening a new tab, the textview isn't visible and has a 0 height, so wait until it's visible
			// before moving the caret.
			if (wpfTextViewHost.TextView.ViewportHeight == 0 && !wpfTextViewHost.TextView.VisualElement.IsVisible) {
				new GoToHelper(this, spanData.Value, newTab, followLocalRefs, canRecordHistory, canJumpToReference);
				return true;
			}

			return GoToCore(spanData.Value, newTab, followLocalRefs, canRecordHistory, canJumpToReference);
		}

		bool GoToCore(SpanData<ReferenceInfo> spanData, bool newTab, bool followLocalRefs, bool canRecordHistory, bool canJumpToReference) {
			Debug.Assert(spanData.Span.End <= wpfTextViewHost.TextView.TextSnapshot.Length);
			if (spanData.Span.End > wpfTextViewHost.TextView.TextSnapshot.Length)
				return false;

			if (newTab) {
				Debug.Assert(canJumpToReference);
				if (!canJumpToReference)
					return false;
				textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
				return true;
			}

			if (followLocalRefs) {
				if (!IsOwnerOf(spanData)) {
					if (!canJumpToReference)
						return false;
					textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
					return true;
				}

				var localTarget = FindDefinition(spanData);
				if (localTarget != null)
					spanData = localTarget.Value;

				if (spanData.Data.IsDefinition) {
					if (canRecordHistory) {
						if (!canJumpToReference)
							return false;
						textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
					}
					else {
						var line = wpfTextViewHost.TextView.TextSnapshot.GetLineFromPosition(spanData.Span.Start);
						int column = spanData.Span.Start - line.Start.Position;
						ScrollAndMoveCaretTo(line.LineNumber, column);
					}
					return true;
				}

				if (spanData.Data.IsLocal)
					return false;
				if (!canJumpToReference)
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
						if (!canJumpToReference)
							return false;
						textEditorHelper.FollowReference(spanData.ToTextReference(), newTab);
					}
					else {
						textEditorHelper.SetFocus();
						wpfTextViewHost.TextView.Selection.Clear();
						wpfTextViewHost.TextView.Caret.MoveTo(new SnapshotPoint(wpfTextViewHost.TextView.TextSnapshot, pos));
						wpfTextViewHost.TextView.Caret.EnsureVisible();//TODO: Use wpfTextViewHost.TextView.ViewScroller.EnsureSpanVisible()
					}
					return true;
				}

				if (spanData.Data.IsLocal)
					return false;	// Allow another handler to set a new caret position

				textEditorHelper.SetFocus();
				if (!canJumpToReference)
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
			// There are no refs in virtual space
			if (caretPos.VirtualSpaces > 0)
				return null;
			var pos = caretPos.BufferPosition;
			// If it's at the end of a word wrapped line, don't mark the reference that's
			// shown on the next line.
			if (caretPos.Affinity == PositionAffinity.Predecessor && pos.Position != 0)
				pos = pos - 1;
			var spanData = GetReferenceInfo(pos.Position);
			if (spanData == null)
				return null;
			return spanData.Value.Data.Reference == null ? null : spanData;
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

		public void ScrollAndMoveCaretTo(int line, int column, bool focus = true) {
			wpfTextViewHost.TextView.MoveCaretTo(line, column);
			wpfTextViewHost.TextView.Caret.EnsureVisible();//TODO: Use wpfTextViewHost.TextView.ViewScroller.EnsureSpanVisible()
			wpfTextViewHost.TextView.Selection.Clear();
			if (focus)
				textEditorHelper.SetFocus();
		}

		public object SaveReferencePosition(ICodeMappings codeMappings) => GetReferencePosition(codeMappings);

		public bool RestoreReferencePosition(ICodeMappings codeMappings, object obj) {
			var referencePosition = obj as ReferencePosition;
			if (referencePosition == null)
				return false;
			return GoTo(codeMappings, referencePosition);
		}

		sealed class ReferencePosition {
			public SourceCodeMapping SourceCodeMapping { get; }
			public SpanData<ReferenceInfo>? SpanData { get; }

			public ReferencePosition(SpanData<ReferenceInfo> spanData) {
				this.SpanData = spanData;
			}

			public ReferencePosition(IList<SourceCodeMapping> sourceCodeMappings) {
				this.SourceCodeMapping = sourceCodeMappings.Count > 0 ? sourceCodeMappings[0] : null;
			}
		}

		ReferencePosition GetReferencePosition(ICodeMappings codeMappings) {
			int caretPos = wpfTextViewHost.TextView.Caret.Position.BufferPosition.Position;
			var line = wpfTextViewHost.TextView.TextSnapshot.GetLineFromPosition(caretPos);
			var mappings = codeMappings.Find(line.LineNumber, caretPos - line.Start.Position).ToList();
			mappings.Sort(Sort);
			var mapping = mappings.Count == 0 ? null : mappings[0];

			int position = line.Start.Position;
			var spanData = currentContent.Content.ReferenceCollection.FindFrom(position).FirstOrDefault(r => r.Data.Reference is IMemberDef && r.Data.IsDefinition && !r.Data.IsLocal);

			if (mapping == null) {
				if (spanData.Data.Reference != null)
					return new ReferencePosition(spanData);
			}
			else if (spanData.Data.Reference == null)
				return new ReferencePosition(mappings);
			else {
				position = wpfTextViewHost.TextView.LineColumnToPosition(mapping.StartPosition.Line, mapping.StartPosition.Column);
				if (position < spanData.Span.Start)
					return new ReferencePosition(mappings);
				return new ReferencePosition(spanData);
			}

			return null;
		}

		static int Sort(SourceCodeMapping a, SourceCodeMapping b) => a.StartPosition.CompareTo(b.StartPosition);

		bool GoTo(ICodeMappings codeMappings, ReferencePosition referencePosition) {
			if (referencePosition == null)
				return false;

			if (referencePosition.SourceCodeMapping != null) {
				var mapping = referencePosition.SourceCodeMapping;
				var codeMapping = codeMappings.Find(mapping.Mapping.Method, mapping.ILRange.From);
				if (codeMapping != null) {
					ScrollAndMoveCaretTo(codeMapping.StartPosition.Line, codeMapping.StartPosition.Column);
					return true;
				}
			}

			if (referencePosition.SpanData != null) {
				var spanData = FindReferenceInfo(referencePosition.SpanData.Value);
				if (spanData != null)
					return GoToTarget(spanData, false, false);
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
			var spanData = GetCurrentReferenceInfo();
			if (spanData == null)
				return;

			foreach (var newSpanData in GetReferenceInfosFrom(spanData.Value.Span.Start, forward)) {
				if (SpanDataReferenceInfoExtensions.CompareReferences(newSpanData.Data, spanData.Value.Data)) {
					MoveToSpan(newSpanData);
					break;
				}
			}
		}

		public void MoveToNextDefinition(bool forward) {
			int offset = wpfTextViewHost.TextView.Caret.Position.BufferPosition.Position;
			foreach (var newSpanData in GetReferenceInfosFrom(offset, forward)) {
				if (newSpanData.Data.IsDefinition && newSpanData.Data.Reference is IMemberDef) {
					MoveToSpan(newSpanData);
					break;
				}
			}
		}

		void MoveToSpan(SpanData<ReferenceInfo> spanData) {
			var snapshot = wpfTextViewHost.TextView.TextSnapshot;
			Debug.Assert(spanData.Span.End <= snapshot.Length);
			if (spanData.Span.End > snapshot.Length)
				return;
			var line = snapshot.GetLineFromPosition(spanData.Span.Start);
			int column = spanData.Span.End - line.Start.Position;
			ScrollAndMoveCaretTo(line.LineNumber, column);
			wpfTextViewHost.TextView.Selection.Mode = TextSelectionMode.Stream;
			wpfTextViewHost.TextView.Selection.Select(new SnapshotSpan(snapshot, spanData.Span), false);

			// If there's another reference at the caret, move caret to Start instead of End
			var nextRef = GetReferenceInfo(spanData.Span.End);
			if (nextRef != null && nextRef.Value.Span != spanData.Span)
				wpfTextViewHost.TextView.Caret.MoveTo(new SnapshotPoint(snapshot, spanData.Span.Start));
		}

		IEnumerable<SpanData<ReferenceInfo>> GetReferenceInfosFrom(int position, bool forward) {
			var referenceCollection = currentContent.Content.ReferenceCollection;
			if (referenceCollection.Count == 0)
				yield break;

			if (forward) {
				int startIndex = referenceCollection.GetStartIndex(position);
				if (startIndex < 0)
					startIndex = referenceCollection.Count - 1;

				for (int i = 0; i < referenceCollection.Count; i++) {
					int index = (startIndex + i + 1) % referenceCollection.Count;
					yield return referenceCollection[index];
				}
			}
			else {
				int startIndex = referenceCollection.GetStartIndex(position);
				if (startIndex < 0)
					startIndex = 0;

				for (int i = 0; i < referenceCollection.Count; i++) {
					int index = (referenceCollection.Count + startIndex - (i + 1)) % referenceCollection.Count;
					yield return referenceCollection[index];
				}
			}
		}

		public void FollowReference() => GoToTarget(GetCurrentReferenceInfo(), true, true);

		public void FollowReferenceNewTab() {
			if (textEditorHelper == null)
				return;
			GoTo(GetCurrentReferenceInfo(), true, true, true, true);
		}

		public void Clear() {
			CurrentWaitAdorner = null;
			cachedColorsList.Clear();
			currentContent = new CurrentContent(emptyContent, defaultContentType);
			wpfTextViewHost.TextView.TextBuffer.Replace(new Span(0, wpfTextViewHost.TextView.TextBuffer.CurrentSnapshot.Length), string.Empty);
		}

		public void Dispose() {
			Clear();
			if (!wpfTextViewHost.IsClosed)
				wpfTextViewHost.Close();
		}
	}
}
