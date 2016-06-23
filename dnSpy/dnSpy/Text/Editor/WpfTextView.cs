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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed partial class WpfTextView : Canvas, IWpfTextView {
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
		public ITextBuffer TextBuffer => TextViewModel.EditBuffer;
		public ITextSnapshot TextSnapshot => TextBuffer.CurrentSnapshot;
		public ITextSnapshot VisualSnapshot => TextViewModel.VisualBuffer.CurrentSnapshot;
		public ITextDataModel TextDataModel => TextViewModel.DataModel;
		public ITextViewModel TextViewModel { get; }
		public bool IsClosed { get; set; }
		public double MaxTextRightCoordinate { get { throw new NotImplementedException(); } }//TODO: Use this prop
		public ITrackingSpan ProvisionalTextHighlight { get; set; }//TODO: Use this prop
		public event EventHandler GotAggregateFocus;
		public event EventHandler LostAggregateFocus;
		public event EventHandler Closed;
		public event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged;
		public event EventHandler ViewportLeftChanged;
		public event EventHandler ViewportHeightChanged;
		public event EventHandler ViewportWidthChanged;
		public event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged;
		public event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged;
#pragma warning disable CS0067
		public event EventHandler<MouseHoverEventArgs> MouseHover;//TODO: Use this event
#pragma warning restore CS0067
		//TODO: Remove public from this property once all refs to it from REPL and LOG editors have been removed
		public DnSpyTextEditor DnSpyTextEditor { get; }
		public IFormattedLineSource FormattedLineSource { get; private set; }
		public bool InLayout { get; private set; }
		ITextViewLineCollection ITextView.TextViewLines => TextViewLines;
		IWpfTextViewLineCollection IWpfTextView.TextViewLines => TextViewLines;

		readonly IFormattedTextSourceFactoryService formattedTextSourceFactoryService;
		readonly IClassifier aggregateClassifier;
		readonly ITextAndAdornmentSequencer textAndAdornmentSequencer;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IAdornmentLayerDefinitionService adornmentLayerDefinitionService;
		readonly AdornmentLayerCollection adornmentLayerCollection;
		readonly PhysicalLineCache physicalLineCache;
		readonly List<PhysicalLine> visiblePhysicalLines;
		readonly TextLayer textLayer;

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
			this.physicalLineCache = new PhysicalLineCache(32);
			this.visiblePhysicalLines = new List<PhysicalLine>();
			this.invalidatedRegions = new List<SnapshotSpan>();
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

			this.textLayer = new TextLayer(this, GetAdornmentLayer(PredefinedAdornmentLayers.Text));
			Selection = new TextSelection(this, GetAdornmentLayer(PredefinedAdornmentLayers.Selection));
			TextCaret = new TextCaret(this, GetAdornmentLayer(PredefinedAdornmentLayers.Caret), smartIndentationService);

			Children.Add(adornmentLayerCollection);
			this.Cursor = Cursors.IBeam;
			this.Focusable = true;
			this.FocusVisualStyle = null;
			InitializeOptions();

			Options.OptionChanged += EditorOptions_OptionChanged;
			TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			TextBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;
			aggregateClassifier.ClassificationChanged += AggregateClassifier_ClassificationChanged;
			textAndAdornmentSequencer.SequenceChanged += TextAndAdornmentSequencer_SequenceChanged;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;

			Background = classificationFormatMap.DefaultWindowBackground;
			CreateFormattedLineSource(ViewportWidth);
		}

		public void InvalidateClassifications(SnapshotSpan span) {
			Dispatcher.VerifyAccess();
			if (span.Snapshot == null)
				throw new ArgumentException();
			InvalidateSpans(new[] { span });
		}

		void DelayScreenRefresh() {
			if (IsClosed)
				return;
			if (screenRefreshTimer != null)
				return;
			int ms = Options.GetOptionValue(DefaultTextViewOptions.RefreshScreenOnChangeWaitMilliSecsId);
			if (ms > 0)
				screenRefreshTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(ms), DispatcherPriority.Normal, RefreshScreenHandler, Dispatcher);
			else
				RefreshScreen();
		}
		DispatcherTimer screenRefreshTimer;

		void RefreshScreen() => DelayLayoutLines(true);
		void RefreshScreenHandler(object sender, EventArgs e) {
			StopRefreshTimer();
			RefreshScreen();
		}

		void StopRefreshTimer() {
			screenRefreshTimer?.Stop();
			screenRefreshTimer = null;
		}

		void TextBuffer_ContentTypeChanged(object sender, ContentTypeChangedEventArgs e) {
			// Refresh all lines since IFormattedTextSourceFactoryService uses the content type to
			// pick a ITextParagraphPropertiesFactoryService
			InvalidateFormattedLineSource(true);
		}

		void TextBuffer_ChangedLowPriority(object sender, TextContentChangedEventArgs e) {
			foreach (var c in e.Changes) {
				if (c.OldSpan.Length > 0)
					InvalidateSpan(new SnapshotSpan(e.Before, c.OldSpan));
				if (c.NewSpan.Length > 0)
					InvalidateSpan(new SnapshotSpan(e.After, c.NewSpan));
			}
			InvalidateFormattedLineSource(false);
			if (Options.GetOptionValue(DefaultTextViewOptions.RefreshScreenOnChangeId))
				DelayScreenRefresh();
		}

		void AggregateClassifier_ClassificationChanged(object sender, ClassificationChangedEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() => InvalidateSpan(e.ChangeSpan)), DispatcherPriority.Normal);

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			// It uses the classification format map and must use the latest changes
			Dispatcher.BeginInvoke(new Action(() => {
				Background = classificationFormatMap.DefaultWindowBackground;
				InvalidateFormattedLineSource(true);
			}), DispatcherPriority.Normal);
		}

		void TextAndAdornmentSequencer_SequenceChanged(object sender, TextAndAdornmentSequenceChangedEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() => InvalidateSpans(e.Span.GetSpans(TextBuffer))), DispatcherPriority.Normal);

		void InvalidateSpans(IEnumerable<SnapshotSpan> spans) {
			Dispatcher.VerifyAccess();
			int count = invalidatedRegions.Count;
			invalidatedRegions.AddRange(spans);
			if (invalidatedRegions.Count != count)
				DelayLayoutLines();
		}

		void InvalidateSpan(SnapshotSpan span) {
			Dispatcher.VerifyAccess();
			invalidatedRegions.Add(span);
			DelayLayoutLines();
		}

		void DelayLayoutLines(bool refreshAllLines = false) {
			Dispatcher.VerifyAccess();
			if (IsClosed)
				return;
			if (refreshAllLines) {
				invalidatedRegions.Clear();
				invalidatedRegions.Add(new SnapshotSpan(TextSnapshot, 0, TextSnapshot.Length));
			}
			if (delayLayoutLinesInProgress)
				return;
			delayLayoutLinesInProgress = true;
			Dispatcher.BeginInvoke(new Action(DelayLayoutLinesHandler), DispatcherPriority.DataBind);
		}
		bool delayLayoutLinesInProgress;
		readonly List<SnapshotSpan> invalidatedRegions;
		bool formattedLineSourceIsInvalidated;

		void DelayLayoutLinesHandler() {
			Dispatcher.VerifyAccess();
			if (IsClosed)
				return;
			if (!delayLayoutLinesInProgress)
				return;
			delayLayoutLinesInProgress = false;

			SnapshotPoint bufferPosition;
			double verticalDistance;
			if (TextViewLines.Count == 0) {
				verticalDistance = 0;
				bufferPosition = new SnapshotPoint(TextSnapshot, 0);
			}
			else {
				var line = TextViewLines.FirstVisibleLine;
				verticalDistance = line.Top - ViewportTop;
				bufferPosition = line.Start.TranslateTo(TextSnapshot, PointTrackingMode.Negative);
			}

			DisplayLines(bufferPosition, verticalDistance, ViewRelativePosition.Top, ViewportWidth, ViewportHeight);
		}

		void InvalidateFormattedLineSource(bool refreshAllLines) {
			Dispatcher.VerifyAccess();
			formattedLineSourceIsInvalidated = true;
			DelayLayoutLines(refreshAllLines);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);
			if (e.Property == TextOptions.TextFormattingModeProperty)
				InvalidateFormattedLineSource(true);
		}

		void EditorOptions_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			UpdateOption(e.OptionId);
			if (e.OptionId == DefaultTextViewOptions.WordWrapStyleId.Name) {
				if ((Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) & WordWrapStyles.WordWrap) != 0)
					ViewportLeft = 0;
				InvalidateFormattedLineSource(true);
			}
			else if (e.OptionId == DefaultOptions.TabSizeOptionId.Name)
				InvalidateFormattedLineSource(true);
			else if (e.OptionId == DefaultTextViewOptions.RefreshScreenOnChangeId.Name) {
				if (!Options.GetOptionValue(DefaultTextViewOptions.RefreshScreenOnChangeId))
					StopRefreshTimer();
			}
		}

		double lastFormattedLineSourceViewportWidth = double.NaN;
		void CreateFormattedLineSource(double viewportWidthOverride) {
			lastFormattedLineSourceViewportWidth = viewportWidthOverride;
			var wordWrapStyle = Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId);
			bool isWordWrap = (wordWrapStyle & WordWrapStyles.WordWrap) != 0;
			bool isAutoIndent = isWordWrap && (wordWrapStyle & WordWrapStyles.AutoIndent) != 0;
			double wordWrapWidth = isWordWrap ? viewportWidthOverride : 0;
			var maxAutoIndent = isAutoIndent ? viewportWidthOverride / 4 : 0;
			bool useDisplayMode = zoomLevel != 100 || TextOptions.GetTextFormattingMode(this) == TextFormattingMode.Display;

			int tabSize = Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
			tabSize = Math.Max(1, tabSize);
			tabSize = Math.Min(60, tabSize);

			// This value is what VS uses, see: https://msdn.microsoft.com/en-us/library/microsoft.visualstudio.text.formatting.iformattedlinesource.baseindentation.aspx
			//	"This is generally a small value like 2.0, so that some characters (such as an italic
			//	 slash) will not be clipped by the left edge of the view."
			const double baseIndent = 2.0;
			(FormattedLineSource as IDisposable)?.Dispose();
			FormattedLineSource = formattedTextSourceFactoryService.Create(
				TextSnapshot,
				VisualSnapshot,
				tabSize,
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
				double newValue = value;
				newValue = Math.Min(ZoomConstants.MaxZoom, newValue);
				newValue = Math.Max(ZoomConstants.MinZoom, newValue);
				if (double.IsNaN(newValue) || Math.Abs(newValue - ZoomConstants.DefaultZoom) < 0.01)
					newValue = ZoomConstants.DefaultZoom;
				if (newValue == zoomLevel)
					return;

				//TODO: Use TabElementScaler since it works correctly when in high dpi mode
				if (newValue == 100)
					LayoutTransform = Transform.Identity;
				else {
					double scale = newValue / 100;
					var st = new ScaleTransform(scale, scale);
					st.Freeze();
					LayoutTransform = st;
				}
				// If display/ideal mode changed
				if (newValue == 100 || zoomLevel == 100)
					InvalidateFormattedLineSource(true);

				zoomLevel = newValue;
				ZoomLevelChanged?.Invoke(this, new ZoomLevelChangedEventArgs(newValue, LayoutTransform));
			}
		}
		double zoomLevel;

		public WpfTextViewLineCollection TextViewLines {
			get {
				if (InLayout)
					throw new InvalidOperationException();
				Debug.Assert(wpfTextViewLineCollection != null);
				return wpfTextViewLineCollection;
			}
			private set { wpfTextViewLineCollection = value; }
		}
		WpfTextViewLineCollection wpfTextViewLineCollection;

		public double LineHeight => FormattedLineSource.LineHeight;
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
				UpdateVisibleLines();
				SetLeft(adornmentLayerCollection, -viewportLeft);
				RaiseLayoutChanged();
				ViewportLeftChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		double viewportTop, viewportLeft;

		void RaiseLayoutChanged() => RaiseLayoutChanged(ViewportWidth, ViewportHeight, Array.Empty<ITextViewLine>(), Array.Empty<ITextViewLine>());
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
			StopRefreshTimer();
			RegisteredCommandElement.Unregister();
			TextViewModel.Dispose();
			IsClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
			DnSpyTextEditor.Dispose();
			(aggregateClassifier as IDisposable)?.Dispose();
			TextCaret.Dispose();
			Selection.Dispose();
			(FormattedLineSource as IDisposable)?.Dispose();
			physicalLineCache.Dispose();
			textLayer.Dispose();
			foreach (var physLine in visiblePhysicalLines)
				physLine.Dispose();
			visiblePhysicalLines.Clear();

			Options.OptionChanged -= EditorOptions_OptionChanged;
			TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			TextBuffer.ContentTypeChanged -= TextBuffer_ContentTypeChanged;
			aggregateClassifier.ClassificationChanged -= AggregateClassifier_ClassificationChanged;
			textAndAdornmentSequencer.SequenceChanged -= TextAndAdornmentSequencer_SequenceChanged;
			classificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
		}

		void InitializeOptions() =>
			UpdateOption(DefaultWpfViewOptions.ZoomLevelId.Name);

		void UpdateOption(string optionId) {
			if (IsClosed)
				return;
			if (optionId == DefaultWpfViewOptions.ZoomLevelId.Name) {
				if (Roles.Contains(PredefinedTextViewRoles.Zoomable))
					ZoomLevel = Options.GetOptionValue(DefaultWpfViewOptions.ZoomLevelId);
			}
		}

		ITextViewLine ITextView.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) => GetTextViewLineContainingBufferPosition(bufferPosition);
		public IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) {
			if (IsClosed)
				throw new InvalidOperationException();
			if (bufferPosition.Snapshot != TextSnapshot)
				throw new ArgumentException();

			if (visiblePhysicalLines.Count != 0 && visiblePhysicalLines[0].BufferSpan.Snapshot == bufferPosition.Snapshot) {
				foreach (var pline in visiblePhysicalLines) {
					var lline = pline.FindFormattedLineByBufferPosition(bufferPosition);
					if (lline != null)
						return lline;
				}
			}

			var cachedLine = physicalLineCache.FindFormattedLineByBufferPosition(bufferPosition);
			if (cachedLine != null)
				return cachedLine;

			var physLine = CreatePhysicalLineNoCache(bufferPosition, ViewportWidth);
			physicalLineCache.Add(physLine);
			var line = physLine.FindFormattedLineByBufferPosition(bufferPosition);
			if (line == null)
				throw new InvalidOperationException();
			return line;
		}

		PhysicalLine CreatePhysicalLineNoCache(SnapshotPoint bufferPosition, double viewportWidthOverride) {
			if (bufferPosition.Snapshot != TextSnapshot)
				throw new ArgumentException();
			if (formattedLineSourceIsInvalidated || FormattedLineSource.SourceTextSnapshot != TextSnapshot)
				CreateFormattedLineSource(viewportWidthOverride);
			return CreatePhysicalLineNoCache(FormattedLineSource, TextViewModel, VisualSnapshot, bufferPosition);
		}

		static PhysicalLine CreatePhysicalLineNoCache(IFormattedLineSource formattedLineSource, ITextViewModel textViewModel, ITextSnapshot visualSnapshot, SnapshotPoint bufferPosition) {
			var visualPoint = textViewModel.GetNearestPointInVisualSnapshot(bufferPosition, visualSnapshot, PointTrackingMode.Positive);
			var lines = formattedLineSource.FormatLineInVisualBuffer(visualPoint.GetContainingLine());
			Debug.Assert(lines.Count > 0);
			return new PhysicalLine(bufferPosition.GetContainingLine(), lines);
		}

		public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo) =>
			DisplayTextLineContainingBufferPosition(bufferPosition, verticalDistance, relativeTo, null, null);
		public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride) =>
			DisplayLines(bufferPosition, verticalDistance, relativeTo, viewportWidthOverride ?? ViewportWidth, viewportHeightOverride ?? ViewportHeight);

		double lastViewportWidth = double.NaN;
		void DisplayLines(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double viewportWidthOverride, double viewportHeightOverride) {
			if (IsClosed)
				throw new InvalidOperationException();
			Dispatcher.VerifyAccess();
			if (bufferPosition.Snapshot != TextSnapshot)
				throw new ArgumentException();
			if (relativeTo != ViewRelativePosition.Top && relativeTo != ViewRelativePosition.Bottom)
				throw new ArgumentOutOfRangeException(nameof(relativeTo));
			if (viewportHeightOverride < 0 || double.IsNaN(viewportHeightOverride))
				throw new ArgumentOutOfRangeException(nameof(viewportHeightOverride));
			if (viewportWidthOverride < 0 || double.IsNaN(viewportWidthOverride))
				throw new ArgumentOutOfRangeException(nameof(viewportWidthOverride));

			// Don't allow too big distances since we have to format all lines until we find the viewport
			double maxDist = viewportHeightOverride * 2;
			if (verticalDistance < -maxDist)
				verticalDistance = -maxDist;
			else if (verticalDistance > maxDist)
				verticalDistance = maxDist;

			bool invalidateAllLines = false;
			if (viewportWidthOverride != lastViewportWidth || viewportWidthOverride != lastFormattedLineSourceViewportWidth) {
				invalidateAllLines = true;
				lastViewportWidth = viewportWidthOverride;
			}

			// Make sure the scheduled method doesn't try to call this method
			delayLayoutLinesInProgress = false;

			if (invalidateAllLines) {
				invalidatedRegions.Clear();
				invalidatedRegions.Add(new SnapshotSpan(TextSnapshot, 0, TextSnapshot.Length));
			}
			var regionsToInvalidate = new NormalizedSnapshotSpanCollection(invalidatedRegions.Select(a => a.TranslateTo(TextSnapshot, SpanTrackingMode.EdgeInclusive)));
			invalidatedRegions.Clear();
			if (invalidatedRegions.Capacity > 100)
				invalidatedRegions.TrimExcess();

			if (invalidateAllLines || formattedLineSourceIsInvalidated) {
				CreateFormattedLineSource(viewportWidthOverride);
				formattedLineSourceIsInvalidated = false;
			}
			Debug.Assert(FormattedLineSource.SourceTextSnapshot == TextSnapshot && FormattedLineSource.TopTextSnapshot == VisualSnapshot);

			if (InLayout)
				throw new InvalidOperationException();
			InLayout = true;
			wpfTextViewLineCollection.Invalidate();

			var layoutHelper = new LayoutHelper(GetValidCachedLines(regionsToInvalidate), FormattedLineSource, TextViewModel, VisualSnapshot, TextSnapshot);
			layoutHelper.LayoutLines(bufferPosition, relativeTo, verticalDistance, ViewportLeft, viewportWidthOverride, viewportHeightOverride);

			visiblePhysicalLines.AddRange(layoutHelper.AllVisiblePhysicalLines);
			TextViewLines = new WpfTextViewLineCollection(TextSnapshot, layoutHelper.AllVisibleLines);

			if (!InLayout)
				throw new InvalidOperationException();
			InLayout = false;

			textLayer.AddVisibleLines(layoutHelper.AllVisibleLines);
			var newOrReformattedLines = layoutHelper.NewOrReformattedLines.ToArray();
			var translatedLines = layoutHelper.TranslatedLines.ToArray();

			viewportTop = layoutHelper.NewViewportTop;
			SetTop(adornmentLayerCollection, -viewportTop);
			RaiseLayoutChanged(viewportWidthOverride, viewportHeightOverride, newOrReformattedLines, translatedLines);
		}

		List<PhysicalLine> GetValidCachedLines(NormalizedSnapshotSpanCollection regionsToInvalidate) {
			var lines = new List<PhysicalLine>(visiblePhysicalLines);
			lines.AddRange(physicalLineCache.TakeOwnership());
			visiblePhysicalLines.Clear();

			// Common enough that it's worth checking
			bool invalidateAll = false;
			if (regionsToInvalidate.Count == 1) {
				var r = regionsToInvalidate[0];
				if (r.Start.Position == 0 && r.End.Position == r.Snapshot.Length)
					invalidateAll = true;
			}
			if (invalidateAll) {
				foreach (var line in lines)
					line.Dispose();
				lines.Clear();
				return lines;
			}

			for (int i = lines.Count - 1; i >= 0; i--) {
				var line = lines[i];
				bool remove = line.TranslateTo(VisualSnapshot, TextSnapshot) || line.OverlapsWith(regionsToInvalidate);
				if (remove) {
					line.Dispose();
					lines.RemoveAt(i);
				}
				else
					line.UpdateIsLastLine();
			}

			return lines;
		}

		public SnapshotSpan GetTextElementSpan(SnapshotPoint point) {
			if (point.Snapshot != TextSnapshot)
				throw new ArgumentException();
			return GetTextViewLineContainingBufferPosition(point).GetTextElementSpan(point);
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
				if (sizeInfo.PreviousSize.Width != sizeInfo.NewSize.Width) {
					ViewportWidthChanged?.Invoke(this, EventArgs.Empty);
					if ((Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) & WordWrapStyles.WordWrap) != 0)
						InvalidateFormattedLineSource(true);
				}
				UpdateVisibleLines();
				RaiseLayoutChanged();
				InvalidateFormattedLineSource(false);
			}
			base.OnRenderSizeChanged(sizeInfo);
		}

		void UpdateVisibleLines() => UpdateVisibleLines(ViewportWidth, ViewportHeight);
		void UpdateVisibleLines(double viewportWidthOverride, double ViewportHeightOverride) {
			foreach (IFormattedLine line in TextViewLines)
				line.SetVisibleArea(new Rect(ViewportLeft, ViewportTop, viewportWidthOverride, ViewportHeightOverride));
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
