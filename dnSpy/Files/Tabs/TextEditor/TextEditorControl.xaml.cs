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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;
using dnSpy.Decompiler.Shared;
using dnSpy.Files.Tabs.TextEditor.ToolTips;
using dnSpy.Shared.AvalonEdit;
using dnSpy.Shared.Decompiler;
using dnSpy.Shared.Highlighting;
using dnSpy.Shared.MVVM;
using dnSpy.TextEditor;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.Files.Tabs.TextEditor {
	sealed partial class TextEditorControl : UserControl, IDisposable {
		readonly ITextEditorHelper textEditorHelper;

		public NewTextEditor TextEditor {
			get { return textEditor; }
		}
		readonly NewTextEditor textEditor;

		readonly IThemeManager themeManager;

		readonly IconBarMargin iconBarMargin;

		public IEnumerable<object> AllReferences {
			get { return references.Select(a => a.Reference); }
		}

		DefinitionLookup definitionLookup;
		TextSegmentCollection<ReferenceSegment> references;
		readonly TextMarkerService textMarkerService;
		readonly List<ITextMarker> markedReferences = new List<ITextMarker>();

		readonly ReferenceElementGenerator referenceElementGenerator;
		readonly UIElementGenerator uiElementGenerator;
		List<VisualLineElementGenerator> activeCustomElementGenerators = new List<VisualLineElementGenerator>();

		readonly ToolTipHelper toolTipHelper;
		readonly ITextEditorSettings textEditorSettings;

		public TextEditorControl(IThemeManager themeManager, ToolTipHelper toolTipHelper, ITextEditorSettings textEditorSettings, ITextEditorUIContextImpl uiContext, ITextEditorHelper textEditorHelper, ITextLineObjectManager textLineObjectManager, IImageManager imageManager, IIconBarCommandManager iconBarCommandManager) {
			this.references = new TextSegmentCollection<ReferenceSegment>();
			this.themeManager = themeManager;
			this.toolTipHelper = toolTipHelper;
			this.textEditorSettings = textEditorSettings;
			this.textEditorHelper = textEditorHelper;
			InitializeComponent();
			this.textEditorSettings.PropertyChanged += TextEditorSettings_PropertyChanged;

			themeManager.ThemeChanged += ThemeManager_ThemeChanged;

			textEditor = new NewTextEditor(themeManager, textEditorSettings);
			this.toolTipHelper.Initialize(TextEditor);
			RemoveCommands(TextEditor);
			newTextEditor.Content = TextEditor;
			TextEditor.IsReadOnly = true;
			TextEditor.ShowLineNumbers = true;

			referenceElementGenerator = new ReferenceElementGenerator(JumpToReference, a => true);
			// Add the ref elem generator first in case one of the refs looks like a http link etc
			TextEditor.TextArea.TextView.ElementGenerators.Insert(0, referenceElementGenerator);
			this.uiElementGenerator = new UIElementGenerator();
			textEditor.TextArea.TextView.ElementGenerators.Add(uiElementGenerator);

			iconBarMargin = new IconBarMargin(uiContext, textLineObjectManager, imageManager, themeManager);
			iconBarCommandManager.Initialize(iconBarMargin);
			TextEditor.TextArea.LeftMargins.Insert(0, iconBarMargin);
			TextEditor.TextArea.TextView.VisualLinesChanged += (s, e) => iconBarMargin.InvalidateVisual();

			textMarkerService = new TextMarkerService(this, uiContext, textLineObjectManager);
			TextEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
			TextEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);

			TextEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;

			InputBindings.Add(new KeyBinding(new RelayCommand(a => MoveReference(true)), Key.Tab, ModifierKeys.None));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => MoveReference(false)), Key.Tab, ModifierKeys.Shift));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => MoveReference(true)), Key.Down, ModifierKeys.Control | ModifierKeys.Shift));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => MoveReference(false)), Key.Up, ModifierKeys.Control | ModifierKeys.Shift));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => MoveToNextDefinition(true)), Key.Down, ModifierKeys.Alt));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => MoveToNextDefinition(false)), Key.Up, ModifierKeys.Alt));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => FollowReference()), Key.F12, ModifierKeys.None));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => FollowReference()), Key.Enter, ModifierKeys.None));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => FollowReferenceNewTab()), Key.F12, ModifierKeys.Control));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => FollowReferenceNewTab()), Key.Enter, ModifierKeys.Control));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => ClearMarkedReferencesAndToolTip()), Key.Escape, ModifierKeys.None));

			TextEditor.OnShowLineNumbersChanged();
			OnAutoHighlightRefsChanged();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e) {
			// Check for this combo here because of the scrollviewer
			if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) {
				if (e.Key == Key.Up || e.Key == Key.Down) {
					MoveReference(e.Key == Key.Down);
					e.Handled = true;
					return;
				}
			}
			base.OnPreviewKeyDown(e);
		}

		public Button CancelButton {
			get {
				var wa = this.waitAdorner.Content as WaitAdorner;
				return wa == null ? null : wa.button;
			}
		}

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
			if (wa != null && wa.IsKeyboardFocusWithin)
				this.textEditorHelper.SetFocus();
		}

		// Remove commands the text editor added so we can use them for our own purposes
		static void RemoveCommands(NewTextEditor textEditor) {
			var handler = textEditor.TextArea.DefaultInputHandler;

			RemoveCommands(handler.Editing);
			RemoveCommands(handler.CaretNavigation);
			RemoveCommands(handler.CommandBindings);
		}

		static void RemoveCommands(TextAreaInputHandler handler) {
			var commands = new HashSet<ICommand>();
			var inputList = (IList<InputBinding>)handler.InputBindings;
			for (int i = inputList.Count - 1; i >= 0; i--) {
				var kb = inputList[i] as KeyBinding;
				if (kb == null)
					continue;
				if ((kb.Modifiers == ModifierKeys.None && kb.Key == Key.Back) ||
					(kb.Modifiers == ModifierKeys.Shift && kb.Key == Key.Back) ||
					(kb.Modifiers == ModifierKeys.None && kb.Key == Key.Enter) ||
					(kb.Modifiers == ModifierKeys.None && kb.Key == Key.Tab) ||
					(kb.Modifiers == ModifierKeys.Shift && kb.Key == Key.Tab) ||
					(kb.Modifiers == ModifierKeys.Control && kb.Key == Key.Enter) ||
					(kb.Modifiers == ModifierKeys.None && kb.Key == Key.Delete)) {
					inputList.RemoveAt(i);
					commands.Add(kb.Command);
				}
			}
			RemoveCommands(handler.CommandBindings);
			var bindingList = (IList<CommandBinding>)handler.CommandBindings;
			for (int i = bindingList.Count - 1; i >= 0; i--) {
				var binding = bindingList[i];
				if (commands.Contains(binding.Command))
					bindingList.RemoveAt(i);
			}
		}

		static void RemoveCommands(ICollection<CommandBinding> commandBindings) {
			var bindingList = (IList<CommandBinding>)commandBindings;
			for (int i = bindingList.Count - 1; i >= 0; i--) {
				var binding = bindingList[i];
				if (binding.Command == AvalonEditCommands.DeleteLine ||
					binding.Command == ApplicationCommands.Undo ||
					binding.Command == ApplicationCommands.Redo ||
					binding.Command == ApplicationCommands.Cut ||
					binding.Command == ApplicationCommands.Delete ||
					binding.Command == EditingCommands.Delete ||
					binding.Command == EditingCommands.Backspace ||
					binding.Command == NavigationCommands.BrowseBack)
					bindingList.RemoveAt(i);
			}
		}

		void ClearCustomElementGenerators() {
			foreach (var elementGenerator in activeCustomElementGenerators)
				textEditor.TextArea.TextView.ElementGenerators.Remove(elementGenerator);
			activeCustomElementGenerators.Clear();
		}

		struct LastOutput : IEquatable<LastOutput> {
			readonly ITextOutput output;
			readonly IHighlightingDefinition highlighting;

			public LastOutput(ITextOutput output, IHighlightingDefinition highlighting) {
				this.output = output;
				this.highlighting = highlighting;
			}

			public bool Equals(LastOutput other) {
				return output == other.output &&
					highlighting == other.highlighting;
			}

			public override bool Equals(object obj) {
				if (obj is LastOutput)
					return Equals((LastOutput)obj);
				return false;
			}

			public override int GetHashCode() {
				return (output == null ? 0 : output.GetHashCode()) ^
					(highlighting == null ? 0 : highlighting.GetHashCode());
			}
		}

		public void OnUseNewRendererChanged() {
			lastOutput = new LastOutput();
		}

		LastOutput lastOutput;
		public void SetOutput(ITextOutput output, IHighlightingDefinition highlighting) {
			if (output == null)
				throw new ArgumentNullException();

			HideCancelButton();

			var newLastOutput = new LastOutput(output, highlighting);
			if (lastOutput.Equals(newLastOutput))
				return;
			lastOutput = newLastOutput;

			var avOutput = output as AvalonEditTextOutput;
			Debug.Assert(avOutput != null, "output should be an AvalonEditTextOutput instance");

			TextEditor.LanguageTokens = avOutput == null ? new LanguageTokens() : avOutput.LanguageTokens;
			TextEditor.LanguageTokens.Finish();

			ClearMarkedReferences();
			TextEditor.ScrollToHome();
			TextEditor.Document = null;
			TextEditor.SyntaxHighlighting = highlighting;
			ClearCustomElementGenerators();

			if (avOutput == null) {
				uiElementGenerator.UIElements = null;
				referenceElementGenerator.References = null;
				references = new TextSegmentCollection<ReferenceSegment>();
				definitionLookup = null;
				TextEditor.Document = new TextDocument(output.ToString());
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

				TextEditor.Document = avOutput.GetDocument();
			}
		}

		public void Clear() {
			ClearMarkedReferences();
			ClearCustomElementGenerators();
			TextEditor.Document = new TextDocument();
			definitionLookup = null;
			uiElementGenerator.UIElements = null;
			referenceElementGenerator.References = null;
			references = new TextSegmentCollection<ReferenceSegment>();
			lastOutput = new LastOutput();
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			iconBarMargin.InvalidateVisual();
		}

		void Caret_PositionChanged(object sender, EventArgs e) {
			toolTipHelper.Close();

			OnAutoHighlightRefsChanged();
		}

		void ClearMarkedReferencesAndToolTip() {
			ClearMarkedReferences();
			toolTipHelper.Close();
		}

		public object GetReferenceSegmentAt(MouseEventArgs e) {
			var te = TextEditor;
			var tv = te.TextArea.TextView;
			var pos = tv.GetPosition(e.GetPosition(tv) + tv.ScrollOffset);
			if (pos == null)
				return null;
			int offset = te.Document.GetOffset(pos.Value.Location);
			var seg = GetReferenceSegmentAt(offset);
			return seg == null ? null : seg.Reference;
		}

		public ReferenceSegment GetReferenceSegmentAt(TextViewPosition? position) {
			if (position == null)
				return null;
			int offset = textEditor.Document.GetOffset(position.Value.Location);
			return GetReferenceSegmentAt(offset);
		}

		public ReferenceSegment GetCurrentReferenceSegment() {
			return GetReferenceSegmentAt(TextEditor.TextArea.Caret.Offset);
		}

		public IEnumerable<CodeReference> GetSelectedCodeReferences() {
			if (TextEditor.SelectionLength <= 0)
				yield break;
			int start = TextEditor.SelectionStart;
			int end = start + TextEditor.SelectionLength;

			var refs = references;
			if (refs == null)
				yield break;
			var r = refs.FindFirstSegmentWithStartAfter(start);
			while (r != null) {
				if (r.StartOffset >= end)
					break;
				yield return r.ToCodeReference();

				r = refs.GetNextSegment(r);
			}
		}

		public IEnumerable<Tuple<CodeReference, TextEditorLocation>> GetCodeReferences(int line, int column) {
			int offset = TextEditor.Document.GetOffset(line, column);
			var refSeg = references.FindFirstSegmentWithStartAfter(offset);

			while (refSeg != null) {
				yield return Tuple.Create(refSeg.ToCodeReference(), GetLocation(refSeg));
				refSeg = references.GetNextSegment(refSeg);
			}
		}

		TextEditorLocation GetLocation(ReferenceSegment refSeg) {
			var loc = TextEditor.Document.GetLocation(refSeg.StartOffset);
			return new TextEditorLocation(loc.Line, loc.Column);
		}

		public ReferenceSegment GetReferenceSegmentAt(int offset) {
			var segs = references.FindSegmentsContaining(offset).ToArray();
			foreach (var seg in segs) {
				if (seg.StartOffset <= offset && offset < seg.EndOffset)
					return seg;
			}
			return segs.Length == 0 ? null : segs[0];
		}

		void FollowReference() {
			GoToTarget(GetCurrentReferenceSegment(), true, true);
		}

		void FollowReferenceNewTab() {
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
				textEditorHelper.FollowReference(refSeg.ToCodeReference(), newTab);
				return true;
			}

			if (followLocalRefs) {
				if (!IsOwnerOf(refSeg)) {
					if (!canJumpToReference)
						return false;
					textEditorHelper.FollowReference(refSeg.ToCodeReference(), newTab);
					return true;
				}

				var localTarget = FindLocalTarget(refSeg);
				if (localTarget != null)
					refSeg = localTarget;

				if (refSeg.IsLocalTarget) {
					if (canRecordHistory) {
						if (!canJumpToReference)
							return false;
						textEditorHelper.FollowReference(refSeg.ToCodeReference(), newTab);
					}
					else {
						var line = TextEditor.Document.GetLineByOffset(refSeg.StartOffset);
						int column = refSeg.StartOffset - line.Offset + 1;
						ScrollAndMoveCaretTo(line.LineNumber, column);
					}
					return true;
				}

				if (refSeg.IsLocal)
					return false;
				if (!canJumpToReference)
					return false;
				textEditorHelper.FollowReference(refSeg.ToCodeReference(), newTab);
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
						textEditorHelper.FollowReference(refSeg.ToCodeReference(), newTab);
					}
					else {
						MarkReferences(refSeg);
						textEditorHelper.SetFocus();
						TextEditor.Select(pos, 0);
						TextEditor.ScrollTo(TextEditor.TextArea.Caret.Line, TextEditor.TextArea.Caret.Column);
					}
					return true;
				}

				if (refSeg.IsLocal && MarkReferences(refSeg))
					return false;	// Allow another handler to set a new caret position

				textEditorHelper.SetFocus();
				if (!canJumpToReference)
					return false;
				textEditorHelper.FollowReference(refSeg.ToCodeReference(), newTab);
				return true;
			}
		}

		void MoveReference(bool forward) {
			var refSeg = GetCurrentReferenceSegment();
			if (refSeg == null)
				return;

			foreach (var newSeg in GetReferenceSegmentsFrom(refSeg, forward)) {
				if (RefSegEquals(newSeg, refSeg)) {
					var line = TextEditor.Document.GetLineByOffset(newSeg.StartOffset);
					int column = newSeg.StartOffset - line.Offset + 1;
					ScrollAndMoveCaretTo(line.LineNumber, column);
					break;
				}
			}
		}

		void MoveToNextDefinition(bool forward) {
			int offset = TextEditor.CaretOffset;
			var refSeg = references.FindFirstSegmentWithStartAfter(offset) ?? (forward ? references.LastSegment : references.FirstSegment);
			if (refSeg == null)
				return;

			foreach (var newSeg in GetReferenceSegmentsFrom(refSeg, forward)) {
				if (newSeg.IsLocalTarget && newSeg.Reference is IMemberDef) {
					var line = TextEditor.Document.GetLineByOffset(newSeg.StartOffset);
					int column = newSeg.StartOffset - line.Offset + 1;
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
			TextEditor.ScrollTo(line, column);
			TextEditor.SetCaretPosition(line, column);
			if (focus)
				textEditorHelper.SetFocus();
		}

		void JumpToReference(ReferenceSegment referenceSegment, MouseEventArgs e) {
			if (textEditorHelper == null)
				return;
			bool newTab = Keyboard.Modifiers == ModifierKeys.Control;
			textEditorHelper.SetActive();
			textEditorHelper.SetFocus();
			TextEditor.GoToMousePosition();
			e.Handled = GoTo(referenceSegment, newTab, false, true, true);
		}

		bool MarkReferences(ReferenceSegment referenceSegment) {
			if (TextEditor.TextArea.TextView.Document == null)
				return false;
			if (previousReferenceSegment == referenceSegment)
				return true;
			object reference = referenceSegment.Reference;
			if (reference == null)
				return false;
			ClearMarkedReferences();
			previousReferenceSegment = referenceSegment;
			foreach (var tmp in references) {
				var r = tmp;
				if (RefSegEquals(referenceSegment, r)) {
					var mark = textMarkerService.Create(r.StartOffset, r.Length);
					mark.ZOrder = TextEditorConstants.ZORDER_SEARCHRESULT;
					mark.HighlightingColor = () => {
						return r.IsLocalTarget ?
							themeManager.Theme.GetTextColor(ColorType.LocalDefinition).ToHighlightingColor() :
							themeManager.Theme.GetTextColor(ColorType.LocalReference).ToHighlightingColor();
					};
					markedReferences.Add(mark);
				}
			}
			return true;
		}
		ReferenceSegment previousReferenceSegment = null;

		void ClearMarkedReferences() {
			foreach (var mark in markedReferences) {
				textMarkerService.Remove(mark);
			}
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

			var codeRef = @ref as CodeReference;
			if (codeRef != null) {
				var refSeg = references.FirstOrDefault(a => a.Equals(codeRef));
				return GoToTarget(refSeg, false, false);
			}

			Debug.Fail(string.Format("Unknown type: {0} = {1}", @ref.GetType(), @ref));
			return false;
		}

		void TextEditorSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			if (e.PropertyName == "AutoHighlightRefs")
				OnAutoHighlightRefsChanged();
		}

		void OnAutoHighlightRefsChanged() {
			if (!textEditorSettings.AutoHighlightRefs)
				ClearMarkedReferences();
			else {
				int offset = textEditor.TextArea.Caret.Offset;
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
			textMarkerService.Dispose();
			textEditor.Dispose();
		}

		public object SaveReferencePosition(ICodeMappings cms) {
			return GetRefPos(cms);
		}

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

		RefPos GetRefPos(ICodeMappings cms) {
			var mappings = cms.Find(textEditor.TextArea.Caret.Line, textEditor.TextArea.Caret.Column).ToList();
			mappings.Sort(Sort);
			var mapping = mappings.Count == 0 ? null : mappings[0];

			var doc = textEditor.TextArea.Document;
			int offset = doc == null ? 0 : doc.GetOffset(textEditor.TextArea.Caret.Line, 0);
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
				offset = doc == null ? 0 : doc.GetOffset(mapping.StartPosition.Line, mapping.StartPosition.Column);
				if (offset < refSeg.StartOffset)
					return new RefPos(mappings);
				return new RefPos(refSeg);
			}

			return null;
		}

		static int Sort(SourceCodeMapping a, SourceCodeMapping b) {
			return a.StartPosition.CompareTo(b.StartPosition);
		}

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
