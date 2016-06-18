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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
		public FrameworkElement VisualElement => this;
		public ITextViewRoleSet Roles { get; }
		public IEditorOptions Options { get; }
		public ICommandTargetCollection CommandTarget => RegisteredCommandElement.CommandTarget;
		IRegisteredCommandElement RegisteredCommandElement { get; }
		public ITextCaret Caret => TextCaret;
		TextCaret TextCaret { get; }
		ITextSelection ITextView.Selection => Selection;
		TextSelection Selection { get; }
		public IEditorOperations2 EditorOperations { get; }
		public IViewScroller ViewScroller { get; }
		public bool HasAggregateFocus => this.IsKeyboardFocusWithin;
		public bool IsMouseOverViewOrAdornments => this.IsMouseOver;
		//TODO: Remove public from this property once all refs to it from REPL and LOG editors have been removed
		public DnSpyTextEditor DnSpyTextEditor { get; }
		public IFormattedLineSource FormattedLineSource { get; private set; }

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
			this.zoomLevel = ZoomConstants.DefaultZoom;
			this.adornmentLayerDefinitionService = adornmentLayerDefinitionService;
			this.adornmentLayerCollection = new AdornmentLayerCollection(this);
			Properties = new PropertyCollection();
			DnSpyTextEditor = dnSpyTextEditor;
			TextViewLines = new WpfTextViewLineCollection();
			TextViewModel = textViewModel;
			Roles = roles;
			Options = editorOptionsFactoryService.GetOptions(this);
			Options.Parent = parentOptions;
			Options.OptionChanged += EditorOptions_OptionChanged;
			EditorOperations = editorOperationsFactoryService.GetEditorOperations(this);
			ViewScroller = new ViewScroller(this);
			hasKeyboardFocus = this.IsKeyboardFocusWithin;
			oldViewState = new ViewState(this);
			this.aggregateClassifier = viewClassifierAggregatorService.GetClassifier(this);
			this.textAndAdornmentSequencer = textAndAdornmentSequencerFactoryService.Create(this);
			this.classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(this);
			if (Roles.Contains(PredefinedTextViewRoles.Interactive))
				RegisteredCommandElement = commandManager.Register(VisualElement, this);
			else
				RegisteredCommandElement = NullRegisteredCommandElement.Instance;

			Selection = new TextSelection(this, GetAdornmentLayer(PredefinedAdornmentLayers.Selection));
			TextCaret = new TextCaret(this, GetAdornmentLayer(PredefinedAdornmentLayers.Caret), smartIndentationService);

			Children.Add(adornmentLayerCollection);
			this.Cursor = Cursors.IBeam;
			this.Focusable = true;
			this.FocusVisualStyle = null;
			InitializeOptions();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);
			if (e.Property == TextOptions.TextFormattingModeProperty)
				CreateFormattedLineSource();
		}

		//TODO: Call this each time one of the values it uses gets updated or when default font/fg/bg/etc changes
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

		protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e) {
			UpdateKeyboardFocus();
			base.OnIsKeyboardFocusWithinChanged(e);
		}

		bool hasKeyboardFocus;
		public event EventHandler GotAggregateFocus;
		public event EventHandler LostAggregateFocus;
		void UpdateKeyboardFocus() {
			bool newValue = this.IsKeyboardFocusWithin;
			if (hasKeyboardFocus != newValue) {
				hasKeyboardFocus = newValue;
				if (hasKeyboardFocus)
					GotAggregateFocus?.Invoke(this, EventArgs.Empty);
				else
					LostAggregateFocus?.Invoke(this, EventArgs.Empty);
			}
		}

		void EditorOptions_OptionChanged(object sender, EditorOptionChangedEventArgs e) => UpdateOption(e.OptionId);

		public ITextBuffer TextBuffer => TextViewModel.EditBuffer;
		public ITextSnapshot TextSnapshot => TextBuffer.CurrentSnapshot;
		public ITextSnapshot VisualSnapshot => TextViewModel.VisualBuffer.CurrentSnapshot;
		public ITextDataModel TextDataModel => TextViewModel.DataModel;
		public ITextViewModel TextViewModel { get; }

		public bool IsClosed { get; set; }

		public new Brush Background {
			get { return base.Background; }
			set {
				if (base.Background != value) {
					base.Background = value;
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

		public double LineHeight => this.FormattedLineSource.LineHeight;
		public double ViewportTop => viewportTop;
		public double ViewportBottom => ViewportTop + ViewportHeight;
		public double ViewportRight => ViewportLeft + ViewportWidth;
		public double ViewportWidth => ActualWidth;
		public double ViewportHeight => ActualHeight;
		public double ViewportLeft {
			get { return viewportLeft; }
			set {
				if (double.IsNaN(value))
					throw new ArgumentOutOfRangeException(nameof(value));
				double left = value;
				if ((Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) & WordWrapStyles.WordWrap) != 0)
					left = 0;
				if (left < 0)
					left = 0;
				if (viewportLeft == left)
					return;
				viewportLeft = left;
				SetLeft(adornmentLayerCollection, -viewportLeft);
				//TODO: RaiseLayoutChanged(), update lines
				ViewportLeftChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		double viewportTop, viewportLeft;

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

		void UpdateVisibleArea() {
			foreach (WpfTextViewLine line in TextViewLines)
				line.VisibleArea = new Rect(ViewportLeft, ViewportTop, ViewportWidth, ViewportHeight);
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
			TextCaret.Dispose();
			Selection.Dispose();
		}

		void InitializeOptions() {
			UpdateOption(DefaultOptions.TabSizeOptionId.Name);
			UpdateOption(DefaultOptions.IndentStyleOptionId.Name);
			UpdateOption(DefaultTextViewOptions.WordWrapStyleId.Name);
			UpdateOption(DefaultWpfViewOptions.ZoomLevelId.Name);
		}

		void UpdateOption(string optionId) {
			if (IsClosed)
				return;
			if (optionId == DefaultOptions.TabSizeOptionId.Name) {
				//TODO: Repaint
			}
			else if (optionId == DefaultOptions.IndentStyleOptionId.Name) {
				//TODO: Repaint
			}
			else if (optionId == DefaultTextViewOptions.WordWrapStyleId.Name) {
				//TODO: Repaint
			}
			else if (optionId == DefaultWpfViewOptions.ZoomLevelId.Name) {
				if (Roles.Contains(PredefinedTextViewRoles.Zoomable))
					ZoomLevel = Options.GetOptionValue(DefaultWpfViewOptions.ZoomLevelId);
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

		//TODO: Cache the lines
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
			return new WpfTextViewLine(TextSnapshot, visualLine, info, top, deltaY, change, visibleArea, virtualSpaceWidth);
		}

		public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo) =>
			DisplayTextLineContainingBufferPosition(bufferPosition, verticalDistance, relativeTo, null, null);
		public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride) {
			if (bufferPosition.Snapshot != TextSnapshot)
				throw new ArgumentException();

			double viewportWidth = viewportWidthOverride ?? ViewportWidth;
			double viewportHeight = viewportHeightOverride ?? ViewportHeight;

			var viewLine = GetTextViewLineContainingBufferPosition(bufferPosition);
			double newTop;
			switch (relativeTo) {
			case ViewRelativePosition.Top:
				newTop = viewLine.Top - verticalDistance;
				break;

			case ViewRelativePosition.Bottom:
				newTop = viewLine.Bottom - ViewportHeight + verticalDistance;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(relativeTo));
			}
			if (newTop < 0)
				newTop = 0;
			viewportTop = newTop;
			//TODO: call RaiseLayoutChanged
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
			if (!IsClosed) {
				adornmentLayerCollection.OnParentSizeChanged(sizeInfo.NewSize);
				if (sizeInfo.PreviousSize.Height != sizeInfo.NewSize.Height)
					ViewportHeightChanged?.Invoke(this, EventArgs.Empty);
				if (sizeInfo.PreviousSize.Width != sizeInfo.NewSize.Width)
					ViewportWidthChanged?.Invoke(this, EventArgs.Empty);
				UpdateVisibleArea();
			}
			base.OnRenderSizeChanged(sizeInfo);
		}

		protected override Size MeasureOverride(Size constraint) =>
			new Size(FilterLength(constraint.Width, Width), FilterLength(constraint.Height, Height));

		static double FilterLength(double length1, double length2) {
			if (IsValidLength(length1))
				return length1;
			if (IsValidLength(length2))
				return length2;
			return 42;
		}

		static bool IsValidLength(double v) => v != double.PositiveInfinity && !double.IsNaN(v) && v >= 0;
	}
}
