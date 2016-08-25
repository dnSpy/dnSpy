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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

// Adds colorized vertical lines between block start/end lines. Similar to Productivity Power Tools 2015's structure visualizer.

namespace dnSpy.Text.Editor {
	[Export(typeof(IStructureVisualizerServiceProvider))]
	sealed class StructureVisualizerServiceProvider : IStructureVisualizerServiceProvider {
		readonly IEditorFormatMapService editorFormatMapService;

		[ImportingConstructor]
		StructureVisualizerServiceProvider(IEditorFormatMapService editorFormatMapService) {
			this.editorFormatMapService = editorFormatMapService;
		}

		public IStructureVisualizerService GetService(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			return wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(StructureVisualizerService), () => new StructureVisualizerService(wpfTextView, editorFormatMapService));
		}
	}

	sealed class StructureVisualizerService : IStructureVisualizerService {
#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedDnSpyAdornmentLayers.StructureVisualizer)]
		[Order(After = PredefinedDnSpyAdornmentLayers.BottomLayer, Before = PredefinedDnSpyAdornmentLayers.TopLayer)]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.InterLine)]
		static AdornmentLayerDefinition textMarkerAdornmentLayerDefinition;
#pragma warning restore 0169

		readonly IWpfTextView wpfTextView;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly List<LineElement> lineElements;
		readonly List<LineColorInfo> lineColorInfos;
		IAdornmentLayer layer;
		IEditorFormatMap editorFormatMap;
		IStructureVisualizerServiceDataProvider structureVisualizerServiceDataProvider;
		bool enabled;

		sealed class NullStructureVisualizerServiceDataProvider : IStructureVisualizerServiceDataProvider {
			public static readonly NullStructureVisualizerServiceDataProvider Instance = new NullStructureVisualizerServiceDataProvider();
			public void GetData(SnapshotSpan lineExtent, List<StructureVisualizerData> list) { }
		}

		public StructureVisualizerService(IWpfTextView wpfTextView, IEditorFormatMapService editorFormatMapService) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (editorFormatMapService == null)
				throw new ArgumentNullException(nameof(editorFormatMapService));
			this.wpfTextView = wpfTextView;
			this.editorFormatMapService = editorFormatMapService;
			this.structureVisualizerServiceDataProvider = NullStructureVisualizerServiceDataProvider.Instance;
			this.onRemovedDelegate = OnRemoved;
			this.lineElements = new List<LineElement>();
			this.xPosCache = new XPosCache(wpfTextView);
			this.lineColorInfos = new List<LineColorInfo> {
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerNamespace),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerType),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerMethod),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerConditional),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerLoop),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerProperty),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerEvent),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerTry),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerCatch),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerFilter),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerFinally),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerFault),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerOther),
			};
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.Options.OptionChanged += Options_OptionChanged;
			UpdateEnabled();
		}

		sealed class LineColorInfo {
			public string Type { get; }
			public Pen Pen { get; set; }

			public LineColorInfo(string type) {
				Type = type;
			}
		}

		void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e) {
			if (wpfTextView.IsClosed)
				return;

			bool refresh = false;
			if (e.ChangedItems.Count > 50)
				refresh = true;
			if (!refresh) {
				var hash = new HashSet<string>(StringComparer.Ordinal);
				foreach (var info in lineColorInfos)
					hash.Add(info.Type);
				foreach (var s in e.ChangedItems) {
					if (hash.Contains(s)) {
						refresh = true;
						break;
					}
				}
			}

			if (refresh) {
				UpdateColorInfos();
				RepaintAllLines();
			}
		}

		void UpdateColorInfos() {
			foreach (var info in lineColorInfos) {
				var props = editorFormatMap.GetProperties(info.Type);
				info.Pen = GetPen(props);
			}
		}

		const double PEN_THICKNESS = 1.0;
		Pen GetPen(ResourceDictionary props) {
			Color? color;
			SolidColorBrush scBrush;

			Pen newPen;
			if ((color = props[EditorFormatDefinition.ForegroundColorId] as Color?) != null) {
				var brush = new SolidColorBrush(color.Value);
				brush.Freeze();
				newPen = new Pen(brush, PEN_THICKNESS);
				newPen.Freeze();
			}
			else if ((scBrush = props[EditorFormatDefinition.ForegroundBrushId] as SolidColorBrush) != null) {
				if (scBrush.CanFreeze)
					scBrush.Freeze();
				newPen = new Pen(scBrush, PEN_THICKNESS);
				newPen.Freeze();
			}
			else if ((newPen = props[MarkerFormatDefinition.BorderId] as Pen) != null) {
				if (newPen.CanFreeze)
					newPen.Freeze();
			}

			return newPen;
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			if (e.OptionId == DefaultDnSpyTextViewOptions.ShowStructureLinesId.Name)
				UpdateEnabled();
		}

		void UpdateEnabled() {
			var newValue = wpfTextView.Options.IsShowStructureLinesEnabled();
			if (newValue == enabled)
				return;
			enabled = newValue;
			if (enabled) {
				if (layer == null)
					layer = wpfTextView.GetAdornmentLayer(PredefinedDnSpyAdornmentLayers.StructureVisualizer);
				if (editorFormatMap == null)
					editorFormatMap = editorFormatMapService.GetEditorFormatMap(wpfTextView);
				RegisterEvents();
				UpdateColorInfos();
				RepaintAllLines();
			}
			else {
				UnregisterEvents();
				RemoveAllLineElements();
				ClearXPosCache();
			}
		}

		public void SetDataProvider(IStructureVisualizerServiceDataProvider dataProvider) {
			if (wpfTextView.IsClosed)
				return;
			this.structureVisualizerServiceDataProvider = dataProvider ?? NullStructureVisualizerServiceDataProvider.Instance;
			if (enabled) {
				ClearXPosCache();
				RepaintAllLines();
			}
		}

		void RepaintAllLines() {
			RemoveAllLineElements();
			UpdateRange(new NormalizedSnapshotSpanCollection(wpfTextView.TextViewLines.FormattedSpan));
		}

		void RemoveLineElements(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				return;
			for (int i = lineElements.Count - 1; i >= 0; i--) {
				var lineElement = lineElements[i];
				if (spans.IntersectsWith(lineElement.Span))
					layer.RemoveAdornment(lineElement);
			}
		}

		struct LineElementData {
			public ITextViewLine Line { get; }
			public StructureVisualizerData[] Data { get; }
			public LineElementData(ITextViewLine line, StructureVisualizerData[] data) {
				Line = line;
				Data = data;
			}
		}

		// Returns the intersection, including the previous line if span starts at the beginning of a line
		IList<ITextViewLine> GetTextViewLinesIntersectingSpan(SnapshotSpan span, ref List<ITextViewLine> list) {
			IList<ITextViewLine> lines = wpfTextView.TextViewLines.GetTextViewLinesIntersectingSpan(span);
			if ((lines.Count == 0 || span.Start == lines[0].Start) && span.Start.Position != 0) {
				var prevLine = wpfTextView.TextViewLines.GetTextViewLineContainingBufferPosition(span.Start - 1);
				if (prevLine != null) {
					if (list == null)
						list = new List<ITextViewLine>();
					list.Add(prevLine);
					list.AddRange(lines);
					return list;
				}
			}
			return lines;
		}

		IEnumerable<LineElementData> GetLineElementData(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				yield break;
			var snapshot = spans[0].Snapshot;
			ITextSnapshotLine snapshotLine = null;
			SnapshotSpan lineExtent;
			var prevLineExtent = default(SnapshotSpan);
			var list = new List<StructureVisualizerData>();
			StructureVisualizerData[] listArray = null;
			List<ITextViewLine> linesList = null;
			foreach (var span in spans) {
				var lines = GetTextViewLinesIntersectingSpan(span, ref linesList);
				foreach (var line in lines) {
					if (snapshotLine != null) {
						if (line.Start >= snapshotLine.Start && line.EndIncludingLineBreak <= snapshotLine.EndIncludingLineBreak) {
							// Nothing
						}
						else if (line.Start == snapshotLine.EndIncludingLineBreak)
							snapshotLine = snapshotLine.Snapshot.GetLineFromLineNumber(snapshotLine.LineNumber + 1);
						else
							snapshotLine = line.Start.GetContainingLine();
						lineExtent = snapshotLine.Extent;
					}
					else if (line.IsFirstTextViewLineForSnapshotLine && line.IsLastTextViewLineForSnapshotLine)
						lineExtent = line.Extent;
					else {
						snapshotLine = line.Start.GetContainingLine();
						lineExtent = snapshotLine.Extent;
					}

					if (prevLineExtent != lineExtent) {
						list.Clear();
						structureVisualizerServiceDataProvider.GetData(lineExtent, list);
						listArray = list.Count == 0 ? Array.Empty<StructureVisualizerData>() : list.ToArray();
					}

					if (listArray.Length != 0) {
						var last = listArray[listArray.Length - 1];
						// Don't add a vertical line to the line containing the start block
						if (!(last.Top.Start >= lineExtent.Start && last.Top.End <= lineExtent.End)) {
							var ary = listArray;
							if (last.Bottom.Start >= lineExtent.Start && last.Bottom.End <= lineExtent.End) {
								ary = new StructureVisualizerData[listArray.Length - 1];
								for (int i = 0; i < ary.Length; i++)
									ary[i] = listArray[i];
							}
							yield return new LineElementData(line, ary);
						}
					}

					prevLineExtent = lineExtent;
				}
			}
		}

		void AddLineElements(NormalizedSnapshotSpanCollection spans) {
			foreach (var data in GetLineElementData(spans)) {
				var lineElement = TryCreateLineElement(data);
				if (lineElement == null)
					continue;
				bool added = layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, lineElement.Span, null, lineElement, onRemovedDelegate);
				if (added)
					lineElements.Add(lineElement);
			}
		}

		readonly AdornmentRemovedCallback onRemovedDelegate;
		void OnRemoved(object tag, UIElement element) => lineElements.Remove((LineElement)element);

		void UpdateRange(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 1 && spans[0].Start.Position == 0 && spans[0].Length == spans[0].Snapshot.Length)
				RemoveAllLineElements();
			else
				RemoveLineElements(spans);
			AddLineElements(spans);
		}

		void RemoveAllLineElements() {
			lineElements.Clear();
			layer?.RemoveAllAdornments();
		}

		LineElement TryCreateLineElement(LineElementData data) {
			var lineElement = new LineElement(data.Line, GetLineElementDrawData(data.Data));
			return lineElement;
		}

		LineElementDrawData[] GetLineElementDrawData(StructureVisualizerData[] data) {
			var res = new LineElementDrawData[data.Length];

			for (int i = 0; i < res.Length; i++)
				res[i] = CreateLineElementDrawData(data[i]);

			return res;
		}

		LineElementDrawData CreateLineElementDrawData(StructureVisualizerData data) {
			var pen = GetPen(data.BlockKind);
			Debug.Assert(pen != null);
			double x = GetLineXPosition(data);
			return new LineElementDrawData(pen, x);
		}

		Pen GetPen(StructureVisualizerDataBlockKind blockKind) => GetLineColorInfo(GetColorInfoType(blockKind)).Pen;

		LineColorInfo GetLineColorInfo(string type) {
			foreach (var info in lineColorInfos) {
				if (info.Type == type)
					return info;
			}
			Debug.Fail($"Unknown type: {type}");
			return lineColorInfos[0];
		}

		string GetColorInfoType(StructureVisualizerDataBlockKind blockKind) {
			switch (blockKind) {
			case StructureVisualizerDataBlockKind.Namespace:	return ThemeClassificationTypeNameKeys.StructureVisualizerNamespace;
			case StructureVisualizerDataBlockKind.Type:			return ThemeClassificationTypeNameKeys.StructureVisualizerType;
			case StructureVisualizerDataBlockKind.Method:		return ThemeClassificationTypeNameKeys.StructureVisualizerMethod;
			case StructureVisualizerDataBlockKind.Conditional:	return ThemeClassificationTypeNameKeys.StructureVisualizerConditional;
			case StructureVisualizerDataBlockKind.Loop:			return ThemeClassificationTypeNameKeys.StructureVisualizerLoop;
			case StructureVisualizerDataBlockKind.Property:		return ThemeClassificationTypeNameKeys.StructureVisualizerProperty;
			case StructureVisualizerDataBlockKind.Event:		return ThemeClassificationTypeNameKeys.StructureVisualizerEvent;
			case StructureVisualizerDataBlockKind.Try:			return ThemeClassificationTypeNameKeys.StructureVisualizerTry;
			case StructureVisualizerDataBlockKind.Catch:		return ThemeClassificationTypeNameKeys.StructureVisualizerCatch;
			case StructureVisualizerDataBlockKind.Filter:		return ThemeClassificationTypeNameKeys.StructureVisualizerFilter;
			case StructureVisualizerDataBlockKind.Finally:		return ThemeClassificationTypeNameKeys.StructureVisualizerFinally;
			case StructureVisualizerDataBlockKind.Fault:		return ThemeClassificationTypeNameKeys.StructureVisualizerFault;
			case StructureVisualizerDataBlockKind.Other:		return ThemeClassificationTypeNameKeys.StructureVisualizerOther;
			default:
				Debug.Fail($"Unknown block kind: {blockKind}");
				return ThemeClassificationTypeNameKeys.StructureVisualizerOther;
			}
		}

		double GetLineXPosition(StructureVisualizerData data) => xPosCache.GetXPosition(data);
		void ClearXPosCache() => xPosCache.Clear();
		readonly XPosCache xPosCache;

		sealed class XPosCache {
			readonly IWpfTextView wpfTextView;
			readonly Dictionary<int, double> toXPosDict;
			ITextSnapshot toXPosDictSnapshot;
			IFormattedLineSource formattedLineSource;

			public XPosCache(IWpfTextView wpfTextView) {
				this.wpfTextView = wpfTextView;
				this.toXPosDict = new Dictionary<int, double>();
			}

			public double GetXPosition(StructureVisualizerData data) {
				TryUpdateState();
				var topPoint = data.Top.Start.TranslateTo(toXPosDictSnapshot, PointTrackingMode.Negative);
				double x;
				if (toXPosDict.TryGetValue(topPoint.Position, out x))
					return x;

				var point = GetBlockStartPoint(topPoint, data.Bottom.Start.TranslateTo(toXPosDictSnapshot, PointTrackingMode.Negative));
				var line = wpfTextView.GetTextViewLineContainingBufferPosition(point);
				var bounds = line.GetExtendedCharacterBounds(point);
				x = Math.Round(bounds.Left + bounds.Width / 2 - PEN_THICKNESS / 2) + 0.5;
				toXPosDict[topPoint.Position] = x;
				return x;
			}

			SnapshotPoint GetBlockStartPoint(SnapshotPoint top, SnapshotPoint bottom) {
				int topColumn, bottomColumn;
				var topPoint = GetPositionOfNonWhitespace(top, out topColumn);
				var bottomPoint = GetPositionOfNonWhitespace(bottom, out bottomColumn);
				return topColumn <= bottomColumn ? topPoint : bottomPoint;
			}

			SnapshotPoint GetPositionOfNonWhitespace(SnapshotPoint point, out int column) {
				var line = point.GetContainingLine();
				var snapshot = line.Snapshot;
				int pos = line.Start.Position;
				int end = line.End.Position;
				int colRes = 0;
				while (pos < end) {
					int len = end - pos;
					if (len > readBuffer.Length)
						len = readBuffer.Length;
					snapshot.CopyTo(pos, readBuffer, 0, len);
					for (int i = 0; i < len; i++) {
						if (!char.IsWhiteSpace(readBuffer[i]))
							goto done;
						colRes++;
					}
					pos += len;
				}
done:
				column = colRes;
				return line.Start + colRes;
			}
			readonly char[] readBuffer = new char[0x20];

			public bool TryUpdateState() {
				if (toXPosDictSnapshot != wpfTextView.TextSnapshot || formattedLineSource != wpfTextView.FormattedLineSource) {
					Clear();
					toXPosDictSnapshot = wpfTextView.TextSnapshot;
					formattedLineSource = wpfTextView.FormattedLineSource;
					return true;
				}
				return false;
			}

			public void Clear() {
				toXPosDict.Clear();
				toXPosDictSnapshot = null;
				formattedLineSource = null;
			}
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (xPosCache.TryUpdateState())
				RepaintAllLines();
			else
				UpdateLines(e.NewOrReformattedLines);
		}

		void UpdateLines(IList<ITextViewLine> newOrReformattedLines) {
			if (newOrReformattedLines.Count == wpfTextView.TextViewLines.Count)
				RemoveAllLineElements();

			var lineSpans = new List<SnapshotSpan>();
			foreach (var line in newOrReformattedLines)
				lineSpans.Add(line.ExtentIncludingLineBreak);
			var spans = new NormalizedSnapshotSpanCollection(lineSpans);
			UpdateRange(spans);
		}

		struct LineElementDrawData {
			public Pen Pen { get; }
			public double X { get; }
			public LineElementDrawData(Pen pen, double x) {
				Pen = pen;
				X = x;
			}
		}

		sealed class LineElement : UIElement {
			public SnapshotSpan Span { get; }

			readonly LineElementDrawData[] drawData;
			readonly double height;

			public LineElement(ITextViewLine line, LineElementDrawData[] drawData) {
				Span = line.ExtentIncludingLineBreak;
				this.drawData = drawData;
				this.height = line.Height;
				Canvas.SetTop(this, line.Top);
			}

			protected override void OnRender(DrawingContext drawingContext) {
				base.OnRender(drawingContext);
				foreach (var data in drawData)
					drawingContext.DrawLine(data.Pen, new Point(data.X, 0), new Point(data.X, height));
			}
		}

		void RegisterEvents() {
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
			editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
		}

		void UnregisterEvents() {
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			editorFormatMap.FormatMappingChanged -= EditorFormatMap_FormatMappingChanged;
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			UnregisterEvents();
			RemoveAllLineElements();
			ClearXPosCache();
			structureVisualizerServiceDataProvider = NullStructureVisualizerServiceDataProvider.Instance;
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.Options.OptionChanged -= Options_OptionChanged;
		}
	}
}
