/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using CTC = dnSpy.Contracts.Text.Classification;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewCreationListener))]
	[VSTE.TextViewRole(PredefinedHexViewRoles.CanHaveColumnLineSeparator)]
	sealed class ColumnLineSeparatorWpfHexViewCreationListener : WpfHexViewCreationListener {
		readonly ColumnLineSeparatorServiceProvider columnLineSeparatorServiceProvider;

		[ImportingConstructor]
		ColumnLineSeparatorWpfHexViewCreationListener(ColumnLineSeparatorServiceProvider columnLineSeparatorServiceProvider) {
			this.columnLineSeparatorServiceProvider = columnLineSeparatorServiceProvider;
		}

		public override void HexViewCreated(WpfHexView wpfHexView) =>
			columnLineSeparatorServiceProvider.InstallLineSeparatorService(wpfHexView);
	}

	abstract class ColumnLineSeparatorServiceProvider {
		public abstract void InstallLineSeparatorService(WpfHexView wpfHexView);
	}

	[Export(typeof(ColumnLineSeparatorServiceProvider))]
	sealed class ColumnLineSeparatorServiceProviderImpl : ColumnLineSeparatorServiceProvider {
		readonly HexEditorFormatMapService editorFormatMapService;

		[ImportingConstructor]
		ColumnLineSeparatorServiceProviderImpl(HexEditorFormatMapService editorFormatMapService) {
			this.editorFormatMapService = editorFormatMapService;
		}

		public override void InstallLineSeparatorService(WpfHexView wpfHexView) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			wpfHexView.Properties.GetOrCreateSingletonProperty(typeof(ColumnLineSeparatorService), () => new ColumnLineSeparatorService(wpfHexView, editorFormatMapService));
		}
	}

	sealed class ColumnLineSeparatorService {
		readonly WpfHexView wpfHexView;
		readonly VSTC.IEditorFormatMap editorFormatMap;
		HexAdornmentLayer adornmentLayer;
		bool enabled;
		readonly List<LineElement> lineElements;

		enum LineElementKind {
			Column0,
			Column1,
			Group0,
			Group1,
			Last,
		}

		static readonly string[] classificationTypeNames = new string[(int)LineElementKind.Last] {
			CTC.ThemeClassificationTypeNameKeys.HexColumnLine0,
			CTC.ThemeClassificationTypeNameKeys.HexColumnLine1,
			CTC.ThemeClassificationTypeNameKeys.HexColumnLineGroup0,
			CTC.ThemeClassificationTypeNameKeys.HexColumnLineGroup1,
		};

		sealed class LineElement : UIElement {
			public LineElementKind Kind { get; }
			readonly double x;
			readonly double top;
			readonly double bottom;
			readonly Pen pen;

			public LineElement(LineElementKind kind, double x, double top, double bottom, Pen pen) {
				Canvas.SetTop(this, top);
				Kind = kind;
				this.x = x;
				this.top = top;
				this.bottom = bottom;
				this.pen = pen;
			}

			protected override void OnRender(DrawingContext drawingContext) {
				base.OnRender(drawingContext);
				drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, bottom - top));
			}
		}

#pragma warning disable 0169
		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.ColumnLineSeparator)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.BottomLayer, Before = PredefinedHexAdornmentLayers.TopLayer)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.Selection, Before = PredefinedHexAdornmentLayers.Text)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.TextMarker)]
		static HexAdornmentLayerDefinition theAdornmentLayerDefinition;
#pragma warning restore 0169

		public ColumnLineSeparatorService(WpfHexView wpfHexView, HexEditorFormatMapService editorFormatMapService) {
			if (editorFormatMapService == null)
				throw new ArgumentNullException(nameof(editorFormatMapService));
			lineElements = new List<LineElement>();
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			editorFormatMap = editorFormatMapService.GetEditorFormatMap(wpfHexView);
			wpfHexView.Closed += WpfHexView_Closed;
			wpfHexView.Options.OptionChanged += Options_OptionChanged;
			UpdateEnabled();
		}

		void UpdateEnabled() {
			var newEnabled = wpfHexView.Options.ShowColumnLines();
			if (newEnabled == enabled)
				return;
			enabled = newEnabled;

			if (enabled) {
				if (adornmentLayer == null)
					adornmentLayer = wpfHexView.GetAdornmentLayer(PredefinedHexAdornmentLayers.ColumnLineSeparator);
				HookEnabledEvents();
			}
			else
				UnhookEnabledEvents();

			DelayRecreateColumnLines();
		}

		void HookEnabledEvents() {
			wpfHexView.LayoutChanged += WpfHexView_LayoutChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
		}

		void UnhookEnabledEvents() {
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
		}

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			switch (e.OptionId) {
			case DefaultHexViewOptions.ShowColumnLinesName:
				UpdateEnabled();
				break;

			case DefaultHexViewOptions.ColumnLine0Name:
			case DefaultHexViewOptions.ColumnLine1Name:
			case DefaultHexViewOptions.ColumnGroupLine0Name:
			case DefaultHexViewOptions.ColumnGroupLine1Name:
				DelayRecreateColumnLines();
				break;
			}
		}

		void WpfHexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			bool recreate = false;
			if (latestBufferLines != wpfHexView.BufferLines)
				recreate = true;
			else if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				recreate = true;
			if (recreate)
				RecreateColumnLines();
			else
				UpdateLineElementPositions(e);
		}
		HexBufferLineFormatter latestBufferLines;

		void UpdateLineElementPositions(HexViewLayoutChangedEventArgs e) {
			var d = e.NewViewState.ViewportTop - e.OldViewState.ViewportTop;
			if (Math.Abs(d) <= 0.001)
				return;
			foreach (var lineElement in lineElements)
				Canvas.SetTop(lineElement, Canvas.GetTop(lineElement) + d);
		}

		void EditorFormatMap_FormatMappingChanged(object sender, VSTC.FormatItemsEventArgs e) {
			if (wpfHexView.IsClosed)
				return;

			bool refresh = false;
			if (e.ChangedItems.Count > 50)
				refresh = true;
			if (!refresh) {
				var hash = new HashSet<string>(StringComparer.Ordinal);
				foreach (var name in classificationTypeNames)
					hash.Add(name);
				foreach (var s in e.ChangedItems) {
					if (hash.Contains(s)) {
						refresh = true;
						break;
					}
				}
			}

			if (refresh)
				DelayRecreateColumnLines();
		}

		void DelayRecreateColumnLines() {
			if (delayRecreateColumnLinesCalled)
				return;
			wpfHexView.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(RecreateColumnLines));
		}
		bool delayRecreateColumnLinesCalled;

		void RecreateColumnLines() {
			delayRecreateColumnLinesCalled = false;
			if (wpfHexView.IsClosed)
				return;

			RemoveAllLines();
			if (!enabled)
				return;

			if (wpfHexView.ViewportHeight == 0)
				return;

			var line = wpfHexView.HexViewLines.FirstVisibleLine;
			var top = wpfHexView.ViewportTop;
			var bottom = wpfHexView.ViewportBottom;
			foreach (var info in GetColumnPositions(line.BufferLine)) {
				var lineKind = GetColumnLineKind(info.Key);
				if (lineKind == HexColumnLineKind.None)
					continue;
				var props = editorFormatMap.GetProperties(classificationTypeNames[(int)info.Key]);
				var pen = GetPen(props, lineKind);
				var bounds = line.GetCharacterBounds(info.Value);
				var x = Math.Round(bounds.Left + bounds.Width / 2 - PEN_THICKNESS / 2) + 0.5;
				var lineElem = new LineElement(info.Key, x, top, bottom, pen);
				bool added = adornmentLayer.AddAdornment(VSTE.AdornmentPositioningBehavior.OwnerControlled, (HexBufferSpan?)null, null, lineElem, null);
				if (added)
					lineElements.Add(lineElem);
			}

			latestBufferLines = wpfHexView.BufferLines;
		}

		HexColumnLineKind GetColumnLineKind(LineElementKind lineElemKind) {
			switch (lineElemKind) {
			case LineElementKind.Column0:	return wpfHexView.Options.GetColumnLine0Kind();
			case LineElementKind.Column1:	return wpfHexView.Options.GetColumnLine1Kind();
			case LineElementKind.Group0:	return wpfHexView.Options.GetColumnGroupLine0Kind();
			case LineElementKind.Group1:	return wpfHexView.Options.GetColumnGroupLine1Kind();
			default: throw new ArgumentOutOfRangeException(nameof(lineElemKind));
			}
		}

		const double PEN_THICKNESS = 1.0;
		static Pen GetPen(ResourceDictionary props, HexColumnLineKind lineKind) {
			Color? color;
			SolidColorBrush scBrush;

			Pen newPen;
			if ((color = props[VSTC.EditorFormatDefinition.ForegroundColorId] as Color?) != null) {
				var brush = new SolidColorBrush(color.Value);
				brush.Freeze();
				newPen = InitializePen(new Pen(brush, PEN_THICKNESS), lineKind);
				newPen.Freeze();
			}
			else if ((scBrush = props[VSTC.EditorFormatDefinition.ForegroundBrushId] as SolidColorBrush) != null) {
				if (scBrush.CanFreeze)
					scBrush.Freeze();
				newPen = InitializePen(new Pen(scBrush, PEN_THICKNESS), lineKind);
				newPen.Freeze();
			}
			else if ((newPen = props[VSTC.MarkerFormatDefinition.BorderId] as Pen) != null) {
				if (newPen.CanFreeze)
					newPen.Freeze();
			}

			return newPen;
		}

		static Pen InitializePen(Pen pen, HexColumnLineKind lineKind) {
			switch (lineKind) {
			case HexColumnLineKind.None:
				Debug.Fail("Shouldn't be here");
				break;

			case HexColumnLineKind.Solid:
				break;

			case HexColumnLineKind.Dashed_1_1:
				pen.DashStyle = new DashStyle(dashed_1_1_DashStyle, 1);
				pen.DashCap = PenLineCap.Flat;
				break;

			case HexColumnLineKind.Dashed_2_2:
				pen.DashStyle = new DashStyle(dashed_2_2_DashStyle, 1);
				pen.DashCap = PenLineCap.Flat;
				break;

			case HexColumnLineKind.Dashed_3_3:
				pen.DashStyle = new DashStyle(dashed_3_3_DashStyle, 1);
				pen.DashCap = PenLineCap.Flat;
				break;

			case HexColumnLineKind.Dashed_4_4:
				pen.DashStyle = new DashStyle(dashed_4_4_DashStyle, 1);
				pen.DashCap = PenLineCap.Flat;
				break;

			default:
				Debug.Fail($"Unknown line kind: {lineKind}");
				break;
			}
			return pen;
		}
		static readonly IEnumerable<double> dashed_1_1_DashStyle = new ReadOnlyCollection<double>(new double[] { 1, 1 });
		static readonly IEnumerable<double> dashed_2_2_DashStyle = new ReadOnlyCollection<double>(new double[] { 2, 2 });
		static readonly IEnumerable<double> dashed_3_3_DashStyle = new ReadOnlyCollection<double>(new double[] { 3, 3 });
		static readonly IEnumerable<double> dashed_4_4_DashStyle = new ReadOnlyCollection<double>(new double[] { 4, 4 });

		IEnumerable<KeyValuePair<LineElementKind, int>> GetColumnPositions(HexBufferLine line) {
			var columns = line.ColumnOrder.Where(a => line.IsColumnPresent(a)).ToArray();
			for (int i = 1; i < columns.Length; i++) {
				Debug.Assert(i < 3);
				if (i >= 3)
					break;
				var colSpan1 = line.LineProvider.GetColumnSpan(columns[i - 1]);
				var colSpan2 = line.LineProvider.GetColumnSpan(columns[i]);
				Debug.Assert(colSpan1.End < colSpan2.Start);
				var kind = i == 1 ? LineElementKind.Column0 : LineElementKind.Column1;
				yield return new KeyValuePair<LineElementKind, int>(kind, colSpan1.End);
			}

			if (line.IsValuesColumnPresent) {
				var spanInfos = line.GetValuesSpans(line.BufferSpan, HexSpanSelectionFlags.Group0 | HexSpanSelectionFlags.Group1).ToArray();
				for (int i = 1; i < spanInfos.Length; i++) {
					Debug.Assert(spanInfos[i - 1].TextSpan.End == spanInfos[i].TextSpan.Start);
					int linePosition = spanInfos[i - 1].TextSpan.End - 1;
					var kind = i % 2 == 1 ? LineElementKind.Group0 : LineElementKind.Group1;
					yield return new KeyValuePair<LineElementKind, int>(kind, linePosition);
				}
			}
		}

		void RemoveAllLines() {
			adornmentLayer?.RemoveAllAdornments();
			lineElements.Clear();
		}

		void WpfHexView_Closed(object sender, EventArgs e) {
			RemoveAllLines();
			latestBufferLines = null;
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.Options.OptionChanged -= Options_OptionChanged;
			UnhookEnabledEvents();
		}
	}
}
