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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using dnSpy.Text.Classification;
using dnSpy.Text.Formatting;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	interface IDnSpyWpfTextViewImpl : IDnSpyWpfTextView {
		bool IsMouseOverOverlayLayerElement(MouseEventArgs e);
	}

	sealed partial class WpfTextView : Canvas, IDnSpyWpfTextView, ILineTransformSource, IDnSpyWpfTextViewImpl {
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
		public IViewScroller ViewScroller { get; }
		public bool HasAggregateFocus => IsKeyboardFocusWithin || spaceReservationStack.HasAggregateFocus;
		public bool IsMouseOverViewOrAdornments => IsMouseOver || spaceReservationStack.IsMouseOver;
		public ITextBuffer TextBuffer => TextViewModel.EditBuffer;
		public ITextSnapshot TextSnapshot => TextBuffer.CurrentSnapshot;
		public ITextSnapshot VisualSnapshot => TextViewModel.VisualBuffer.CurrentSnapshot;
		public ITextDataModel TextDataModel => TextViewModel.DataModel;
		public ITextViewModel TextViewModel { get; }
		public bool IsClosed { get; set; }
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
		public IFormattedLineSource FormattedLineSource { get; private set; }
		public bool InLayout { get; private set; }
		ITextViewLineCollection ITextView.TextViewLines => TextViewLines;
		IWpfTextViewLineCollection IWpfTextView.TextViewLines => TextViewLines;

		public IBufferGraph BufferGraph {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public double MaxTextRightCoordinate {
			get {
				double max = 0;
				var snapshot = TextSnapshot;
				foreach (var p in visiblePhysicalLines) {
					if (p.BufferSpan.Snapshot == snapshot) {
						foreach (var l in p.Lines)
							max = Math.Max(max, l.Right);
					}
				}
				return max;
			}
		}

		readonly IFormattedTextSourceFactoryService formattedTextSourceFactoryService;
		readonly IClassifier aggregateClassifier;
		readonly ITextAndAdornmentSequencer textAndAdornmentSequencer;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IEditorFormatMap editorFormatMap;
		readonly ISpaceReservationStack spaceReservationStack;
		readonly IAdornmentLayerDefinitionService adornmentLayerDefinitionService;
		readonly ILineTransformProviderService lineTransformProviderService;
		readonly Lazy<IWpfTextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>[] wpfTextViewCreationListeners;
		readonly AdornmentLayerCollection normalAdornmentLayerCollection;
		readonly AdornmentLayerCollection overlayAdornmentLayerCollection;
		readonly AdornmentLayerCollection underlayAdornmentLayerCollection;
		readonly PhysicalLineCache physicalLineCache;
		readonly List<PhysicalLine> visiblePhysicalLines;
		readonly TextLayer textLayer;

#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedAdornmentLayers.Text)]
		[Order(After = PredefinedDnSpyAdornmentLayers.BottomLayer, Before = PredefinedDnSpyAdornmentLayers.TopLayer)]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Caret)]
		static readonly AdornmentLayerDefinition textAdornmentLayerDefinition;

		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedAdornmentLayers.Caret)]
		[Order(After = PredefinedDnSpyAdornmentLayers.BottomLayer, Before = PredefinedDnSpyAdornmentLayers.TopLayer)]
		[Order(After = PredefinedAdornmentLayers.Text)]
		static readonly AdornmentLayerDefinition caretAdornmentLayerDefinition;

		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedAdornmentLayers.Selection)]
		[Order(After = PredefinedDnSpyAdornmentLayers.BottomLayer, Before = PredefinedDnSpyAdornmentLayers.TopLayer)]
		[Order(Before = PredefinedAdornmentLayers.Text)]
		static readonly AdornmentLayerDefinition selectionAdornmentLayerDefinition;
#pragma warning restore 0169

		public WpfTextView(ITextViewModel textViewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, IEditorOptionsFactoryService editorOptionsFactoryService, ICommandManager commandManager, ISmartIndentationService smartIndentationService, IFormattedTextSourceFactoryService formattedTextSourceFactoryService, IViewClassifierAggregatorService viewClassifierAggregatorService, ITextAndAdornmentSequencerFactoryService textAndAdornmentSequencerFactoryService, IClassificationFormatMapService classificationFormatMapService, IEditorFormatMapService editorFormatMapService, IAdornmentLayerDefinitionService adornmentLayerDefinitionService, ILineTransformProviderService lineTransformProviderService, ISpaceReservationStackProvider spaceReservationStackProvider, Lazy<IWpfTextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>[] wpfTextViewCreationListeners) {
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
			if (editorFormatMapService == null)
				throw new ArgumentNullException(nameof(editorFormatMapService));
			if (adornmentLayerDefinitionService == null)
				throw new ArgumentNullException(nameof(adornmentLayerDefinitionService));
			if (lineTransformProviderService == null)
				throw new ArgumentNullException(nameof(lineTransformProviderService));
			if (spaceReservationStackProvider == null)
				throw new ArgumentNullException(nameof(spaceReservationStackProvider));
			if (wpfTextViewCreationListeners == null)
				throw new ArgumentNullException(nameof(wpfTextViewCreationListeners));
			this.mouseHoverHelper = new MouseHoverHelper(this);
			this.physicalLineCache = new PhysicalLineCache(32);
			this.visiblePhysicalLines = new List<PhysicalLine>();
			this.invalidatedRegions = new List<SnapshotSpan>();
			this.formattedTextSourceFactoryService = formattedTextSourceFactoryService;
			this.zoomLevel = ZoomConstants.DefaultZoom;
			this.adornmentLayerDefinitionService = adornmentLayerDefinitionService;
			this.lineTransformProviderService = lineTransformProviderService;
			this.wpfTextViewCreationListeners = wpfTextViewCreationListeners.Where(a => roles.ContainsAny(a.Metadata.TextViewRoles)).ToArray();
			this.recreateLineTransformProvider = true;
			this.normalAdornmentLayerCollection = new AdornmentLayerCollection(this, LayerKind.Normal);
			this.overlayAdornmentLayerCollection = new AdornmentLayerCollection(this, LayerKind.Overlay);
			this.underlayAdornmentLayerCollection = new AdornmentLayerCollection(this, LayerKind.Underlay);
			IsVisibleChanged += WpfTextView_IsVisibleChanged;
			Properties = new PropertyCollection();
			TextViewModel = textViewModel;
			Roles = roles;
			Options = editorOptionsFactoryService.GetOptions(this);
			Options.Parent = parentOptions;
			ViewScroller = new ViewScroller(this);
			hasKeyboardFocus = this.IsKeyboardFocusWithin;
			oldViewState = new ViewState(this);
			this.aggregateClassifier = viewClassifierAggregatorService.GetClassifier(this);
			this.textAndAdornmentSequencer = textAndAdornmentSequencerFactoryService.Create(this);
			this.classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(this);
			this.editorFormatMap = editorFormatMapService.GetEditorFormatMap(this);
			this.spaceReservationStack = spaceReservationStackProvider.Create(this);

			this.textLayer = new TextLayer(GetAdornmentLayer(PredefinedAdornmentLayers.Text));
			Selection = new TextSelection(this, GetAdornmentLayer(PredefinedAdornmentLayers.Selection), editorFormatMap);
			TextCaret = new TextCaret(this, GetAdornmentLayer(PredefinedAdornmentLayers.Caret), smartIndentationService, classificationFormatMap);

			Children.Add(underlayAdornmentLayerCollection);
			Children.Add(normalAdornmentLayerCollection);
			Children.Add(overlayAdornmentLayerCollection);
			this.Cursor = Cursors.IBeam;
			this.Focusable = true;
			this.FocusVisualStyle = null;
			InitializeOptions();

			Options.OptionChanged += EditorOptions_OptionChanged;
			TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			TextViewModel.DataModel.ContentTypeChanged += DataModel_ContentTypeChanged;
			aggregateClassifier.ClassificationChanged += AggregateClassifier_ClassificationChanged;
			textAndAdornmentSequencer.SequenceChanged += TextAndAdornmentSequencer_SequenceChanged;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			spaceReservationStack.GotAggregateFocus += SpaceReservationStack_GotAggregateFocus;
			spaceReservationStack.LostAggregateFocus += SpaceReservationStack_LostAggregateFocus;

			UpdateBackground();
			CreateFormattedLineSource(ViewportWidth);
			InitializeZoom();
			UpdateRemoveExtraTextLineVerticalPixels();

			if (Roles.Contains(PredefinedTextViewRoles.Interactive))
				RegisteredCommandElement = commandManager.Register(VisualElement, this);
			else
				RegisteredCommandElement = NullRegisteredCommandElement.Instance;

			NotifyTextViewCreated(TextViewModel.DataModel.ContentType, null);
		}

		void NotifyTextViewCreated(IContentType newContentType, IContentType oldContentType) {
			foreach (var lazy in wpfTextViewCreationListeners) {
				if (oldContentType != null && oldContentType.IsOfAnyType(lazy.Metadata.ContentTypes))
					continue;
				if (!TextDataModel.ContentType.IsOfAnyType(lazy.Metadata.ContentTypes))
					continue;
				lazy.Value.TextViewCreated(this);
			}
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
			int ms = Options.GetRefreshScreenOnChangeWaitMilliSeconds();
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

		void DataModel_ContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e) {
			recreateLineTransformProvider = true;

			// Refresh all lines since IFormattedTextSourceFactoryService uses the content type to
			// pick a ITextParagraphPropertiesFactoryService
			InvalidateFormattedLineSource(true);

			NotifyTextViewCreated(e.AfterContentType, e.BeforeContentType);
		}

		void TextBuffer_ChangedLowPriority(object sender, TextContentChangedEventArgs e) {
			foreach (var c in e.Changes) {
				if (c.OldSpan.Length > 0)
					InvalidateSpan(new SnapshotSpan(e.Before, c.OldSpan));
				if (c.NewSpan.Length > 0)
					InvalidateSpan(new SnapshotSpan(e.After, c.NewSpan));
			}
			InvalidateFormattedLineSource(false);
			if (Options.IsRefreshScreenOnChangeEnabled())
				DelayScreenRefresh();
		}

		void AggregateClassifier_ClassificationChanged(object sender, ClassificationChangedEventArgs e) =>
			Dispatcher.BeginInvoke(new Action(() => InvalidateSpan(e.ChangeSpan)), DispatcherPriority.Normal);

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			Dispatcher.BeginInvoke(new Action(() => {
				if (IsClosed)
					return;
				UpdateForceClearTypeIfNeeded();
				InvalidateFormattedLineSource(true);
			}), DispatcherPriority.Normal);
		}

		void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e) {
			if (e.ChangedItems.Contains(EditorFormatMapConstants.TextViewBackgroundId))
				UpdateBackground();
		}

		void UpdateBackground() {
			var bgProps = editorFormatMap.GetProperties(EditorFormatMapConstants.TextViewBackgroundId);
			Background = ResourceDictionaryUtilities.GetBackgroundBrush(bgProps, SystemColors.WindowBrush);
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

		void DelayLayoutLinesHandler() => DoDelayDisplayLines();
		void DoDelayDisplayLines() {
			Dispatcher.VerifyAccess();
			if (IsClosed)
				return;
			if (!delayLayoutLinesInProgress)
				return;
			delayLayoutLinesInProgress = false;

			SnapshotPoint bufferPosition;
			double verticalDistance;
			if (wpfTextViewLineCollection == null) {
				verticalDistance = 0;
				bufferPosition = new SnapshotPoint(TextSnapshot, 0);
			}
			else {
				var line = wpfTextViewLineCollection.FirstVisibleLine;
				verticalDistance = line.Top - ViewportTop;
				bufferPosition = line.Start.TranslateTo(TextSnapshot, PointTrackingMode.Negative);
			}

			DisplayLines(bufferPosition, verticalDistance, ViewRelativePosition.Top, ViewportWidth, ViewportHeight, ViewportTop);
		}

		void InvalidateFormattedLineSource(bool refreshAllLines) {
			Dispatcher.VerifyAccess();
			formattedLineSourceIsInvalidated = true;
			DelayLayoutLines(refreshAllLines);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);
			if (e.Property == TextOptions.TextFormattingModeProperty || e.Property == TextOptions.TextRenderingModeProperty)
				InvalidateFormattedLineSource(true);
		}

		void EditorOptions_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			UpdateOption(e.OptionId);
			if (e.OptionId == DefaultTextViewOptions.WordWrapStyleId.Name) {
				if ((Options.WordWrapStyle() & WordWrapStyles.WordWrap) != 0)
					ViewportLeft = 0;
				InvalidateFormattedLineSource(true);
			}
			else if (e.OptionId == DefaultOptions.TabSizeOptionId.Name)
				InvalidateFormattedLineSource(true);
			else if (e.OptionId == DefaultDnSpyTextViewOptions.RefreshScreenOnChangeId.Name) {
				if (!Options.IsRefreshScreenOnChangeEnabled())
					StopRefreshTimer();
			}
			else if (e.OptionId == DefaultDnSpyTextViewOptions.EnableColorizationId.Name)
				InvalidateFormattedLineSource(true);
			else if (e.OptionId == DefaultDnSpyTextViewOptions.RemoveExtraTextLineVerticalPixelsId.Name)
				UpdateRemoveExtraTextLineVerticalPixels();
		}

		void UpdateRemoveExtraTextLineVerticalPixels() {
			bool newValue = Options.IsRemoveExtraTextLineVerticalPixelsEnabled();
			if (newValue == removeExtraTextLineVerticalPixels)
				return;
			removeExtraTextLineVerticalPixels = newValue;
			recreateLineTransformProvider = true;
			DelayLayoutLines(true);
		}

		double lastFormattedLineSourceViewportWidth = double.NaN;
		void CreateFormattedLineSource(double viewportWidthOverride) {
			lastFormattedLineSourceViewportWidth = viewportWidthOverride;
			var wordWrapStyle = Options.WordWrapStyle();
			bool isWordWrap = (wordWrapStyle & WordWrapStyles.WordWrap) != 0;
			bool isAutoIndent = isWordWrap && (wordWrapStyle & WordWrapStyles.AutoIndent) != 0;
			double wordWrapWidth = isWordWrap ? viewportWidthOverride : 0;
			var maxAutoIndent = isAutoIndent ? viewportWidthOverride / 4 : 0;
			bool useDisplayMode = TextOptions.GetTextFormattingMode(this) == TextFormattingMode.Display;
			var classifier = Options.IsColorizationEnabled() ? aggregateClassifier : NullClassifier.Instance;

			int tabSize = Options.GetTabSize();
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
				classifier,
				textAndAdornmentSequencer,
				classificationFormatMap,
				(wordWrapStyle & (WordWrapStyles.WordWrap | WordWrapStyles.VisibleGlyphs)) == (WordWrapStyles.WordWrap | WordWrapStyles.VisibleGlyphs));
		}

		protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e) {
			UpdateKeyboardFocus();
			base.OnIsKeyboardFocusWithinChanged(e);
		}

		void SpaceReservationStack_GotAggregateFocus(object sender, EventArgs e) => UpdateKeyboardFocus();
		void SpaceReservationStack_LostAggregateFocus(object sender, EventArgs e) => UpdateKeyboardFocus();

		bool hasKeyboardFocus;
		void UpdateKeyboardFocus() {
			if (IsClosed)
				return;
			bool newValue = HasAggregateFocus;
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
					if (!IsClosed)
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

				zoomLevel = newValue;
				metroWindow?.SetScaleTransform(this, zoomLevel / 100);
				if (!IsClosed)
					ZoomLevelChanged?.Invoke(this, new ZoomLevelChangedEventArgs(newValue, LayoutTransform));
			}
		}
		double zoomLevel;

		public WpfTextViewLineCollection TextViewLines {
			get {
				if (InLayout)
					throw new InvalidOperationException();
				// The adornment layer accesses this property in its LayoutChanged handler to check
				// whether an adornment intersects the visible textview lines. Don't create new
				// lines if we're raising LayoutChanged.
				if (delayLayoutLinesInProgress && !raisingLayoutChanged)
					DoDelayDisplayLines();
				return wpfTextViewLineCollection;
			}
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
				if ((Options.WordWrapStyle() & WordWrapStyles.WordWrap) != 0)
					left = 0;
				if (left < 0)
					left = 0;
				if (viewportLeft == left)
					return;
				viewportLeft = left;
				UpdateVisibleLines();
				SetLeft(normalAdornmentLayerCollection, -viewportLeft);
				RaiseLayoutChanged();
				if (!IsClosed)
					ViewportLeftChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		double viewportTop, viewportLeft;

		void RaiseLayoutChanged() => RaiseLayoutChanged(ViewportWidth, ViewportHeight, Array.Empty<ITextViewLine>(), Array.Empty<ITextViewLine>());
		void RaiseLayoutChanged(double effectiveViewportWidth, double effectiveViewportHeight, ITextViewLine[] newOrReformattedLines, ITextViewLine[] translatedLines) {
			if (IsClosed)
				return;
			Debug.Assert(!raisingLayoutChanged);
			raisingLayoutChanged = true;
			var newViewState = new ViewState(this, effectiveViewportWidth, effectiveViewportHeight);
			LayoutChanged?.Invoke(this, new TextViewLayoutChangedEventArgs(oldViewState, newViewState, newOrReformattedLines, translatedLines));
			oldViewState = newViewState;
			foreach (var p in visiblePhysicalLines) {
				foreach (var l in p.Lines) {
					l.SetChange(TextViewLineChange.None);
					l.SetDeltaY(0);
				}
			}
			Debug.Assert(raisingLayoutChanged);
			raisingLayoutChanged = false;
			mouseHoverHelper.OnLayoutChanged();
			QueueSpaceReservationStackRefresh();
		}
		ViewState oldViewState;
		bool raisingLayoutChanged;

		public void Close() {
			if (IsClosed)
				throw new InvalidOperationException();
			mouseHoverHelper.OnClosed();
			StopRefreshTimer();
			RegisteredCommandElement.Unregister();
			TextViewModel.Dispose();
			IsClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
			(aggregateClassifier as IDisposable)?.Dispose();
			TextCaret.Dispose();
			Selection.Dispose();
			(FormattedLineSource as IDisposable)?.Dispose();
			physicalLineCache.Dispose();
			textLayer.Dispose();
			foreach (var physLine in visiblePhysicalLines)
				physLine.Dispose();
			visiblePhysicalLines.Clear();
			(__lineTransformProvider as IDisposable)?.Dispose();

			Loaded -= WpfTextView_Loaded;
			Options.OptionChanged -= EditorOptions_OptionChanged;
			TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			TextViewModel.DataModel.ContentTypeChanged -= DataModel_ContentTypeChanged;
			aggregateClassifier.ClassificationChanged -= AggregateClassifier_ClassificationChanged;
			textAndAdornmentSequencer.SequenceChanged -= TextAndAdornmentSequencer_SequenceChanged;
			classificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			spaceReservationStack.GotAggregateFocus -= SpaceReservationStack_GotAggregateFocus;
			spaceReservationStack.LostAggregateFocus -= SpaceReservationStack_LostAggregateFocus;
			if (metroWindow != null)
				metroWindow.WindowDPIChanged -= MetroWindow_WindowDPIChanged;
		}

		void InitializeOptions() {
			UpdateOption(DefaultWpfViewOptions.ZoomLevelId.Name);
			UpdateOption(DefaultDnSpyWpfViewOptions.ForceClearTypeIfNeededId.Name);
		}

		void UpdateOption(string optionId) {
			if (IsClosed)
				return;
			if (optionId == DefaultWpfViewOptions.ZoomLevelId.Name) {
				if (Roles.Contains(PredefinedTextViewRoles.Zoomable))
					ZoomLevel = Options.ZoomLevel();
			}
			else if (optionId == DefaultDnSpyWpfViewOptions.ForceClearTypeIfNeededId.Name)
				UpdateForceClearTypeIfNeeded();
		}

		void UpdateForceClearTypeIfNeeded() => TextFormattingUtilities.UpdateForceClearTypeIfNeeded(this, Options, classificationFormatMap);

		bool IsVisiblePhysicalLinesSnapshot(ITextSnapshot snapshot) =>
			visiblePhysicalLines.Count != 0 && visiblePhysicalLines[0].BufferSpan.Snapshot == snapshot;
		ITextViewLine ITextView.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) => GetTextViewLineContainingBufferPosition(bufferPosition);
		public IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) {
			if (IsClosed)
				throw new InvalidOperationException();
			if (bufferPosition.Snapshot != TextSnapshot)
				throw new ArgumentException();

			if (delayLayoutLinesInProgress && !IsVisiblePhysicalLinesSnapshot(bufferPosition.Snapshot))
				DoDelayDisplayLines();

			if (IsVisiblePhysicalLinesSnapshot(bufferPosition.Snapshot)) {
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
			DisplayLines(bufferPosition, verticalDistance, relativeTo, viewportWidthOverride ?? ViewportWidth, viewportHeightOverride ?? ViewportHeight, null);

		double lastViewportWidth = double.NaN;
		void DisplayLines(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double viewportWidthOverride, double viewportHeightOverride, double? newViewportTop) {
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

			if (!(FormattedLineSource.SourceTextSnapshot == TextSnapshot && FormattedLineSource.TopTextSnapshot == VisualSnapshot))
				invalidateAllLines = true;
			if (invalidateAllLines || formattedLineSourceIsInvalidated) {
				CreateFormattedLineSource(viewportWidthOverride);
				formattedLineSourceIsInvalidated = false;
			}
			Debug.Assert(FormattedLineSource.SourceTextSnapshot == TextSnapshot && FormattedLineSource.TopTextSnapshot == VisualSnapshot);

			var lineTransformProvider = LineTransformProvider;

			if (InLayout)
				throw new InvalidOperationException();
			InLayout = true;
			var oldVisibleLines = new HashSet<ITextViewLine>(wpfTextViewLineCollection == null ? (IList<ITextViewLine>)Array.Empty<ITextViewLine>() : wpfTextViewLineCollection);
			wpfTextViewLineCollection?.Invalidate();

			var layoutHelper = new LayoutHelper(lineTransformProvider, newViewportTop ?? 0, oldVisibleLines, GetValidCachedLines(regionsToInvalidate), FormattedLineSource, TextViewModel, VisualSnapshot, TextSnapshot);
			layoutHelper.LayoutLines(bufferPosition, relativeTo, verticalDistance, ViewportLeft, viewportWidthOverride, viewportHeightOverride);

			visiblePhysicalLines.AddRange(layoutHelper.AllVisiblePhysicalLines);
			wpfTextViewLineCollection = new WpfTextViewLineCollection(this, TextSnapshot, layoutHelper.AllVisibleLines);

			if (!InLayout)
				throw new InvalidOperationException();
			InLayout = false;

			textLayer.AddVisibleLines(layoutHelper.AllVisibleLines);
			var newOrReformattedLines = layoutHelper.NewOrReformattedLines.ToArray();
			var translatedLines = layoutHelper.TranslatedLines.ToArray();

			if (layoutHelper.NewViewportTop != viewportTop) {
				viewportTop = layoutHelper.NewViewportTop;
				SetTop(normalAdornmentLayerCollection, -viewportTop);
			}
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

			var info = adornmentLayerDefinitionService.GetLayerDefinition(name);
			if (info == null)
				throw new ArgumentException($"Adornment layer {name} doesn't exist");

			switch (GetLayerKind(info.Value.Metadata)) {
			case LayerKind.Normal:
				return normalAdornmentLayerCollection.GetAdornmentLayer(info.Value);

			case LayerKind.Overlay:
				return overlayAdornmentLayerCollection.GetAdornmentLayer(info.Value);

			case LayerKind.Underlay:
				return underlayAdornmentLayerCollection.GetAdornmentLayer(info.Value);

			default:
				Debug.Fail($"Invalid {nameof(LayerKind)} value: {info.Value.Metadata.LayerKind}");
				goto case LayerKind.Normal;
			}
		}

		static LayerKind GetLayerKind(IAdornmentLayersMetadata md) {
			if (md.IsOverlayLayer) {
				Debug.Assert(md.LayerKind == LayerKind.Normal, $"Use only one of {nameof(IsOverlayLayerAttribute)} and {nameof(LayerKindAttribute)}");
				return LayerKind.Overlay;
			}
			return md.LayerKind;
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
			if (!IsClosed) {
				if (sizeInfo.PreviousSize.Height != sizeInfo.NewSize.Height)
					ViewportHeightChanged?.Invoke(this, EventArgs.Empty);
				if (sizeInfo.PreviousSize.Width != sizeInfo.NewSize.Width) {
					ViewportWidthChanged?.Invoke(this, EventArgs.Empty);
					if ((Options.WordWrapStyle() & WordWrapStyles.WordWrap) != 0)
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
			if (wpfTextViewLineCollection == null)
				return;
			foreach (IFormattedLine line in wpfTextViewLineCollection)
				line.SetVisibleArea(new Rect(ViewportLeft, ViewportTop, viewportWidthOverride, ViewportHeightOverride));
		}

		void InitializeZoom() {
			var window = Window.GetWindow(this);
			metroWindow = window as MetroWindow;
			if (window != null && metroWindow == null)
				return;
			if (metroWindow != null) {
				metroWindow.WindowDPIChanged += MetroWindow_WindowDPIChanged;
				MetroWindow_WindowDPIChanged(metroWindow, EventArgs.Empty);
				ZoomLevelChanged?.Invoke(this, new ZoomLevelChangedEventArgs(ZoomLevel, LayoutTransform));
				return;
			}

			this.Loaded += WpfTextView_Loaded;
		}
		MetroWindow metroWindow;

		void WpfTextView_Loaded(object sender, RoutedEventArgs e) {
			this.Loaded -= WpfTextView_Loaded;
			var window = Window.GetWindow(this);
			metroWindow = window as MetroWindow;
			Debug.Assert(window != null);
			if (metroWindow != null) {
				metroWindow.WindowDPIChanged += MetroWindow_WindowDPIChanged;
				MetroWindow_WindowDPIChanged(metroWindow, EventArgs.Empty);
				ZoomLevelChanged?.Invoke(this, new ZoomLevelChangedEventArgs(ZoomLevel, LayoutTransform));
				return;
			}
		}

		void MetroWindow_WindowDPIChanged(object sender, EventArgs e) {
			Debug.Assert(sender != null && sender == metroWindow);
			((MetroWindow)sender).SetScaleTransform(this, ZoomLevel / 100);
		}

		ILineTransformProvider LineTransformProvider {
			get {
				if (recreateLineTransformProvider) {
					__lineTransformProvider = lineTransformProviderService.Create(this, removeExtraTextLineVerticalPixels);
					recreateLineTransformProvider = false;
				}
				return __lineTransformProvider;
			}
		}
		ILineTransformProvider __lineTransformProvider;
		bool recreateLineTransformProvider;
		bool removeExtraTextLineVerticalPixels;

		public ILineTransformSource LineTransformSource => this;
		LineTransform ILineTransformSource.GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement) =>
			LineTransformProvider.GetLineTransform(line, yPosition, placement);

		public event EventHandler<MouseHoverEventArgs> MouseHover {
			add { mouseHoverHelper.MouseHover += value; }
			remove { mouseHoverHelper.MouseHover -= value; }
		}
		readonly MouseHoverHelper mouseHoverHelper;

		public ISpaceReservationManager GetSpaceReservationManager(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			return spaceReservationStack.GetSpaceReservationManager(name);
		}

		public void QueueSpaceReservationStackRefresh() {
			if (IsClosed)
				return;
			if (queueSpaceReservationStackRefreshInProgress)
				return;
			queueSpaceReservationStackRefreshInProgress = true;
			Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (IsClosed)
					return;
				queueSpaceReservationStackRefreshInProgress = false;
				spaceReservationStack.Refresh();
			}));
		}
		bool queueSpaceReservationStackRefreshInProgress;

		void WpfTextView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) =>
			QueueSpaceReservationStackRefresh();

		public bool IsMouseOverOverlayLayerElement(MouseEventArgs e) => overlayAdornmentLayerCollection.IsMouseOverOverlayLayerElement(e);
	}
}
