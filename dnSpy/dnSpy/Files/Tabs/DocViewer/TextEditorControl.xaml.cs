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

// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;
using dnSpy.Decompiler.Shared;
using dnSpy.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Shared.Decompiler;
using dnSpy.Text;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Files.Tabs.DocViewer {
	sealed partial class TextEditorControl : UserControl, IDisposable {
		readonly ITextEditorHelper textEditorHelper;
		readonly CachedColorsList cachedColorsList;
		readonly IThemeManager themeManager;
		readonly IconBarMargin iconBarMargin;

		public IDnSpyWpfTextViewHost TextViewHost => wpfTextViewHost;
		public IDnSpyWpfTextView TextView => wpfTextViewHost.TextView;
		public ICSharpCode.AvalonEdit.TextEditor TextEditor { get; }
		public IEnumerable<object> AllReferences => references.Select(a => a.Reference);

		DefinitionLookup definitionLookup;
		TextSegmentCollection<ReferenceSegment> references;
		//readonly TextMarkerService textMarkerService;
		readonly List<ITextMarker> markedReferences = new List<ITextMarker>();

		readonly ReferenceElementGenerator referenceElementGenerator;
		readonly UIElementGenerator uiElementGenerator;
		List<VisualLineElementGenerator> activeCustomElementGenerators = new List<VisualLineElementGenerator>();

		readonly ToolTipHelper toolTipHelper;
		readonly ITextEditorSettings textEditorSettings;
		readonly IContentType defaultContentType;
		readonly IDnSpyWpfTextView wpfTextView;
		readonly IDnSpyWpfTextViewHost wpfTextViewHost;
		readonly IEditorOperations editorOperations;

		static readonly string[] defaultRoles = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Debuggable,
			PredefinedTextViewRoles.Document,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.Structured,
			PredefinedTextViewRoles.Zoomable,
			FileTabTextViewRoles.FileTab,
		};

		internal static TextEditorControl TryGetInstance(ITextView textView) {
			TextEditorControl instance;
			textView.Properties.TryGetProperty(typeof(TextEditorControl), out instance);
			return instance;
		}

		public TextEditorControl(IThemeManager themeManager, ToolTipHelper toolTipHelper, ITextEditorSettings textEditorSettings, ITextEditorUIContext uiContext, ITextEditorHelper textEditorHelper, ITextLineObjectManager textLineObjectManager, IImageManager imageManager, IIconBarCommandManager iconBarCommandManager, ITextBufferFactoryService textBufferFactoryService, IDnSpyTextEditorFactoryService dnSpyTextEditorFactoryService, IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.references = new TextSegmentCollection<ReferenceSegment>();
			this.themeManager = themeManager;
			this.toolTipHelper = toolTipHelper;
			this.textEditorSettings = textEditorSettings;
			this.textEditorHelper = textEditorHelper;
			this.defaultContentType = textBufferFactoryService.TextContentType;
			this.cachedColorsList = new CachedColorsList();
			InitializeComponent();
			this.textEditorSettings.PropertyChanged += TextEditorSettings_PropertyChanged;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;

			var textBuffer = textBufferFactoryService.CreateTextBuffer(textBufferFactoryService.TextContentType);
			CachedColorsListTaggerProvider.AddColorizer(textBuffer, cachedColorsList);
			var roles = dnSpyTextEditorFactoryService.CreateTextViewRoleSet(defaultRoles);
			IDnSpyWpfTextView textView = null;//textEditorFactoryService2.CreateTextView(textBuffer, roles, new TextViewCreatorOptions(), null);
			var wpfTextViewHost = dnSpyTextEditorFactoryService.CreateTextViewHost(textView, false);
			this.wpfTextViewHost = wpfTextViewHost;
			this.wpfTextView = wpfTextViewHost.TextView;
			this.editorOperations = editorOperationsFactoryService.GetEditorOperations(wpfTextView);
			wpfTextView.Properties.AddProperty(typeof(TextEditorControl), this);
			wpfTextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategoryConstants.Viewer);
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId, true);
			wpfTextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, true);
			//TextEditor = textView.DnSpyTextEditor;
			this.toolTipHelper.Initialize(TextEditor);
			dnSpyTextEditor.Content = wpfTextViewHost.HostControl;

			referenceElementGenerator = new ReferenceElementGenerator(JumpToReference, a => true);
			// Add the ref elem generator first in case one of the refs looks like a http link etc
			TextEditor.TextArea.TextView.ElementGenerators.Insert(0, referenceElementGenerator);
			this.uiElementGenerator = new UIElementGenerator();
			TextEditor.TextArea.TextView.ElementGenerators.Add(uiElementGenerator);

			iconBarMargin = new IconBarMargin(uiContext, textLineObjectManager, imageManager, themeManager);
			iconBarCommandManager.Initialize(iconBarMargin);
			TextEditor.TextArea.LeftMargins.Insert(0, iconBarMargin);
			TextEditor.TextArea.TextView.VisualLinesChanged += (s, e) => iconBarMargin.InvalidateVisual();

			//textMarkerService = new TextMarkerService(this, uiContext, textLineObjectManager);
			//TextEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
			//TextEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);

			wpfTextView.Caret.PositionChanged += Caret_PositionChanged;

			OnAutoHighlightRefsChanged();
		}

		public Button CancelButton => (this.waitAdorner.Content as WaitAdorner)?.button;

		public void ShowCancelButton(Action onCancel, string msg) {
			var wa = new WaitAdorner(onCancel, msg);
			this.waitAdorner.Content = wa;

			// Prevents flickering when decompiling small classes
			wa.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)), FillBehavior.Stop));

			wa.MouseDown += (s, e) => e.Handled = true;
			wa.MouseUp += (s, e) => e.Handled = true;
			wa.button.IsVisibleChanged += (s, e) => {
				if (wa != this.waitAdorner.Content)
					return;
				if (wa.button.IsVisible && IsKeyboardFocusWithin)
					wa.button.Focus();
			};

			if (IsKeyboardFocusWithin)
				wa.button.Focus();
		}

		public void HideCancelButton() {
			var wa = this.waitAdorner.Content as WaitAdorner;
			// It contains a progress bar that can still be shown on the screen if some older
			// version of the .NET Framework is used. I could reproduce it with .NET 4 + VMWare + XP.
			// Also frees the hard ref to the onCancel() delegate.
			this.waitAdorner.Content = null;
			if (wa?.IsKeyboardFocusWithin == true)
				this.textEditorHelper.SetFocus();
		}

		void ClearCustomElementGenerators() {
			foreach (var elementGenerator in activeCustomElementGenerators)
				TextEditor.TextArea.TextView.ElementGenerators.Remove(elementGenerator);
			activeCustomElementGenerators.Clear();
		}

		struct LastOutput : IEquatable<LastOutput> {
			readonly ITextOutput output;
			readonly IHighlightingDefinition highlighting;
			readonly IContentType contentType;

			public LastOutput(ITextOutput output, IHighlightingDefinition highlighting, IContentType contentType) {
				this.output = output;
				this.highlighting = highlighting;
				this.contentType = contentType;
			}

			public bool Equals(LastOutput other) => output == other.output && highlighting == other.highlighting && contentType == other.contentType;

			public override bool Equals(object obj) {
				if (obj is LastOutput)
					return Equals((LastOutput)obj);
				return false;
			}

			public override int GetHashCode() => (output?.GetHashCode() ?? 0) ^ (highlighting?.GetHashCode() ?? 0) ^ (contentType?.GetHashCode() ?? 0);
		}

		public void OnUseNewRendererChanged() => lastOutput = new LastOutput();

		LastOutput lastOutput;
		public void SetOutput(ITextOutput output, IHighlightingDefinition highlighting, IContentType contentType) {
			if (output == null)
				throw new ArgumentNullException();
			if (contentType == null)
				contentType = defaultContentType;

			HideCancelButton();

			var newLastOutput = new LastOutput(output, highlighting, contentType);
			if (lastOutput.Equals(newLastOutput))
				return;
			lastOutput = newLastOutput;

			var avOutput = output as AvalonEditTextOutput;
			Debug.Assert(avOutput != null, "output should be an AvalonEditTextOutput instance");

			ClearMarkedReferences();
			editorOperations.MoveToStartOfDocument(false);
			TextEditor.SyntaxHighlighting = highlighting;
			ClearCustomElementGenerators();

			string newText;
			if (avOutput == null) {
				uiElementGenerator.UIElements = null;
				referenceElementGenerator.References = null;
				references = new TextSegmentCollection<ReferenceSegment>();
				definitionLookup = null;
				newText = output.ToString();
			}
			else {
				uiElementGenerator.UIElements = avOutput.UIElements;
				referenceElementGenerator.References = avOutput.References;
				references = avOutput.References;
				definitionLookup = avOutput.DefinitionLookup;
				foreach (var elementGenerator in avOutput.ElementGenerators) {
					TextEditor.TextArea.TextView.ElementGenerators.Add(elementGenerator);
					activeCustomElementGenerators.Add(elementGenerator);
				}

				newText = avOutput.GetCachedText();
			}

			wpfTextView.TextBuffer.ChangeContentType(contentType, null);

			var cachedColors = avOutput?.CachedColors ?? new CachedTextTokenColors();
			cachedColors.Finish();
			cachedColorsList.Clear();
			cachedColorsList.Add(0, cachedColors);
			wpfTextView.TextBuffer.Replace(new Span(0, wpfTextView.TextBuffer.CurrentSnapshot.Length), newText);
			wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, 0));
			TextEditor.TextArea.TextView.Document.UndoStack.ClearAll();
			//textMarkerService.OnTextChanged();
		}

		public void Clear() {
			ClearMarkedReferences();
			ClearCustomElementGenerators();
			wpfTextView.TextBuffer.Replace(new Span(0, wpfTextView.TextBuffer.CurrentSnapshot.Length), string.Empty);
			TextEditor.TextArea.TextView.Document.UndoStack.ClearAll();
			definitionLookup = null;
			uiElementGenerator.UIElements = null;
			referenceElementGenerator.References = null;
			references = new TextSegmentCollection<ReferenceSegment>();
			lastOutput = new LastOutput();
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) => iconBarMargin.InvalidateVisual();

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) {
			toolTipHelper.Close();

			OnAutoHighlightRefsChanged();
		}

		internal void ClearMarkedReferencesAndToolTip() {
			ClearMarkedReferences();
			toolTipHelper.Close();
			wpfTextView.Selection.Clear();
		}

		public object GetReferenceSegmentAt(MouseEventArgs e) {
			var tv = TextEditor.TextArea.TextView;
			var pos = tv.GetPosition(e.GetPosition(tv) + tv.ScrollOffset);
			if (pos == null)
				return null;
			int offset = LineColumnToOffset(pos.Value.Location);
			var seg = GetReferenceSegmentAt(offset);
			return seg?.Reference;
		}

		public ReferenceSegment GetReferenceSegmentAt(TextViewPosition? position) {
			if (position == null)
				return null;
			int offset = LineColumnToOffset(position.Value.Location);
			return GetReferenceSegmentAt(offset);
		}

		public ReferenceSegment GetCurrentReferenceSegment() => GetReferenceSegmentAt(wpfTextView.Caret.Position.BufferPosition.Position);

		public IEnumerable<TextReference> GetSelectedTextReferences() {
			if (wpfTextView.Selection.IsEmpty)
				yield break;
			int start = wpfTextView.Selection.Start.Position.Position;
			int end = wpfTextView.Selection.End.Position.Position;

			var refs = references;
			if (refs == null)
				yield break;
			var r = refs.FindFirstSegmentWithStartAfter(start);
			while (r != null) {
				if (r.StartOffset >= end)
					break;
				yield return r.ToTextReference();

				r = refs.GetNextSegment(r);
			}
		}

		public IEnumerable<Tuple<TextReference, TextEditorLocation>> GetTextReferences(int line, int column) {
			int offset = LineColumnToOffset(line, column);
			var refSeg = references.FindFirstSegmentWithStartAfter(offset);

			while (refSeg != null) {
				yield return Tuple.Create(refSeg.ToTextReference(), GetLocation(refSeg));
				refSeg = references.GetNextSegment(refSeg);
			}
		}

		TextEditorLocation GetLocation(ReferenceSegment refSeg) {
			var line = wpfTextView.TextSnapshot.GetLineFromPosition(refSeg.StartOffset);
			return new TextEditorLocation(line.LineNumber, refSeg.StartOffset - line.Start.Position);
		}

		public ReferenceSegment GetReferenceSegmentAt(int offset) {
			var segs = references.FindSegmentsContaining(offset).ToArray();
			foreach (var seg in segs) {
				if (seg.StartOffset <= offset && offset < seg.EndOffset)
					return seg;
			}
			return segs.Length == 0 ? null : segs[0];
		}

		internal void FollowReference() => GoToTarget(GetCurrentReferenceSegment(), true, true);

		internal void FollowReferenceNewTab() {
			if (textEditorHelper == null)
				return;
			GoTo(GetCurrentReferenceSegment(), true, true, true, true);
		}

		bool GoTo(ReferenceSegment refSeg, bool newTab, bool followLocalRefs, bool canRecordHistory, bool canJumpToReference) {
			if (refSeg == null)
				return false;

			if (newTab) {
				Debug.Assert(canJumpToReference);
				if (!canJumpToReference)
					return false;
				textEditorHelper.FollowReference(refSeg.ToTextReference(), newTab);
				return true;
			}

			if (followLocalRefs) {
				if (!IsOwnerOf(refSeg)) {
					if (!canJumpToReference)
						return false;
					textEditorHelper.FollowReference(refSeg.ToTextReference(), newTab);
					return true;
				}

				var localTarget = FindLocalTarget(refSeg);
				if (localTarget != null)
					refSeg = localTarget;

				if (refSeg.IsLocalTarget) {
					if (canRecordHistory) {
						if (!canJumpToReference)
							return false;
						textEditorHelper.FollowReference(refSeg.ToTextReference(), newTab);
					}
					else {
						var line = wpfTextView.TextSnapshot.GetLineFromPosition(refSeg.StartOffset);
						int column = refSeg.StartOffset - line.Start.Position;
						ScrollAndMoveCaretTo(line.LineNumber, column);
					}
					return true;
				}

				if (refSeg.IsLocal)
					return false;
				if (!canJumpToReference)
					return false;
				textEditorHelper.FollowReference(refSeg.ToTextReference(), newTab);
				return true;
			}
			else {
				var localTarget = FindLocalTarget(refSeg);
				if (localTarget != null)
					refSeg = localTarget;

				int pos = -1;
				if (!refSeg.IsLocal) {
					if (refSeg.IsLocalTarget)
						pos = refSeg.EndOffset;
					if (pos < 0 && definitionLookup != null)
						pos = definitionLookup.GetDefinitionPosition(refSeg.Reference);
				}
				if (pos >= 0) {
					if (canRecordHistory) {
						if (!canJumpToReference)
							return false;
						textEditorHelper.FollowReference(refSeg.ToTextReference(), newTab);
					}
					else {
						MarkReferences(refSeg);
						textEditorHelper.SetFocus();
						wpfTextView.Selection.Clear();
						wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, pos));
						TextEditor.ScrollTo(TextEditor.TextArea.Caret.Line, TextEditor.TextArea.Caret.Column);
					}
					return true;
				}

				if (refSeg.IsLocal && MarkReferences(refSeg))
					return false;	// Allow another handler to set a new caret position

				textEditorHelper.SetFocus();
				if (!canJumpToReference)
					return false;
				textEditorHelper.FollowReference(refSeg.ToTextReference(), newTab);
				return true;
			}
		}

		internal void MoveReference(bool forward) {
			var refSeg = GetCurrentReferenceSegment();
			if (refSeg == null)
				return;

			foreach (var newSeg in GetReferenceSegmentsFrom(refSeg, forward)) {
				if (RefSegEquals(newSeg, refSeg)) {
					var line = wpfTextView.TextSnapshot.GetLineFromPosition(newSeg.StartOffset);
					int column = newSeg.StartOffset - line.Start.Position;
					ScrollAndMoveCaretTo(line.LineNumber, column);
					break;
				}
			}
		}

		internal void MoveToNextDefinition(bool forward) {
			int offset = wpfTextView.Caret.Position.BufferPosition.Position;
			var refSeg = references.FindFirstSegmentWithStartAfter(offset) ?? (forward ? references.LastSegment : references.FirstSegment);
			if (refSeg == null)
				return;

			foreach (var newSeg in GetReferenceSegmentsFrom(refSeg, forward)) {
				if (newSeg.IsLocalTarget && newSeg.Reference is IMemberDef) {
					var line = wpfTextView.TextSnapshot.GetLineFromPosition(newSeg.StartOffset);
					int column = newSeg.StartOffset - line.Start.Position;
					ScrollAndMoveCaretTo(line.LineNumber, column);
					break;
				}
			}
		}

		IEnumerable<ReferenceSegment> GetReferenceSegmentsFrom(ReferenceSegment refSeg, bool forward) {
			if (refSeg == null)
				yield break;

			var currSeg = refSeg;
			while (true) {
				currSeg = forward ? references.GetNextSegment(currSeg) : references.GetPreviousSegment(currSeg);
				if (currSeg == null)
					currSeg = forward ? references.FirstSegment : references.LastSegment;
				if (currSeg == refSeg)
					break;

				yield return currSeg;
			}
		}

		bool GoToTarget(ReferenceSegment refSeg, bool canJumpToReference, bool canRecordHistory) {
			if (textEditorHelper == null)
				return false;
			return GoTo(refSeg, false, true, canRecordHistory, canJumpToReference);
		}

		bool IsOwnerOf(ReferenceSegment refSeg) {
			foreach (var r in references) {
				if (r == refSeg)
					return true;
			}
			return false;
		}

		ReferenceSegment FindReferenceSegment(ReferenceSegment refSeg) {
			if (refSeg == null)
				return null;
			foreach (var r in references) {
				if (r.IsLocal == refSeg.IsLocal && r.IsLocalTarget == refSeg.IsLocalTarget && RefSegEquals(r, refSeg))
					return r;
			}
			return null;
		}

		ReferenceSegment FindLocalTarget(ReferenceSegment refSeg) {
			if (refSeg.IsLocalTarget)
				return refSeg;
			foreach (var r in references) {
				if (r.IsLocalTarget && RefSegEquals(r, refSeg))
					return r;
			}
			return null;
		}

		static bool RefSegEquals(ReferenceSegment a, ReferenceSegment b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Reference == null || b.Reference == null)
				return false;
			if (a.Reference.Equals(b.Reference))
				return true;

			var ma = a.Reference as IMemberRef;
			var mb = b.Reference as IMemberRef;
			if (ma != null && mb != null) {
				// PERF: Prevent expensive resolves by doing a quick name check
				if (ma.Name != mb.Name)
					return false;

				ma = Resolve(ma) ?? ma;
				mb = Resolve(mb) ?? mb;
				return new SigComparer(SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.PrivateScopeIsComparable).Equals(ma, mb);
			}

			return false;
		}

		static IMemberRef Resolve(IMemberRef mr) {
			if (mr is ITypeDefOrRef)
				return ((ITypeDefOrRef)mr).ResolveTypeDef();
			if (mr is IMethod && ((IMethod)mr).IsMethod)
				return ((IMethod)mr).ResolveMethodDef();
			if (mr is IField)
				return ((IField)mr).ResolveFieldDef();
			Debug.Assert(mr is PropertyDef || mr is EventDef || mr is GenericParam, "Unknown IMemberRef");
			return null;
		}

		public void ScrollAndMoveCaretTo(int line, int column, bool focus = true) {
			// Make sure the lines have been re-initialized or the ScrollTo() method could fail
			TextEditor.TextArea.TextView.EnsureVisualLines();
			TextEditor.ScrollTo(line + 1, column + 1);
			wpfTextView.MoveCaretTo(line, column);
			if (focus)
				textEditorHelper.SetFocus();
		}

		void JumpToReference(ReferenceSegment referenceSegment, MouseEventArgs e) {
			if (textEditorHelper == null)
				return;
			bool newTab = Keyboard.Modifiers == ModifierKeys.Control;
			textEditorHelper.SetActive();
			textEditorHelper.SetFocus();
			//TextEditor.GoToMousePosition();
			e.Handled = GoTo(referenceSegment, newTab, false, true, true);
		}

		bool MarkReferences(ReferenceSegment referenceSegment) {
			if (previousReferenceSegment == referenceSegment)
				return true;
			object reference = referenceSegment.Reference;
			if (reference == null)
				return false;
			ClearMarkedReferences();
			previousReferenceSegment = referenceSegment;
			//foreach (var tmp in references) {
			//	var r = tmp;
			//	if (RefSegEquals(referenceSegment, r)) {
			//		var mark = textMarkerService.Create(r.StartOffset, r.Length);
			//		mark.ZOrder = TextEditorConstants.ZORDER_SEARCHRESULT;
			//		mark.HighlightingColor = () => {
			//			return r.IsLocalTarget ?
			//				themeManager.Theme.GetTextColor(ColorType.LocalDefinition).ToHighlightingColor() :
			//				themeManager.Theme.GetTextColor(ColorType.LocalReference).ToHighlightingColor();
			//		};
			//		markedReferences.Add(mark);
			//	}
			//}
			return true;
		}
		ReferenceSegment previousReferenceSegment = null;

		void ClearMarkedReferences() {
			//if (textMarkerService == null) return;
			//foreach (var mark in markedReferences) {
			//	textMarkerService.Remove(mark);
			//}
			markedReferences.Clear();
			previousReferenceSegment = null;
		}

		public bool GoToLocation(object @ref) {
			if (@ref == null)
				return false;

			var member = @ref as IMemberDef;
			if (member != null) {
				var refSeg = references.FirstOrDefault(a => a.IsLocalTarget && a.Reference == member);
				return GoToTarget(refSeg, false, false);
			}

			var pd = @ref as ParamDef;
			if (pd != null) {
				var refSeg = references.FirstOrDefault(a => a.IsLocalTarget && a.Reference is Parameter && ((Parameter)a.Reference).ParamDef == pd);
				return GoToTarget(refSeg, false, false);
			}

			var textRef = @ref as TextReference;
			if (textRef != null) {
				var refSeg = references.FirstOrDefault(a => a.Equals(textRef));
				return GoToTarget(refSeg, false, false);
			}

			Debug.Fail(string.Format("Unknown type: {0} = {1}", @ref.GetType(), @ref));
			return false;
		}

		void TextEditorSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(textEditorSettings.AutoHighlightRefs))
				OnAutoHighlightRefsChanged();
		}

		void OnAutoHighlightRefsChanged() {
			if (!textEditorSettings.AutoHighlightRefs)
				ClearMarkedReferences();
			else {
				int offset = wpfTextView.Caret.Position.BufferPosition.Position;
				var refSeg = GetReferenceSegmentAt(offset);
				if (refSeg != null)
					MarkReferences(refSeg);
				else
					ClearMarkedReferences();
			}
		}

		public void Dispose() {
			this.textEditorSettings.PropertyChanged -= TextEditorSettings_PropertyChanged;
			this.themeManager.ThemeChanged -= ThemeManager_ThemeChanged;
			Clear();
			BindingOperations.ClearAllBindings(TextEditor);
			//textMarkerService.Dispose();
			if (!wpfTextViewHost.IsClosed)
				wpfTextViewHost.Close();
		}

		public object SaveReferencePosition(ICodeMappings cms) => GetRefPos(cms);

		public bool RestoreReferencePosition(ICodeMappings cms, object obj) {
			var refPos = obj as RefPos;
			if (refPos == null)
				return false;
			return GoTo(cms, refPos);
		}

		sealed class RefPos {
			public SourceCodeMapping SourceCodeMapping;
			public ReferenceSegment ReferenceSegment;

			public RefPos(IList<SourceCodeMapping> sourceCodeMappings) {
				this.SourceCodeMapping = sourceCodeMappings.Count > 0 ? sourceCodeMappings[0] : null;
			}

			public RefPos(ReferenceSegment refSeg) {
				this.ReferenceSegment = refSeg;
			}
		}

		int LineColumnToOffset(TextLocation location) => LineColumnToOffset(location.Line - 1, location.Column - 1);
		int LineColumnToOffset(TextPosition pos) => LineColumnToOffset(pos.Line, pos.Column);
		int LineColumnToOffset(int line, int column) {
			var snapshotLine = wpfTextView.TextSnapshot.GetLineFromLineNumber(line);
			return snapshotLine.Start.Position + column;
		}

		RefPos GetRefPos(ICodeMappings cms) {
			int caretPos = wpfTextView.Caret.Position.BufferPosition.Position;
			var line = wpfTextView.TextSnapshot.GetLineFromPosition(caretPos);
			var mappings = cms.Find(line.LineNumber, caretPos - line.Start.Position).ToList();
			mappings.Sort(Sort);
			var mapping = mappings.Count == 0 ? null : mappings[0];

			int offset = line.Start.Position;
			var refSeg = references.FindFirstSegmentWithStartAfter(offset);
			while (refSeg != null) {
				if (refSeg.Reference is IMemberDef && refSeg.IsLocalTarget && !refSeg.IsLocal)
					break;
				refSeg = references.GetNextSegment(refSeg);
			}
			if (mapping == null) {
				if (refSeg != null)
					return new RefPos(refSeg);
			}
			else if (refSeg == null)
				return new RefPos(mappings);
			else {
				offset = LineColumnToOffset(mapping.StartPosition);
				if (offset < refSeg.StartOffset)
					return new RefPos(mappings);
				return new RefPos(refSeg);
			}

			return null;
		}

		static int Sort(SourceCodeMapping a, SourceCodeMapping b) => a.StartPosition.CompareTo(b.StartPosition);

		bool GoTo(ICodeMappings cms, RefPos pos) {
			if (pos == null)
				return false;

			if (pos.SourceCodeMapping != null) {
				var mapping = pos.SourceCodeMapping;
				var scm = cms.Find(mapping.Mapping.Method, mapping.ILRange.From);
				if (scm != null) {
					ScrollAndMoveCaretTo(scm.StartPosition.Line, scm.StartPosition.Column);
					return true;
				}
			}

			if (pos.ReferenceSegment != null) {
				var refSeg = FindReferenceSegment(pos.ReferenceSegment);
				if (refSeg != null)
					return GoToTarget(refSeg, false, false);
			}

			return false;
		}
	}
}
