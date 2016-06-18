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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Text.Formatting;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.Text.Editor {
	sealed class WpfTextView : Canvas, IWpfTextView {
		public PropertyCollection Properties { get; }
		public FrameworkElement VisualElement => DnSpyTextEditor.FocusedElement;
		public object UIObject => DnSpyTextEditor;
		public IInputElement FocusedElement => DnSpyTextEditor.FocusedElement;
		public FrameworkElement ScaleElement => DnSpyTextEditor.ScaleElement;
		public new object Tag { get; set; }
		public ITextViewRoleSet Roles { get; }
		public IEditorOptions Options { get; }
		public ICommandTargetCollection CommandTarget => RegisteredCommandElement.CommandTarget;
		IRegisteredCommandElement RegisteredCommandElement { get; }
		public ITextCaret Caret => TextCaret;
		TextCaret TextCaret { get; }
		public ITextSelection Selection { get; }
		public IEditorOperations2 EditorOperations { get; }
		public IViewScroller ViewScroller { get; }
		public bool HasAggregateFocus => DnSpyTextEditor.IsKeyboardFocusWithin;
		public bool IsMouseOverViewOrAdornments => DnSpyTextEditor.IsMouseOver;
		//TODO: Remove public from this property once all refs to it from REPL and LOG editors have been removed
		public DnSpyTextEditor DnSpyTextEditor { get; }
		public IFormattedLineSource FormattedLineSource { get; private set; }

		const int LEFT_MARGIN = 15;
		readonly FrameworkElement paddingElement;
		readonly IFormattedTextSourceFactoryService formattedTextSourceFactoryService;
		readonly IClassifier aggregateClassifier;
		readonly ITextAndAdornmentSequencer textAndAdornmentSequencer;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IAdornmentLayerDefinitionService adornmentLayerDefinitionService;
		readonly AdornmentLayerCollection adornmentLayerCollection;

#pragma warning disable CS0169
		[ExportAdornmentLayerDefinition("Text", PredefinedAdornmentLayers.Text, AdornmentLayerOrder.Text)]
		static readonly AdornmentLayerDefinition textAdornmentLayerDefinition;
		[ExportAdornmentLayerDefinition("Caret", PredefinedAdornmentLayers.Caret, AdornmentLayerOrder.Caret)]
		static readonly AdornmentLayerDefinition caretAdornmentLayerDefinition;
		[ExportAdornmentLayerDefinition("Selection", PredefinedAdornmentLayers.Selection, AdornmentLayerOrder.Selection)]
		static readonly AdornmentLayerDefinition selectionAdornmentLayerDefinition;
#pragma warning restore CS0169

		public WpfTextView(DnSpyTextEditor dnSpyTextEditor, ITextViewModel textViewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, IEditorOptionsFactoryService editorOptionsFactoryService, ICommandManager commandManager, IEditorOperationsFactoryService editorOperationsFactoryService, ISmartIndentationService smartIndentationService, IFormattedTextSourceFactoryService formattedTextSourceFactoryService, IViewClassifierAggregatorService viewClassifierAggregatorService, ITextAndAdornmentSequencerFactoryService textAndAdornmentSequencerFactoryService, IClassificationFormatMapService classificationFormatMapService, IAdornmentLayerDefinitionService adornmentLayerDefinitionService) {
			if (dnSpyTextEditor == null)
				throw new ArgumentNullException(nameof(dnSpyTextEditor));
			if (textViewModel == null)
				throw new ArgumentNullException(nameof(textViewModel));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			if (editorOptionsFactoryService == null)
				throw new ArgumentNullException(nameof(editorOptionsFactoryService));
			if (commandManager == null)
				throw new ArgumentNullException(nameof(commandManager));
			if (editorOperationsFactoryService == null)
				throw new ArgumentNullException(nameof(editorOperationsFactoryService));
			if (smartIndentationService == null)
				throw new ArgumentNullException(nameof(smartIndentationService));
			if (formattedTextSourceFactoryService == null)
				throw new ArgumentNullException(nameof(formattedTextSourceFactoryService));
			if (viewClassifierAggregatorService == null)
				throw new ArgumentNullException(nameof(viewClassifierAggregatorService));
			if (textAndAdornmentSequencerFactoryService == null)
				throw new ArgumentNullException(nameof(textAndAdornmentSequencerFactoryService));
			if (classificationFormatMapService == null)
				throw new ArgumentNullException(nameof(classificationFormatMapService));
			if (adornmentLayerDefinitionService == null)
				throw new ArgumentNullException(nameof(adornmentLayerDefinitionService));
			this.formattedTextSourceFactoryService = formattedTextSourceFactoryService;
			this.paddingElement = new FrameworkElement { Margin = new Thickness(LEFT_MARGIN, 0, 0, 0) };
			this.zoomLevel = ZoomConstants.DefaultZoom;
			this.adornmentLayerDefinitionService = adornmentLayerDefinitionService;
			this.adornmentLayerCollection = new AdornmentLayerCollection(this);
			Properties = new PropertyCollection();
			DnSpyTextEditor = dnSpyTextEditor;
			TextViewLines = new WpfTextViewLineCollection();
			DnSpyTextEditor.Options.AllowToggleOverstrikeMode = true;
			TextViewModel = textViewModel;
			Roles = roles;
			Options = editorOptionsFactoryService.GetOptions(this);
			Options.Parent = parentOptions;
			Options.OptionChanged += EditorOptions_OptionChanged;
			Selection = new TextSelection(this, dnSpyTextEditor);
			EditorOperations = editorOperationsFactoryService.GetEditorOperations(this);
			TextCaret = new TextCaret(this, dnSpyTextEditor, smartIndentationService);
			ViewScroller = new ViewScroller(this);
			InitializeOptions();
			DnSpyTextEditor.Loaded += DnSpyTextEditor_Loaded;
			DnSpyTextEditor.IsKeyboardFocusWithinChanged += DnSpyTextEditor_IsKeyboardFocusWithinChanged;
			hasKeyboardFocus = DnSpyTextEditor.IsKeyboardFocusWithin;
			DnSpyTextEditor.TextArea.TextView.SizeChanged += AvalonEdit_TextView_SizeChanged;
			DnSpyTextEditor.TextArea.TextView.ScrollOffsetChanged += AvalonEdit_TextView_ScrollOffsetChanged;
			DnSpyTextEditor.TextArea.TextView.VisualLinesCreated += AvalonEdit_TextView_VisualLinesCreated;
			DnSpyTextEditor.TextArea.TextView.VisualLineConstructionStarting += AvalonEdit_TextView_VisualLineConstructionStarting;
			DnSpyTextEditor.TextArea.TextView.DefaultTextMetricsInvalidated += (s, e) => CreateFormattedLineSource();
			oldViewportLeft = ViewportLeft;
			oldViewState = new ViewState(this);
			this.aggregateClassifier = viewClassifierAggregatorService.GetClassifier(this);
			this.textAndAdornmentSequencer = textAndAdornmentSequencerFactoryService.Create(this);
			this.classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(this);
			if (Roles.Contains(PredefinedTextViewRoles.Interactive))
				RegisteredCommandElement = commandManager.Register(VisualElement, this);
			else
				RegisteredCommandElement = NullRegisteredCommandElement.Instance;

			Children.Add(adornmentLayerCollection);
			this.Cursor = Cursors.IBeam;
			this.Focusable = true;
			this.FocusVisualStyle = null;
		}

		//TODO: Call this each time one of the values it uses gets updated
		void CreateFormattedLineSource() {
			var wordWrapStyle = Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId);
			bool isWordWrap = (wordWrapStyle & WordWrapStyles.WordWrap) != 0;
			bool isAutoIndent = isWordWrap && (wordWrapStyle & WordWrapStyles.AutoIndent) != 0;
			double wordWrapWidth = isWordWrap ? ViewportWidth : 0;
			var maxAutoIndent = isAutoIndent ? ViewportWidth / 4 : 0;
			bool useDisplayMode = false;//TODO:

			// This value is what VS uses, see: https://msdn.microsoft.com/en-us/library/microsoft.visualstudio.text.formatting.iformattedlinesource.baseindentation.aspx
			//	"This is generally a small value like 2.0, so that some characters (such as an italic
			//	 slash) will not be clipped by the left edge of the view."
			const double baseIndent = 2.0;
			FormattedLineSource = formattedTextSourceFactoryService.Create(
				TextSnapshot,
				VisualSnapshot,
				Options.GetOptionValue(DefaultOptions.TabSizeOptionId),
				baseIndent,
				wordWrapWidth,
				maxAutoIndent,
				useDisplayMode,
				aggregateClassifier,
				textAndAdornmentSequencer,
				classificationFormatMap,
				isWordWrap);
		}

		bool hasKeyboardFocus;
		void DnSpyTextEditor_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateKeyboardFocus();
		public event EventHandler GotAggregateFocus;
		public event EventHandler LostAggregateFocus;
		void UpdateKeyboardFocus() {
			bool newValue = DnSpyTextEditor.IsKeyboardFocusWithin;
			if (hasKeyboardFocus != newValue) {
				hasKeyboardFocus = newValue;
				if (hasKeyboardFocus)
					GotAggregateFocus?.Invoke(this, EventArgs.Empty);
				else
					LostAggregateFocus?.Invoke(this, EventArgs.Empty);
			}
		}

		void DnSpyTextEditor_Loaded(object sender, RoutedEventArgs e) {
			DnSpyTextEditor.Loaded -= DnSpyTextEditor_Loaded;
			// Fix the highlighted line, it won't be shown correctly if this control is in
			// a hidden tab that now got activated for the first time.
			DnSpyTextEditor.UpdateCurrentLineColors();
		}

		void EditorOptions_OptionChanged(object sender, EditorOptionChangedEventArgs e) => UpdateOption(e.OptionId);

		public ITextBuffer TextBuffer => TextViewModel.EditBuffer;
		public ITextSnapshot TextSnapshot => TextBuffer.CurrentSnapshot;
		public ITextSnapshot VisualSnapshot => TextViewModel.VisualBuffer.CurrentSnapshot;
		public ITextDataModel TextDataModel => TextViewModel.DataModel;
		public ITextViewModel TextViewModel { get; }

		public bool IsClosed { get; set; }

		public new Brush Background {
			get { return DnSpyTextEditor.Background; }
			set {
				if (DnSpyTextEditor.Background != value) {
					DnSpyTextEditor.Background = value;
					BackgroundBrushChanged?.Invoke(this, new BackgroundBrushChangedEventArgs(value));
				}
			}
		}

		public double ZoomLevel {
			get { return zoomLevel; }
			set {
				//TODO:
			}
		}
		double zoomLevel;

		public bool InLayout { get; private set; }
		ITextViewLineCollection ITextView.TextViewLines => TextViewLines;
		IWpfTextViewLineCollection IWpfTextView.TextViewLines => TextViewLines;
		public WpfTextViewLineCollection TextViewLines {
			get {
				Debug.Assert(wpfTextViewLineCollection != null);
				return wpfTextViewLineCollection;
			}
			private set {
				wpfTextViewLineCollection = value;
			}
		}
		WpfTextViewLineCollection wpfTextViewLineCollection;

		public double LineHeight => DnSpyTextEditor.TextArea.TextView.DefaultLineHeight;
		public double ViewportTop => DnSpyTextEditor.TextArea.TextView.VerticalOffset;
		public double ViewportBottom => ViewportTop + ViewportHeight;
		public double ViewportRight => ViewportLeft + ViewportWidth;
		public double ViewportWidth => ((IScrollInfo)DnSpyTextEditor.TextArea.TextView).ViewportWidth;
		public double ViewportHeight => ((IScrollInfo)DnSpyTextEditor.TextArea.TextView).ViewportHeight;
		public double ViewportLeft {
			get { return DnSpyTextEditor.TextArea.TextView.HorizontalOffset; }
			set {
				if (double.IsNaN(value))
					throw new ArgumentOutOfRangeException(nameof(value));
				double left = value;
				if ((Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) & WordWrapStyles.WordWrap) != 0)
					left = 0;
				((IScrollInfo)DnSpyTextEditor.TextArea.TextView).SetHorizontalOffset(left);
			}
		}

		public double MaxTextRightCoordinate {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		//TODO: Use this prop
		public ITrackingSpan ProvisionalTextHighlight { get; set; }

		public event EventHandler Closed;
		public event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged;
		public event EventHandler ViewportLeftChanged;
		public event EventHandler ViewportHeightChanged;
		public event EventHandler ViewportWidthChanged;
		public event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged;
#pragma warning disable CS0067
		public event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged;//TODO: Use this event
		public event EventHandler<MouseHoverEventArgs> MouseHover;//TODO: Use this event
#pragma warning restore CS0067

		void AvalonEdit_TextView_SizeChanged(object sender, SizeChangedEventArgs e) {
			if (IsClosed)
				return;
			if (e.PreviousSize.Height != e.NewSize.Height)
				ViewportHeightChanged?.Invoke(this, EventArgs.Empty);
			if (e.PreviousSize.Width != e.NewSize.Width)
				ViewportWidthChanged?.Invoke(this, EventArgs.Empty);
			UpdateVisibleArea();
		}

		void AvalonEdit_TextView_ScrollOffsetChanged(object sender, EventArgs e) {
			if (IsClosed)
				return;
			if (oldViewportLeft != ViewportLeft) {
				oldViewportLeft = ViewportLeft;
				RaiseLayoutChanged(ViewportWidth, ViewportHeight, Array.Empty<ITextViewLine>(), Array.Empty<ITextViewLine>());
				ViewportLeftChanged?.Invoke(this, EventArgs.Empty);
			}
			UpdateVisibleArea();
		}
		double oldViewportLeft;

		void AvalonEdit_TextView_VisualLineConstructionStarting(object sender, VisualLineConstructionStartEventArgs e) {
			Debug.Assert(!InLayout);
			InLayout = true;
		}

		void AvalonEdit_TextView_VisualLinesCreated(object sender, VisualLinesCreatedEventArgs e) {
			InLayout = false;

			ITextViewLine[] newOrReformattedLines = null;
			ITextViewLine[] translatedLines = null;
			var wpfLines = InitializeTextViewLines(e, out newOrReformattedLines, out translatedLines);
			TextViewLines.SetIsInvalid();
			TextViewLines = new WpfTextViewLineCollection(TextSnapshot, wpfLines);

			TextCaret.OnVisualLinesCreated();
			RaiseLayoutChanged(ViewportWidth, ViewportHeight, newOrReformattedLines, translatedLines);
		}

		void UpdateVisibleArea() {
			foreach (WpfTextViewLine line in TextViewLines)
				line.VisibleArea = new Rect(ViewportLeft, ViewportTop, ViewportWidth, ViewportHeight);
		}

		List<IWpfTextViewLine> InitializeTextViewLines(VisualLinesCreatedEventArgs e, out ITextViewLine[] newOrReformattedLines, out ITextViewLine[] translatedLines) {
			var collLines = new List<IWpfTextViewLine>();
			var reusedHash = new HashSet<VisualLine>(e.ReusedVisualLines);
			var newList = new List<ITextViewLine>();
			var translatedList = new List<ITextViewLine>();
			var reusedLinesDict = new Dictionary<TextLine, WpfTextViewLine>();

			foreach (WpfTextViewLine line in TextViewLines) {
				if (!reusedHash.Contains(line.VisualLine))
					line.SetIsInvalid();
				else
					reusedLinesDict.Add(line.TextLine, line);
			}

			Debug.Assert(DnSpyTextEditor.TextArea.TextView.VisualLinesValid);
			if (DnSpyTextEditor.TextArea.TextView.VisualLinesValid) {
				var visualLines = DnSpyTextEditor.TextArea.TextView.VisualLines;

				var snapshot = TextSnapshot;
				foreach (var line in visualLines) {
					var top = line.VisualTop - DnSpyTextEditor.TextArea.TextView.VerticalOffset + ViewportTop;

					foreach (var info in line.TextLineInfos) {
						WpfTextViewLine oldWpfLine;
						var change = TextViewLineChange.NewOrReformatted;
						double deltaY = 0;
						if (reusedLinesDict.TryGetValue(info.TextLine, out oldWpfLine)) {
							deltaY = top - oldWpfLine.GetTop();
							change = TextViewLineChange.Translated;
						}

						var visibleArea = new Rect(ViewportLeft, ViewportTop, ViewportWidth, ViewportHeight);
						double virtualSpaceWidth = DnSpyTextEditor.TextArea.TextView.WideSpaceWidth;
						var wpfLine = new WpfTextViewLine(this, snapshot, line, info, top, deltaY, change, visibleArea, virtualSpaceWidth);
						if (!reusedHash.Contains(line))
							newList.Add(wpfLine);
						else
							translatedList.Add(wpfLine);
						collLines.Add(wpfLine);

						top += wpfLine.Height;
					}
				}
			}

			newOrReformattedLines = newList.ToArray();
			translatedLines = translatedList.ToArray();
			return collLines;
		}

		void RaiseLayoutChanged(double effectiveViewportWidth, double effectiveViewportHeight, ITextViewLine[] newOrReformattedLines, ITextViewLine[] translatedLines) {
			if (IsClosed)
				return;
			var newViewState = new ViewState(this, effectiveViewportWidth, effectiveViewportHeight);
			LayoutChanged?.Invoke(this, new TextViewLayoutChangedEventArgs(oldViewState, newViewState, newOrReformattedLines, translatedLines));
			oldViewState = newViewState;
		}
		ViewState oldViewState;

		public void Close() {
			if (IsClosed)
				throw new InvalidOperationException();
			RegisteredCommandElement.Unregister();
			TextViewModel.Dispose();
			IsClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
			DnSpyTextEditor.Dispose();
			(aggregateClassifier as IDisposable)?.Dispose();
		}

		void InitializeOptions() {
			UpdateOption(DefaultOptions.TabSizeOptionId.Name);
			UpdateOption(DefaultOptions.IndentSizeOptionId.Name);
			UpdateOption(DefaultOptions.IndentStyleOptionId.Name);
			UpdateOption(DefaultOptions.NewLineCharacterOptionId.Name);
			UpdateOption(DefaultOptions.ReplicateNewLineCharacterOptionId.Name);
			UpdateOption(DefaultOptions.ConvertTabsToSpacesOptionId.Name);
			UpdateOption(DefaultTextViewHostOptions.HorizontalScrollBarId.Name);
			UpdateOption(DefaultTextViewHostOptions.VerticalScrollBarId.Name);
			UpdateOption(DefaultTextViewHostOptions.LineNumberMarginId.Name);
			UpdateOption(DefaultTextViewHostOptions.SelectionMarginId.Name);
			UpdateOption(DefaultTextViewHostOptions.GlyphMarginId.Name);
			UpdateOption(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId.Name);
			UpdateOption(DefaultTextViewOptions.DisplayUrlsAsHyperlinksId.Name);
			UpdateOption(DefaultTextViewOptions.DragDropEditingId.Name);
			UpdateOption(DefaultTextViewOptions.CanChangeOverwriteModeId.Name);
			UpdateOption(DefaultTextViewOptions.OverwriteModeId.Name);
			UpdateOption(DefaultTextViewOptions.UseVirtualSpaceId.Name);
			UpdateOption(DefaultTextViewOptions.CanChangeUseVisibleWhitespaceId.Name);
			UpdateOption(DefaultTextViewOptions.UseVisibleWhitespaceId.Name);
			UpdateOption(DefaultTextViewOptions.ViewProhibitUserInputId.Name);
			UpdateOption(DefaultTextViewOptions.CanChangeWordWrapStyleId.Name);
			UpdateOption(DefaultTextViewOptions.WordWrapStyleId.Name);
			UpdateOption(DefaultTextViewOptions.ScrollBelowDocumentId.Name);
			UpdateOption(DefaultTextViewOptions.AllowBoxSelectionId.Name);
			UpdateOption(DefaultTextViewOptions.HideCaretWhileTypingId.Name);
			UpdateOption(DefaultTextViewOptions.ShowColumnRulerId.Name);
			UpdateOption(DefaultTextViewOptions.ColumnRulerPositionId.Name);
			UpdateOption(DefaultWpfViewOptions.EnableHighlightCurrentLineId.Name);
			UpdateOption(DefaultWpfViewOptions.EnableMouseWheelZoomId.Name);
			UpdateOption(DefaultWpfViewOptions.ZoomLevelId.Name);
			UpdateOption(DefaultWpfViewOptions.AppearanceCategory.Name);
		}

		void UpdateOption(string optionId) {
			if (IsClosed)
				return;
			if (optionId == DefaultOptions.TabSizeOptionId.Name)
				DnSpyTextEditor.Options.IndentationSize = Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
			else if (optionId == DefaultOptions.IndentSizeOptionId.Name) {
				// Nothing to do
			}
			else if (optionId == DefaultOptions.IndentStyleOptionId.Name) {
				// Nothing to do
			}
			else if (optionId == DefaultOptions.NewLineCharacterOptionId.Name)
				DnSpyTextEditor.Options.NewLineCharacter = Options.GetOptionValue(DefaultOptions.NewLineCharacterOptionId);
			else if (optionId == DefaultOptions.ReplicateNewLineCharacterOptionId.Name)
				DnSpyTextEditor.Options.ReplicateNewLineCharacter = Options.GetOptionValue(DefaultOptions.ReplicateNewLineCharacterOptionId);
			else if (optionId == DefaultOptions.ConvertTabsToSpacesOptionId.Name)
				DnSpyTextEditor.Options.ConvertTabsToSpaces = Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
			else if (optionId == DefaultTextViewHostOptions.HorizontalScrollBarId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId);
				DnSpyTextEditor.HorizontalScrollBarVisibility = newValue ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
			}
			else if (optionId == DefaultTextViewHostOptions.VerticalScrollBarId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarId);
				DnSpyTextEditor.VerticalScrollBarVisibility = newValue ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
			}
			else if (optionId == DefaultTextViewHostOptions.LineNumberMarginId.Name)
				DnSpyTextEditor.ShowLineNumbers = Options.GetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId);
			else if (optionId == DefaultTextViewHostOptions.SelectionMarginId.Name) {
				bool enable = Options.GetOptionValue(DefaultTextViewHostOptions.SelectionMarginId);
				bool b = DnSpyTextEditor.TextArea.LeftMargins.Remove(paddingElement);
				Debug.Assert(b == !enable);
				if (enable)
					DnSpyTextEditor.TextArea.LeftMargins.Add(paddingElement);
			}
			else if (optionId == DefaultTextViewHostOptions.GlyphMarginId.Name) {
				//TODO:
			}
			else if (optionId == DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId.Name)
				DnSpyTextEditor.Options.CutCopyWholeLine = Options.GetOptionValue(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId);
			else if (optionId == DefaultTextViewOptions.DisplayUrlsAsHyperlinksId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewOptions.DisplayUrlsAsHyperlinksId);
				DnSpyTextEditor.Options.EnableHyperlinks = newValue;
				DnSpyTextEditor.Options.EnableEmailHyperlinks = newValue;
				DnSpyTextEditor.Options.RequireControlModifierForHyperlinkClick = false;
			}
			else if (optionId == DefaultTextViewOptions.DragDropEditingId.Name) {
				// DnSpyTextEditor.Options.EnableTextDragDrop also exists, but it's better to disable drag and drop
				// to the text area. If EnableTextDragDrop is false, the user can't drag selected text to another
				// window / application that supports drag and drop.
				DnSpyTextEditor.TextArea.AllowDrop = Options.GetOptionValue(DefaultTextViewOptions.DragDropEditingId);
			}
			else if (optionId == DefaultTextViewOptions.CanChangeOverwriteModeId.Name) {
				// Nothing to do
			}
			else if (optionId == DefaultTextViewOptions.OverwriteModeId.Name)
				DnSpyTextEditor.TextArea.OverstrikeMode = Options.GetOptionValue(DefaultTextViewOptions.OverwriteModeId);
			else if (optionId == DefaultTextViewOptions.UseVirtualSpaceId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId);
				DnSpyTextEditor.Options.EnableVirtualSpace = newValue;
				// Move to a non-virtual location
				if (!newValue && Caret.Position.VirtualSpaces > 0) {
					var line = GetTextViewLineContainingBufferPosition(Caret.Position.BufferPosition);
					Caret.MoveTo(line.End);
				}
			}
			else if (optionId == DefaultTextViewOptions.CanChangeUseVisibleWhitespaceId.Name) {
				// Nothing to do
			}
			else if (optionId == DefaultTextViewOptions.UseVisibleWhitespaceId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewOptions.UseVisibleWhitespaceId);
				DnSpyTextEditor.Options.ShowSpaces = newValue;
				DnSpyTextEditor.Options.ShowTabs = newValue;
			}
			else if (optionId == DefaultTextViewOptions.ViewProhibitUserInputId.Name)
				DnSpyTextEditor.IsReadOnly = Options.GetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId);
			else if (optionId == DefaultTextViewOptions.CanChangeWordWrapStyleId.Name) {
				// Nothing to do
			}
			else if (optionId == DefaultTextViewOptions.WordWrapStyleId.Name) {
				var newValue = Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId);
				DnSpyTextEditor.WordWrap = (newValue & WordWrapStyles.WordWrap) != 0;
				DnSpyTextEditor.Options.InheritWordWrapIndentation = (newValue & WordWrapStyles.AutoIndent) != 0;
			}
			else if (optionId == DefaultTextViewOptions.ScrollBelowDocumentId.Name)
				DnSpyTextEditor.Options.AllowScrollBelowDocument = Options.GetOptionValue(DefaultTextViewOptions.ScrollBelowDocumentId);
			else if (optionId == DefaultTextViewOptions.AllowBoxSelectionId.Name)
				DnSpyTextEditor.Options.EnableRectangularSelection = Options.GetOptionValue(DefaultTextViewOptions.AllowBoxSelectionId);
			else if (optionId == DefaultTextViewOptions.HideCaretWhileTypingId.Name)
				DnSpyTextEditor.Options.HideCursorWhileTyping = Options.GetOptionValue(DefaultTextViewOptions.HideCaretWhileTypingId);
			else if (optionId == DefaultTextViewOptions.ShowColumnRulerId.Name)
				DnSpyTextEditor.Options.ShowColumnRuler = Options.GetOptionValue(DefaultTextViewOptions.ShowColumnRulerId);
			else if (optionId == DefaultTextViewOptions.ColumnRulerPositionId.Name)
				DnSpyTextEditor.Options.ColumnRulerPosition = Options.GetOptionValue(DefaultTextViewOptions.ColumnRulerPositionId);
			else if (optionId == DefaultWpfViewOptions.EnableHighlightCurrentLineId.Name)
				DnSpyTextEditor.HighlightCurrentLine = Options.GetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId);
			else if (optionId == DefaultWpfViewOptions.EnableMouseWheelZoomId.Name) {
				// Nothing to do
			}
			else if (optionId == DefaultWpfViewOptions.ZoomLevelId.Name) {
				if (Roles.Contains(PredefinedTextViewRoles.Zoomable))
					ZoomLevel = Options.GetOptionValue(DefaultWpfViewOptions.ZoomLevelId);
			}
			else if (optionId == DefaultWpfViewOptions.AppearanceCategory.Name) {
				// Nothing to do
			}
		}

		ITextViewLine ITextView.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) => GetTextViewLineContainingBufferPosition(bufferPosition);
		public IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) {
			if (TextViewLines.IsValidSnapshot(bufferPosition.Snapshot)) {
				var line = TextViewLines.GetTextViewLineContainingBufferPosition(bufferPosition);
				if (line != null)
					return line;
			}
			return CreateWpfTextViewLine(bufferPosition);
		}

		IWpfTextViewLine CreateWpfTextViewLine(SnapshotPoint bufferPosition) {
			if (bufferPosition.Snapshot != TextSnapshot)
				throw new ArgumentException();
			var docLine = DnSpyTextEditor.TextArea.TextView.Document.GetLineByOffset(bufferPosition.Position);
			var visualLine = DnSpyTextEditor.TextArea.TextView.GetOrConstructVisualLine(docLine);

			var lineStart = docLine.Offset;
			var info = visualLine.TextLineInfos.FirstOrDefault(a => lineStart + a.StartOffset <= bufferPosition.Position && bufferPosition.Position < lineStart + a.EndOffset);
			if (info == null)
				info = visualLine.TextLineInfos[visualLine.TextLineInfos.Count - 1];
			var top = visualLine.VisualTop - DnSpyTextEditor.TextArea.TextView.VerticalOffset + ViewportTop;
			foreach (var x in visualLine.TextLineInfos) {
				if (x == info)
					break;
				top += x.TextLine.Height;
			}

			var change = TextViewLineChange.None;
			double deltaY = 0;

			var visibleArea = new Rect(ViewportLeft, ViewportTop, ViewportWidth, ViewportHeight);
			double virtualSpaceWidth = DnSpyTextEditor.TextArea.TextView.WideSpaceWidth;
			return new WpfTextViewLine(null, TextSnapshot, visualLine, info, top, deltaY, change, visibleArea, virtualSpaceWidth);
		}

		public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo) =>
			DisplayTextLineContainingBufferPosition(bufferPosition, verticalDistance, relativeTo, null, null);
		public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride) {
			if (bufferPosition.Snapshot != TextSnapshot)
				throw new ArgumentException();

			double viewportWidth = viewportWidthOverride ?? ViewportWidth;
			double viewportHeight = viewportHeightOverride ?? ViewportHeight;
			if (viewportWidth != ViewportWidth)
				throw new NotSupportedException();
			if (viewportHeight != ViewportHeight)
				throw new NotSupportedException();

			var textView = DnSpyTextEditor.TextArea.TextView;
			var viewLine = GetTextViewLineContainingBufferPosition(bufferPosition);
			double newTop;
			Point point;
			var textViewPosition = Utils.ToTextViewPosition(DnSpyTextEditor, new VirtualSnapshotPoint(bufferPosition), isAtEndOfLine: false);
			switch (relativeTo) {
			case ViewRelativePosition.Top:
				point = textView.GetVisualPosition(textViewPosition, VisualYPosition.LineTop);
				newTop = point.Y - verticalDistance;
				break;

			case ViewRelativePosition.Bottom:
				point = textView.GetVisualPosition(textViewPosition, VisualYPosition.LineBottom);
				newTop = point.Y - ViewportHeight + verticalDistance;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(relativeTo));
			}
			if (newTop < 0)
				newTop = 0;
			((IScrollInfo)textView).SetVerticalOffset(newTop);
		}

		public SnapshotSpan GetTextElementSpan(SnapshotPoint point) {
			if (point.Snapshot != TextSnapshot)
				throw new ArgumentException();
			return GetTextViewLineContainingBufferPosition(point).GetTextElementSpan(point);
		}

		internal Tuple<VisualLine, TextLine> HACK_GetVisualLine(WpfTextViewLine line) {
			var line2 = (WpfTextViewLine)CreateWpfTextViewLine(line.Start);
			return Tuple.Create(line2.VisualLine, line2.TextLine);
		}

		public IAdornmentLayer GetAdornmentLayer(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			var mdLayer = adornmentLayerDefinitionService.GetLayerDefinition(Guid.Parse(name));
			if (mdLayer == null)
				throw new ArgumentException($"Adornment layer {name} doesn't exist");
			return adornmentLayerCollection.GetAdornmentLayer(mdLayer);
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
			adornmentLayerCollection.OnParentSizeChanged(sizeInfo.NewSize);
			base.OnRenderSizeChanged(sizeInfo);
		}
	}
}
