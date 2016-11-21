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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Images;
using dnSpy.Hex.Formatting;
using dnSpy.Hex.MEF;
using CTC = dnSpy.Contracts.Text.Classification;
using TE = dnSpy.Text.Editor;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	sealed partial class WpfHexViewImpl : WpfHexView {
		sealed class HexViewCanvas : Canvas {
			readonly WpfHexViewImpl owner;
			public HexViewCanvas(WpfHexViewImpl owner) {
				this.owner = owner;
			}
			protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
				base.OnPropertyChanged(e);
				owner.OnPropertyChanged(e);
			}
			protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e) {
				owner.OnIsKeyboardFocusWithinChanged(e);
				base.OnIsKeyboardFocusWithinChanged(e);
			}
			protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
				owner.OnRenderSizeChanged(sizeInfo);
				base.OnRenderSizeChanged(sizeInfo);
			}
		}

		public override FrameworkElement VisualElement => canvas;
		public override VSTE.ITextViewRoleSet Roles { get; }
		public override VSTE.IEditorOptions Options { get; }
		public override ICommandTargetCollection CommandTarget => RegisteredCommandElement.CommandTarget;
		IRegisteredCommandElement RegisteredCommandElement { get; }
		public override HexCaret Caret => HexCaret;
		HexCaretImpl HexCaret { get; }
		public override HexSelection Selection => HexSelection;
		HexSelectionImpl HexSelection { get; }
		public override HexViewScroller ViewScroller { get; }
		public override bool HasAggregateFocus => canvas.IsKeyboardFocusWithin || spaceReservationStack.HasAggregateFocus;
		public override bool IsMouseOverViewOrAdornments => canvas.IsMouseOver || spaceReservationStack.IsMouseOver;
		public override HexBuffer Buffer { get; }
		public override bool IsClosed => isClosed;
		public override HexBufferSpan? ProvisionalTextHighlight { get; set; }//TODO:
		public override event EventHandler GotAggregateFocus;
		public override event EventHandler LostAggregateFocus;
		public override event EventHandler Closed;
		public override event EventHandler<VSTE.BackgroundBrushChangedEventArgs> BackgroundBrushChanged;
		public override event EventHandler ViewportLeftChanged;
		public override event EventHandler ViewportHeightChanged;
		public override event EventHandler ViewportWidthChanged;
		public override event EventHandler<HexViewLayoutChangedEventArgs> LayoutChanged;
		public override event EventHandler<VSTE.ZoomLevelChangedEventArgs> ZoomLevelChanged;
		public override HexFormattedLineSource FormattedLineSource => formattedLineSource;
		HexFormattedLineSource formattedLineSource;
		public override bool InLayout => inLayout;
		bool inLayout;
		public override HexViewLineCollection HexViewLines => WpfHexViewLines;
		public override WpfHexViewLineCollection WpfHexViewLines => AllHexViewLines;

		public override double MaxTextRightCoordinate {
			get {
				double max = 0;
				foreach (var p in visiblePhysicalLines) {
					foreach (var l in p.Lines)
						max = Math.Max(max, l.Right);
				}
				return max;
			}
		}

		readonly Canvas canvas;
		readonly FormattedHexSourceFactoryService formattedHexSourceFactoryService;
		readonly HexClassifier aggregateClassifier;
		readonly HexAndAdornmentSequencer hexAndAdornmentSequencer;
		readonly HexBufferLineProviderFactoryService bufferLineProviderFactoryService;
		readonly VSTC.IClassificationFormatMap classificationFormatMap;
		readonly VSTC.IEditorFormatMap editorFormatMap;
		readonly HexSpaceReservationStack spaceReservationStack;
		readonly HexAdornmentLayerDefinitionService adornmentLayerDefinitionService;
		readonly HexLineTransformProviderService lineTransformProviderService;
		readonly Lazy<WpfHexViewCreationListener, IDeferrableTextViewRoleMetadata>[] wpfHexViewCreationListeners;
		readonly HexAdornmentLayerCollection normalAdornmentLayerCollection;
		readonly HexAdornmentLayerCollection overlayAdornmentLayerCollection;
		readonly HexAdornmentLayerCollection underlayAdornmentLayerCollection;
		readonly PhysicalLineCache physicalLineCache;
		readonly List<PhysicalLine> visiblePhysicalLines;
		readonly TextLayer textLayer;

#pragma warning disable 0169
		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.Text)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.BottomLayer, Before = PredefinedHexAdornmentLayers.TopLayer)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.Selection, Before = PredefinedHexAdornmentLayers.Caret)]
		static readonly HexAdornmentLayerDefinition textAdornmentLayerDefinition;

		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.Caret)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.BottomLayer, Before = PredefinedHexAdornmentLayers.TopLayer)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.Text)]
		static readonly HexAdornmentLayerDefinition caretAdornmentLayerDefinition;

		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.Selection)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.BottomLayer, Before = PredefinedHexAdornmentLayers.TopLayer)]
		[VSUTIL.Order(Before = PredefinedHexAdornmentLayers.Text)]
		static readonly HexAdornmentLayerDefinition selectionAdornmentLayerDefinition;
#pragma warning restore 0169

		public WpfHexViewImpl(HexBuffer buffer, VSTE.ITextViewRoleSet roles, VSTE.IEditorOptions parentOptions, HexEditorOptionsFactoryService hexEditorOptionsFactoryService, ICommandService commandService, FormattedHexSourceFactoryService formattedHexSourceFactoryService, HexViewClassifierAggregatorService hexViewClassifierAggregatorService, HexAndAdornmentSequencerFactoryService hexAndAdornmentSequencerFactoryService, HexBufferLineProviderFactoryService bufferLineProviderFactoryService, HexClassificationFormatMapService classificationFormatMapService, HexEditorFormatMapService editorFormatMapService, HexAdornmentLayerDefinitionService adornmentLayerDefinitionService, HexLineTransformProviderService lineTransformProviderService, HexSpaceReservationStackProvider spaceReservationStackProvider, Lazy<WpfHexViewCreationListener, IDeferrableTextViewRoleMetadata>[] wpfHexViewCreationListeners, VSTC.IClassificationTypeRegistryService classificationTypeRegistryService) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			if (hexEditorOptionsFactoryService == null)
				throw new ArgumentNullException(nameof(hexEditorOptionsFactoryService));
			if (commandService == null)
				throw new ArgumentNullException(nameof(commandService));
			if (formattedHexSourceFactoryService == null)
				throw new ArgumentNullException(nameof(formattedHexSourceFactoryService));
			if (hexViewClassifierAggregatorService == null)
				throw new ArgumentNullException(nameof(hexViewClassifierAggregatorService));
			if (hexAndAdornmentSequencerFactoryService == null)
				throw new ArgumentNullException(nameof(hexAndAdornmentSequencerFactoryService));
			if (bufferLineProviderFactoryService == null)
				throw new ArgumentNullException(nameof(bufferLineProviderFactoryService));
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
			if (wpfHexViewCreationListeners == null)
				throw new ArgumentNullException(nameof(wpfHexViewCreationListeners));
			if (classificationTypeRegistryService == null)
				throw new ArgumentNullException(nameof(classificationTypeRegistryService));
			canvas = new HexViewCanvas(this);
			Buffer = buffer;
			thisHexLineTransformSource = new MyHexLineTransformSource(this);
			this.bufferLineProviderFactoryService = bufferLineProviderFactoryService;
			mouseHoverHelper = new MouseHoverHelper(this);
			physicalLineCache = new PhysicalLineCache(32);
			visiblePhysicalLines = new List<PhysicalLine>();
			invalidatedRegions = new List<HexBufferSpan>();
			this.formattedHexSourceFactoryService = formattedHexSourceFactoryService;
			zoomLevel = VSTE.ZoomConstants.DefaultZoom;
			DsImage.SetZoom(VisualElement, zoomLevel / 100);
			this.adornmentLayerDefinitionService = adornmentLayerDefinitionService;
			this.lineTransformProviderService = lineTransformProviderService;
			this.wpfHexViewCreationListeners = wpfHexViewCreationListeners.Where(a => roles.ContainsAny(a.Metadata.TextViewRoles)).ToArray();
			recreateLineTransformProvider = true;
			normalAdornmentLayerCollection = new HexAdornmentLayerCollection(this, HexLayerKind.Normal);
			overlayAdornmentLayerCollection = new HexAdornmentLayerCollection(this, HexLayerKind.Overlay);
			underlayAdornmentLayerCollection = new HexAdornmentLayerCollection(this, HexLayerKind.Underlay);
			canvas.IsVisibleChanged += WpfHexView_IsVisibleChanged;
			Roles = roles;
			Options = hexEditorOptionsFactoryService.GetOptions(this);
			Options.Parent = parentOptions;
			ViewScroller = new HexViewScrollerImpl(this);
			hasKeyboardFocus = canvas.IsKeyboardFocusWithin;
			oldViewState = new HexViewState(this);
			aggregateClassifier = hexViewClassifierAggregatorService.GetClassifier(this);
			hexAndAdornmentSequencer = hexAndAdornmentSequencerFactoryService.Create(this);
			classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(this);
			editorFormatMap = editorFormatMapService.GetEditorFormatMap(this);
			spaceReservationStack = spaceReservationStackProvider.Create(this);

			textLayer = new TextLayer(GetAdornmentLayer(PredefinedHexAdornmentLayers.Text));
			HexSelection = new HexSelectionImpl(this, GetAdornmentLayer(PredefinedHexAdornmentLayers.Selection), editorFormatMap);
			HexCaret = new HexCaretImpl(this, GetAdornmentLayer(PredefinedHexAdornmentLayers.Caret), classificationFormatMap, classificationTypeRegistryService);

			canvas.Children.Add(underlayAdornmentLayerCollection);
			canvas.Children.Add(normalAdornmentLayerCollection);
			canvas.Children.Add(overlayAdornmentLayerCollection);
			canvas.Cursor = Cursors.IBeam;
			canvas.Focusable = true;
			canvas.FocusVisualStyle = null;
			InitializeOptions();

			Options.OptionChanged += EditorOptions_OptionChanged;
			Buffer.ChangedLowPriority += HexBuffer_ChangedLowPriority;
			aggregateClassifier.ClassificationChanged += AggregateClassifier_ClassificationChanged;
			hexAndAdornmentSequencer.SequenceChanged += HexAndAdornmentSequencer_SequenceChanged;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
			spaceReservationStack.GotAggregateFocus += SpaceReservationStack_GotAggregateFocus;
			spaceReservationStack.LostAggregateFocus += SpaceReservationStack_LostAggregateFocus;

			UpdateBackground();
			CreateFormattedLineSource(ViewportWidth);
			HexCaret.Initialize();
			InitializeZoom();
			UpdateRemoveExtraTextLineVerticalPixels();

			if (Roles.Contains(PredefinedHexViewRoles.Interactive))
				RegisteredCommandElement = commandService.Register(VisualElement, this);
			else
				RegisteredCommandElement = TE.NullRegisteredCommandElement.Instance;

			NotifyHexViewCreated();
		}

		void NotifyHexViewCreated() {
			foreach (var lz in wpfHexViewCreationListeners)
				lz.Value.HexViewCreated(this);
		}

		void DelayScreenRefresh() {
			if (IsClosed)
				return;
			if (screenRefreshTimer != null)
				return;
			int ms = Options.GetRefreshScreenOnChangeWaitMilliSeconds();
			if (ms > 0)
				screenRefreshTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(ms), DispatcherPriority.Normal, RefreshScreenHandler, canvas.Dispatcher);
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

		void HexBuffer_ChangedLowPriority(object sender, HexContentChangedEventArgs e) {
			foreach (var c in e.Changes) {
				if (c.OldSpan.Length > 0)
					InvalidateSpan(new HexBufferSpan(Buffer, c.OldSpan));
				if (c.NewSpan.Length > 0)
					InvalidateSpan(new HexBufferSpan(Buffer, c.NewSpan));
			}
			InvalidateFormattedLineSource(false);
			if (Options.IsRefreshScreenOnChangeEnabled())
				DelayScreenRefresh();
		}

		void AggregateClassifier_ClassificationChanged(object sender, HexClassificationChangedEventArgs e) =>
			canvas.Dispatcher.BeginInvoke(new Action(() => InvalidateSpan(e.ChangeSpan)), DispatcherPriority.Normal);

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			canvas.Dispatcher.BeginInvoke(new Action(() => {
				if (IsClosed)
					return;
				UpdateForceClearTypeIfNeeded();
				InvalidateFormattedLineSource(true);
			}), DispatcherPriority.Normal);
		}

		void EditorFormatMap_FormatMappingChanged(object sender, VSTC.FormatItemsEventArgs e) {
			if (e.ChangedItems.Contains(CTC.EditorFormatMapConstants.TextViewBackgroundId))
				UpdateBackground();
		}

		void UpdateBackground() {
			var bgProps = editorFormatMap.GetProperties(CTC.EditorFormatMapConstants.TextViewBackgroundId);
			canvas.Background = TE.ResourceDictionaryUtilities.GetBackgroundBrush(bgProps, SystemColors.WindowBrush);
		}

		void HexAndAdornmentSequencer_SequenceChanged(object sender, HexAndAdornmentSequenceChangedEventArgs e) =>
			canvas.Dispatcher.BeginInvoke(new Action(() => InvalidateSpan(e.Span)), DispatcherPriority.Normal);

		void InvalidateSpans(IEnumerable<HexBufferSpan> spans) {
			canvas.Dispatcher.VerifyAccess();
			int count = invalidatedRegions.Count;
			invalidatedRegions.AddRange(spans);
			if (invalidatedRegions.Count != count)
				DelayLayoutLines();
		}

		void InvalidateSpan(HexBufferSpan span) {
			canvas.Dispatcher.VerifyAccess();
			invalidatedRegions.Add(span);
			DelayLayoutLines();
		}

		void DelayLayoutLines(bool refreshAllLines = false) {
			canvas.Dispatcher.VerifyAccess();
			if (IsClosed)
				return;
			if (refreshAllLines) {
				invalidatedRegions.Clear();
				invalidatedRegions.Add(new HexBufferSpan(new HexBufferPoint(Buffer, 0), new HexBufferPoint(Buffer, HexPosition.MaxEndPosition)));
			}
			if (delayLayoutLinesInProgress)
				return;
			delayLayoutLinesInProgress = true;
			canvas.Dispatcher.BeginInvoke(new Action(DelayLayoutLinesHandler), DispatcherPriority.DataBind);
		}
		bool delayLayoutLinesInProgress;
		readonly List<HexBufferSpan> invalidatedRegions;
		bool formattedLineSourceIsInvalidated;

		void DelayLayoutLinesHandler() => DoDelayDisplayLines();
		void DoDelayDisplayLines() {
			canvas.Dispatcher.VerifyAccess();
			if (IsClosed)
				return;
			if (!delayLayoutLinesInProgress)
				return;
			delayLayoutLinesInProgress = false;

			HexBufferPoint bufferPosition;
			double verticalDistance;
			if (wpfHexViewLineCollection == null) {
				verticalDistance = 0;
				bufferPosition = new HexBufferPoint(Buffer, 0);
			}
			else {
				var line = wpfHexViewLineCollection.FirstVisibleLine;
				verticalDistance = line.Top - ViewportTop;
				bufferPosition = line.BufferSpan.Start;
			}

			DisplayLines(bufferPosition, verticalDistance, VSTE.ViewRelativePosition.Top, ViewportWidth, ViewportHeight, ViewportTop);
		}

		void InvalidateFormattedLineSource(bool refreshAllLines) {
			canvas.Dispatcher.VerifyAccess();
			formattedLineSourceIsInvalidated = true;
			recreateHexBufferLineProvider = true;
			DelayLayoutLines(refreshAllLines);
		}

		void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			if (e.Property == TextOptions.TextFormattingModeProperty || e.Property == TextOptions.TextRenderingModeProperty)
				InvalidateFormattedLineSource(true);
		}

		void EditorOptions_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			UpdateOption(e.OptionId);
			if (e.OptionId == DefaultHexViewOptions.RefreshScreenOnChangeName) {
				if (!Options.IsRefreshScreenOnChangeEnabled())
					StopRefreshTimer();
			}
			else if (e.OptionId == DefaultHexViewOptions.EnableColorizationName)
				InvalidateFormattedLineSource(true);
			else if (e.OptionId == DefaultHexViewOptions.RemoveExtraTextLineVerticalPixelsName)
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
			bool useDisplayMode = TextOptions.GetTextFormattingMode(canvas) == TextFormattingMode.Display;
			var classifier = Options.IsColorizationEnabled() ? aggregateClassifier : NullHexClassifier.Instance;

			// This value is what VS uses, see: https://msdn.microsoft.com/en-us/library/microsoft.visualstudio.text.formatting.iformattedlinesource.baseindentation.aspx
			//	"This is generally a small value like 2.0, so that some characters (such as an italic
			//	 slash) will not be clipped by the left edge of the view."
			const double baseIndent = 2.0;
			(formattedLineSource as IDisposable)?.Dispose();
			formattedLineSource = formattedHexSourceFactoryService.Create(
				baseIndent,
				useDisplayMode,
				classifier,
				hexAndAdornmentSequencer,
				classificationFormatMap);
		}

		void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e) => UpdateKeyboardFocus();
		void SpaceReservationStack_GotAggregateFocus(object sender, EventArgs e) => UpdateKeyboardFocus();
		void SpaceReservationStack_LostAggregateFocus(object sender, EventArgs e) => UpdateKeyboardFocus();

		bool hasKeyboardFocus;
		bool updateKeyboardFocusInProgress;
		void UpdateKeyboardFocus() {
			if (IsClosed)
				return;
			if (updateKeyboardFocusInProgress)
				return;
			updateKeyboardFocusInProgress = true;
			// Needs to be delayed or HasAggregateFocus could become false when one of the
			// space reservation agents gets the focus. Eg. we lose focus, then the agent
			// gets focus.
			canvas.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (IsClosed)
					return;
				updateKeyboardFocusInProgress = false;
				bool newValue = HasAggregateFocus;
				if (hasKeyboardFocus != newValue) {
					hasKeyboardFocus = newValue;
					if (hasKeyboardFocus)
						GotAggregateFocus?.Invoke(this, EventArgs.Empty);
					else
						LostAggregateFocus?.Invoke(this, EventArgs.Empty);
				}
			}));
		}

		public override Brush Background {
			get { return canvas.Background; }
			set {
				if (canvas.Background != value) {
					canvas.Background = value;
					if (!IsClosed)
						BackgroundBrushChanged?.Invoke(this, new VSTE.BackgroundBrushChangedEventArgs(value));
				}
			}
		}

		public override double ZoomLevel {
			get { return zoomLevel; }
			set {
				if (IsClosed)
					return;

				double newValue = value;
				newValue = Math.Min(VSTE.ZoomConstants.MaxZoom, newValue);
				newValue = Math.Max(VSTE.ZoomConstants.MinZoom, newValue);
				if (double.IsNaN(newValue) || Math.Abs(newValue - VSTE.ZoomConstants.DefaultZoom) < 0.01)
					newValue = VSTE.ZoomConstants.DefaultZoom;
				if (newValue == zoomLevel)
					return;

				zoomLevel = newValue;

				metroWindow?.SetScaleTransform(canvas, zoomLevel / 100);
				ZoomLevelChanged?.Invoke(this, new VSTE.ZoomLevelChangedEventArgs(newValue, canvas.LayoutTransform));
				DsImage.SetZoom(VisualElement, zoomLevel / 100);
			}
		}
		double zoomLevel;

		WpfHexViewLineCollectionImpl AllHexViewLines {
			get {
				if (InLayout)
					throw new InvalidOperationException();
				// The adornment layer accesses this property in its LayoutChanged handler to check
				// whether an adornment intersects with the visible hexview lines. Don't create new
				// lines if we're raising LayoutChanged.
				if (delayLayoutLinesInProgress && !raisingLayoutChanged)
					DoDelayDisplayLines();
				return wpfHexViewLineCollection;
			}
		}
		WpfHexViewLineCollectionImpl wpfHexViewLineCollection;

		public override double LineHeight => FormattedLineSource.LineHeight;
		public override double ViewportTop => viewportTop;
		public override double ViewportBottom => ViewportTop + ViewportHeight;
		public override double ViewportRight => ViewportLeft + ViewportWidth;
		public override double ViewportWidth => canvas.ActualWidth;
		public override double ViewportHeight => canvas.ActualHeight;
		public override double ViewportLeft {
			get { return viewportLeft; }
			set {
				if (double.IsNaN(value))
					throw new ArgumentOutOfRangeException(nameof(value));
				double left = value;
				if (left < 0)
					left = 0;
				if (viewportLeft == left)
					return;
				viewportLeft = left;
				UpdateVisibleLines();
				Canvas.SetLeft(normalAdornmentLayerCollection, -viewportLeft);
				RaiseLayoutChanged();
				if (!IsClosed)
					ViewportLeftChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		double viewportTop, viewportLeft;

		void RaiseLayoutChanged() => RaiseLayoutChanged(ViewportWidth, ViewportHeight, Array.Empty<HexViewLine>(), Array.Empty<HexViewLine>());
		void RaiseLayoutChanged(double effectiveViewportWidth, double effectiveViewportHeight, HexViewLine[] newOrReformattedLines, HexViewLine[] translatedLines) {
			if (IsClosed)
				return;
			Debug.Assert(!raisingLayoutChanged);
			raisingLayoutChanged = true;
			var newViewState = new HexViewState(this, effectiveViewportWidth, effectiveViewportHeight);
			var layoutChangedEventArgs = new HexViewLayoutChangedEventArgs(oldViewState, newViewState, newOrReformattedLines, translatedLines);
			LayoutChanged?.Invoke(this, layoutChangedEventArgs);
			oldViewState = newViewState;
			foreach (var p in visiblePhysicalLines) {
				foreach (var l in p.Lines) {
					l.SetChange(VSTF.TextViewLineChange.None);
					l.SetDeltaY(0);
				}
			}
			Debug.Assert(raisingLayoutChanged);
			raisingLayoutChanged = false;
			mouseHoverHelper.OnLayoutChanged();
			if (ShouldQueueSpaceReservationStackRefresh(layoutChangedEventArgs))
				QueueSpaceReservationStackRefresh();
		}
		HexViewState oldViewState;
		bool raisingLayoutChanged;

		bool ShouldQueueSpaceReservationStackRefresh(HexViewLayoutChangedEventArgs e) {
			// These checks are necessary or the completion listbox will flicker. Try pressing
			// eg. F followed by backspace quickly and you'll be able to see the code in the
			// background (because the completion listbox gets hidden and then shown).
			if (e.OldViewState.ViewportLeft != e.NewViewState.ViewportLeft)
				return true;
			if (e.OldViewState.ViewportTop != e.NewViewState.ViewportTop)
				return true;
			if (e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth)
				return true;
			if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				return true;
			return false;
		}

		bool isClosed;
		public override void Close() {
			if (IsClosed)
				throw new InvalidOperationException();
			mouseHoverHelper.OnClosed();
			StopRefreshTimer();
			RegisteredCommandElement.Unregister();
			isClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
			(aggregateClassifier as IDisposable)?.Dispose();
			HexCaret.Dispose();
			HexSelection.Dispose();
			(FormattedLineSource as IDisposable)?.Dispose();
			physicalLineCache.Dispose();
			textLayer.Dispose();
			foreach (var physLine in visiblePhysicalLines)
				physLine.Dispose();
			visiblePhysicalLines.Clear();
			(__lineTransformProvider as IDisposable)?.Dispose();

			canvas.Loaded -= WpfHexView_Loaded;
			Options.OptionChanged -= EditorOptions_OptionChanged;
			Buffer.ChangedLowPriority -= HexBuffer_ChangedLowPriority;
			aggregateClassifier.ClassificationChanged -= AggregateClassifier_ClassificationChanged;
			hexAndAdornmentSequencer.SequenceChanged -= HexAndAdornmentSequencer_SequenceChanged;
			classificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
			spaceReservationStack.GotAggregateFocus -= SpaceReservationStack_GotAggregateFocus;
			spaceReservationStack.LostAggregateFocus -= SpaceReservationStack_LostAggregateFocus;
			if (metroWindow != null)
				metroWindow.WindowDpiChanged -= MetroWindow_WindowDpiChanged;
		}

		void InitializeOptions() {
			UpdateOption(DefaultWpfHexViewOptions.ZoomLevelName);
			UpdateOption(DefaultWpfHexViewOptions.ForceClearTypeIfNeededName);
		}

		void UpdateOption(string optionId) {
			if (IsClosed)
				return;
			switch (optionId) {
			case DefaultWpfHexViewOptions.ZoomLevelName:
				if (Roles.Contains(PredefinedHexViewRoles.Zoomable))
					ZoomLevel = Options.ZoomLevel();
				break;

			case DefaultWpfHexViewOptions.ForceClearTypeIfNeededName:
				UpdateForceClearTypeIfNeeded();
				break;

			case DefaultHexViewOptions.BasePositionName:
			case DefaultHexViewOptions.BytesPerLineName:
			case DefaultHexViewOptions.GroupSizeInBytesName:
			case DefaultHexViewOptions.EndPositionName:
			case DefaultHexViewOptions.HexOffsetFormatName:
			case DefaultHexViewOptions.HexValuesDisplayFormatName:
			case DefaultHexViewOptions.OffsetBitSizeName:
			case DefaultHexViewOptions.OffsetLowerCaseHexName:
			case DefaultHexViewOptions.ShowAsciiColumnName:
			case DefaultHexViewOptions.ShowOffsetColumnName:
			case DefaultHexViewOptions.ShowValuesColumnName:
			case DefaultHexViewOptions.StartPositionName:
			case DefaultHexViewOptions.UseRelativePositionsName:
			case DefaultHexViewOptions.ValuesLowerCaseHexName:
				InvalidateHexBufferLineProvider();
				break;
			}
		}

		void UpdateForceClearTypeIfNeeded() => TE.TextFormattingUtilities.UpdateForceClearTypeIfNeeded(canvas, Options.IsForceClearTypeIfNeededEnabled(), classificationFormatMap);

		public override HexViewLine GetHexViewLineContainingBufferPosition(HexBufferPoint bufferPosition) => GetWpfHexViewLineContainingBufferPosition(bufferPosition);
		public override WpfHexViewLine GetWpfHexViewLineContainingBufferPosition(HexBufferPoint bufferPosition) {
			if (IsClosed)
				throw new InvalidOperationException();
			if (bufferPosition.Buffer != Buffer)
				throw new ArgumentException();

			foreach (var pline in visiblePhysicalLines) {
				var lline = pline.FindFormattedLineByBufferPosition(bufferPosition);
				if (lline != null)
					return lline;
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

		PhysicalLine CreatePhysicalLineNoCache(HexBufferPoint bufferPosition, double viewportWidthOverride) {
			if (bufferPosition.Buffer != Buffer)
				throw new ArgumentException();
			if (formattedLineSourceIsInvalidated)
				CreateFormattedLineSource(viewportWidthOverride);
			if (recreateHexBufferLineProvider) {
				recreateHexBufferLineProvider = false;
				var oldBufferLines = hexBufferLineProvider;
				hexBufferLineProvider = null;
				RaiseBufferLinesChanged(oldBufferLines);
			}
			return CreatePhysicalLineNoCache(BufferLines, FormattedLineSource, bufferPosition);
		}

		static PhysicalLine CreatePhysicalLineNoCache(HexBufferLineProvider bufferLines, HexFormattedLineSource formattedLineSource, HexBufferPoint bufferPosition) {
			var bufferLine = bufferLines.GetLineFromPosition(bufferPosition);
			var formattedLine = formattedLineSource.FormatLineInVisualBuffer(bufferLine);
			return new PhysicalLine(new[] { formattedLine });
		}

		public override void DisplayHexLineContainingBufferPosition(HexBufferPoint bufferPosition, double verticalDistance, VSTE.ViewRelativePosition relativeTo) =>
			DisplayHexLineContainingBufferPosition(bufferPosition, verticalDistance, relativeTo, null, null);
		public override void DisplayHexLineContainingBufferPosition(HexBufferPoint bufferPosition, double verticalDistance, VSTE.ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride) =>
			DisplayLines(bufferPosition, verticalDistance, relativeTo, viewportWidthOverride ?? ViewportWidth, viewportHeightOverride ?? ViewportHeight, null);

		double lastViewportWidth = double.NaN;
		void DisplayLines(HexBufferPoint bufferPosition, double verticalDistance, VSTE.ViewRelativePosition relativeTo, double viewportWidthOverride, double viewportHeightOverride, double? newViewportTop) {
			if (IsClosed)
				throw new InvalidOperationException();
			var oldBufferLines = hexBufferLineProvider;
			bool raiseBufferLinesChangedEvent = false;

			canvas.Dispatcher.VerifyAccess();
			if (bufferPosition.Buffer != Buffer)
				throw new ArgumentException();
			if (relativeTo != VSTE.ViewRelativePosition.Top && relativeTo != VSTE.ViewRelativePosition.Bottom)
				throw new ArgumentOutOfRangeException(nameof(relativeTo));
			if (viewportHeightOverride < 0 || double.IsNaN(viewportHeightOverride))
				throw new ArgumentOutOfRangeException(nameof(viewportHeightOverride));
			if (viewportWidthOverride < 0 || double.IsNaN(viewportWidthOverride))
				throw new ArgumentOutOfRangeException(nameof(viewportWidthOverride));

			bool invalidateAllLines = false;
			if (recreateHexBufferLineProvider)
				invalidateAllLines = true;
			if (viewportWidthOverride != lastViewportWidth || viewportWidthOverride != lastFormattedLineSourceViewportWidth) {
				invalidateAllLines = true;
				lastViewportWidth = viewportWidthOverride;
			}

			// Make sure the scheduled method doesn't try to call this method
			delayLayoutLinesInProgress = false;

			if (invalidateAllLines) {
				invalidatedRegions.Clear();
				invalidatedRegions.Add(new HexBufferSpan(new HexBufferPoint(Buffer, 0), new HexBufferPoint(Buffer, HexPosition.MaxEndPosition)));
			}
			var regionsToInvalidate = new NormalizedHexBufferSpanCollection(invalidatedRegions);
			invalidatedRegions.Clear();
			if (invalidatedRegions.Capacity > 100)
				invalidatedRegions.TrimExcess();

			if (invalidateAllLines || formattedLineSourceIsInvalidated) {
				CreateFormattedLineSource(viewportWidthOverride);
				formattedLineSourceIsInvalidated = false;
				recreateHexBufferLineProvider = true;
			}

			// This one depends on FormattedLineSource and must be created afterwards
			if (recreateHexBufferLineProvider) {
				recreateHexBufferLineProvider = false;
				raiseBufferLinesChangedEvent = true;
				hexBufferLineProvider = null;
			}

			var lineTransformProvider = LineTransformProvider;
			if (InLayout)
				throw new InvalidOperationException();
			inLayout = true;
			var oldVisibleLines = new HashSet<HexViewLine>(wpfHexViewLineCollection == null ? (IEnumerable<HexViewLine>)Array.Empty<HexViewLine>() : wpfHexViewLineCollection);
			wpfHexViewLineCollection?.Invalidate();

			var layoutHelper = new LayoutHelper(BufferLines, lineTransformProvider, newViewportTop ?? 0, oldVisibleLines, GetValidCachedLines(regionsToInvalidate), FormattedLineSource);
			layoutHelper.LayoutLines(bufferPosition, relativeTo, verticalDistance, ViewportLeft, viewportWidthOverride, viewportHeightOverride);

			visiblePhysicalLines.AddRange(layoutHelper.AllVisiblePhysicalLines);
			wpfHexViewLineCollection = new WpfHexViewLineCollectionImpl(this, layoutHelper.AllVisibleLines);

			if (!InLayout)
				throw new InvalidOperationException();
			inLayout = false;

			textLayer.AddVisibleLines(layoutHelper.AllVisibleLines);
			var newOrReformattedLines = layoutHelper.NewOrReformattedLines.ToArray();
			var translatedLines = layoutHelper.TranslatedLines.ToArray();

			if (layoutHelper.NewViewportTop != viewportTop) {
				viewportTop = layoutHelper.NewViewportTop;
				Canvas.SetTop(normalAdornmentLayerCollection, -viewportTop);
			}
			RaiseLayoutChanged(viewportWidthOverride, viewportHeightOverride, newOrReformattedLines, translatedLines);
			if (raiseBufferLinesChangedEvent)
				RaiseBufferLinesChanged(oldBufferLines);
		}

		void RaiseBufferLinesChanged(HexBufferLineProvider oldBufferLines) {
			// Always access the property so it's recreated if the backing field is null
			var newBufferLines = BufferLines;
			BufferLinesChanged?.Invoke(this, new BufferLinesChangedEventArgs(oldBufferLines, newBufferLines));
		}

		List<PhysicalLine> GetValidCachedLines(NormalizedHexBufferSpanCollection regionsToInvalidate) {
			var lines = new List<PhysicalLine>(visiblePhysicalLines);
			lines.AddRange(physicalLineCache.TakeOwnership());
			visiblePhysicalLines.Clear();

			// Common enough that it's worth checking
			bool invalidateAll = false;
			if (regionsToInvalidate.Count == 1) {
				var r = regionsToInvalidate[0];
				if (r.Start <= BufferLines.BufferStart && r.End >= BufferLines.BufferEnd)
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
				bool remove = line.OverlapsWith(regionsToInvalidate);
				if (remove) {
					line.Dispose();
					lines.RemoveAt(i);
				}
				else
					line.UpdateIsLastLine();
			}

			return lines;
		}

		public override HexAdornmentLayer GetAdornmentLayer(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var info = adornmentLayerDefinitionService.GetLayerDefinition(name);
			if (info == null)
				throw new ArgumentException($"Adornment layer {name} doesn't exist");

			switch (GetLayerKind(info.Value.Metadata)) {
			case HexLayerKind.Normal:
				return normalAdornmentLayerCollection.GetAdornmentLayer(info.Value);

			case HexLayerKind.Overlay:
				return overlayAdornmentLayerCollection.GetAdornmentLayer(info.Value);

			case HexLayerKind.Underlay:
				return underlayAdornmentLayerCollection.GetAdornmentLayer(info.Value);

			default:
				Debug.Fail($"Invalid {nameof(HexLayerKind)} value: {info.Value.Metadata.LayerKind}");
				goto case HexLayerKind.Normal;
			}
		}

		static HexLayerKind GetLayerKind(IAdornmentLayersMetadata md) {
			if (md.IsOverlayLayer) {
				Debug.Assert(md.LayerKind == HexLayerKind.Normal, $"Use only one of {nameof(Contracts.Text.Editor.IsOverlayLayerAttribute)} and {nameof(HexLayerKindAttribute)}");
				return HexLayerKind.Overlay;
			}
			return md.LayerKind;
		}

		void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
			if (!IsClosed) {
				if (sizeInfo.PreviousSize.Height != sizeInfo.NewSize.Height)
					ViewportHeightChanged?.Invoke(this, EventArgs.Empty);
				if (sizeInfo.PreviousSize.Width != sizeInfo.NewSize.Width)
					ViewportWidthChanged?.Invoke(this, EventArgs.Empty);
				UpdateVisibleLines();
				RaiseLayoutChanged();
				InvalidateFormattedLineSource(false);
			}
		}

		void UpdateVisibleLines() => UpdateVisibleLines(ViewportWidth, ViewportHeight);
		void UpdateVisibleLines(double viewportWidthOverride, double ViewportHeightOverride) {
			if (wpfHexViewLineCollection == null)
				return;
			foreach (HexFormattedLine line in wpfHexViewLineCollection)
				line.SetVisibleArea(new Rect(ViewportLeft, ViewportTop, viewportWidthOverride, ViewportHeightOverride));
		}

		void InitializeZoom() {
			var window = Window.GetWindow(canvas);
			metroWindow = window as MetroWindow;
			if (window != null && metroWindow == null)
				return;
			if (metroWindow != null) {
				metroWindow.WindowDpiChanged += MetroWindow_WindowDpiChanged;
				MetroWindow_WindowDpiChanged(metroWindow, EventArgs.Empty);
				ZoomLevelChanged?.Invoke(this, new VSTE.ZoomLevelChangedEventArgs(ZoomLevel, canvas.LayoutTransform));
				return;
			}

			canvas.Loaded += WpfHexView_Loaded;
		}
		MetroWindow metroWindow;

		void WpfHexView_Loaded(object sender, RoutedEventArgs e) {
			canvas.Loaded -= WpfHexView_Loaded;
			var window = Window.GetWindow(canvas);
			metroWindow = window as MetroWindow;
			Debug.Assert(window != null);
			if (metroWindow != null) {
				metroWindow.WindowDpiChanged += MetroWindow_WindowDpiChanged;
				MetroWindow_WindowDpiChanged(metroWindow, EventArgs.Empty);
				ZoomLevelChanged?.Invoke(this, new VSTE.ZoomLevelChangedEventArgs(ZoomLevel, canvas.LayoutTransform));
				return;
			}
		}

		void MetroWindow_WindowDpiChanged(object sender, EventArgs e) {
			Debug.Assert(sender != null && sender == metroWindow);
			((MetroWindow)sender).SetScaleTransform(canvas, ZoomLevel / 100);
		}

		HexLineTransformProvider LineTransformProvider {
			get {
				if (recreateLineTransformProvider) {
					__lineTransformProvider = lineTransformProviderService.Create(this, removeExtraTextLineVerticalPixels);
					recreateLineTransformProvider = false;
				}
				return __lineTransformProvider;
			}
		}
		HexLineTransformProvider __lineTransformProvider;
		bool recreateLineTransformProvider;
		bool removeExtraTextLineVerticalPixels;

		public override HexLineTransformSource LineTransformSource => thisHexLineTransformSource;
		readonly HexLineTransformSource thisHexLineTransformSource;

		sealed class MyHexLineTransformSource : HexLineTransformSource {
			readonly WpfHexViewImpl owner;
			public MyHexLineTransformSource(WpfHexViewImpl owner) {
				this.owner = owner;
			}
			public override VSTF.LineTransform GetLineTransform(HexViewLine line, double yPosition, VSTE.ViewRelativePosition placement) =>
				owner.LineTransformProvider.GetLineTransform(line, yPosition, placement);
		}

		public override event EventHandler<HexMouseHoverEventArgs> MouseHover {
			add { mouseHoverHelper.MouseHover += value; }
			remove { mouseHoverHelper.MouseHover -= value; }
		}
		readonly MouseHoverHelper mouseHoverHelper;

		public override HexSpaceReservationManager GetSpaceReservationManager(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			return spaceReservationStack.GetSpaceReservationManager(name);
		}

		public override void QueueSpaceReservationStackRefresh() {
			if (IsClosed)
				return;
			if (queueSpaceReservationStackRefreshInProgress)
				return;
			queueSpaceReservationStackRefreshInProgress = true;
			canvas.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (IsClosed)
					return;
				queueSpaceReservationStackRefreshInProgress = false;
				spaceReservationStack.Refresh();
			}));
		}
		bool queueSpaceReservationStackRefreshInProgress;

		void WpfHexView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) =>
			QueueSpaceReservationStackRefresh();

		internal bool IsMouseOverOverlayLayerElement(MouseEventArgs e) => overlayAdornmentLayerCollection.IsMouseOverOverlayLayerElement(e);

		public override event EventHandler<BufferLinesChangedEventArgs> BufferLinesChanged;
		public override HexBufferLineProvider BufferLines {
			get {
				// Don't raise BufferLinesChanged event here. It's the responsibility of the code
				// clearing this field to raise the event. It's not safe to raise the event at any
				// time (eg. in the middle of layout)
				if (hexBufferLineProvider == null)
					hexBufferLineProvider = bufferLineProviderFactoryService.Create(Buffer, GetHexBufferLineProviderOptions());
				return hexBufferLineProvider;
			}
		}
		HexBufferLineProvider hexBufferLineProvider;
		bool recreateHexBufferLineProvider;

		void InvalidateHexBufferLineProvider() {
			canvas.Dispatcher.VerifyAccess();
			recreateHexBufferLineProvider = true;
			DelayLayoutLines(true);
		}

		HexBufferLineProviderOptions GetHexBufferLineProviderOptions() {
			Debug.Assert(!double.IsNaN(lastFormattedLineSourceViewportWidth));
			var options = new HexBufferLineProviderOptions();
			options.CharsPerLine = (int)((lastFormattedLineSourceViewportWidth - FormattedLineSource.BaseIndentation) / FormattedLineSource.ColumnWidth);
			options.BytesPerLine = Options.GetBytesPerLine();
			options.GroupSizeInBytes = Options.GetGroupSizeInBytes();
			options.ShowOffset = Options.ShowOffsetColumn();
			options.OffsetLowerCaseHex = Options.IsOffsetLowerCaseHexEnabled();
			options.OffsetFormat = Options.GetOffsetFormat();
			options.StartPosition = Options.GetStartPosition();
			options.EndPosition = Options.GetEndPosition();
			options.BasePosition = Options.GetBasePosition();
			options.UseRelativePositions = Options.UseRelativePositions();
			options.ShowValues = Options.ShowValuesColumn();
			options.ValuesLowerCaseHex = Options.IsValuesLowerCaseHexEnabled();
			options.OffsetBitSize = Options.GetOffsetBitSize();
			options.ValuesFormat = Options.GetValuesDisplayFormat();
			options.ShowAscii = Options.ShowAsciiColumn();
			options.ColumnOrder = new HexColumnType[3] { HexColumnType.Offset, HexColumnType.Values, HexColumnType.Ascii };

			const int DEFAULT_CHARS_PER_LINE = 80;
			if (options.CharsPerLine <= 0)
				options.CharsPerLine = DEFAULT_CHARS_PER_LINE;
			if (options.BytesPerLine < 0)
				options.BytesPerLine = 0;
			if (options.GroupSizeInBytes < 0)
				options.GroupSizeInBytes = 0;
			if (options.StartPosition >= HexPosition.MaxEndPosition)
				options.StartPosition = 0;
			if (options.EndPosition > HexPosition.MaxEndPosition)
				options.EndPosition = HexPosition.MaxEndPosition;
			if (options.StartPosition > options.EndPosition)
				options.StartPosition = options.EndPosition;
			if (options.BasePosition >= HexPosition.MaxEndPosition)
				options.BasePosition = 0;
			if (options.OffsetBitSize < HexBufferLineProviderOptions.MinOffsetBitSize)
				options.OffsetBitSize = HexBufferLineProviderOptions.MinOffsetBitSize;
			else if (options.OffsetBitSize > HexBufferLineProviderOptions.MaxOffsetBitSize)
				options.OffsetBitSize = HexBufferLineProviderOptions.MaxOffsetBitSize;

			return options;
		}
	}
}
