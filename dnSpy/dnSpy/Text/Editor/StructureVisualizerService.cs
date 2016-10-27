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
using System.Collections.ObjectModel;
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
		[Name(PredefinedAdornmentLayers.BlockStructure)]
		[Order(After = PredefinedDsAdornmentLayers.BottomLayer, Before = PredefinedDsAdornmentLayers.TopLayer)]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
		[Order(After = PredefinedAdornmentLayers.TextMarker)]
		static AdornmentLayerDefinition adornmentLayerDefinition;
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
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerModule),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerValueType),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerInterface),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerMethod),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerAccessor),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerAnonymousMethod),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerConstructor),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerDestructor),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerOperator),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerConditional),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerLoop),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerProperty),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerEvent),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerTry),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerCatch),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerFilter),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerFinally),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerFault),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerLock),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerUsing),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerFixed),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerSwitch),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerCase),
				new LineColorInfo(ThemeClassificationTypeNameKeys.StructureVisualizerLocalFunction),
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

			if (refresh)
				RefreshLinesAndColorInfos();
		}

		void RefreshLinesAndColorInfos() {
			UpdateColorInfos();
			RepaintAllLines();
		}

		void UpdateColorInfos() {
			var lineKind = wpfTextView.Options.GetBlockStructureLineKind();
			foreach (var info in lineColorInfos) {
				var props = editorFormatMap.GetProperties(info.Type);
				info.Pen = GetPen(props, lineKind);
			}
		}

		const double PEN_THICKNESS = 1.0;
		Pen GetPen(ResourceDictionary props, BlockStructureLineKind lineKind) {
			Color? color;
			SolidColorBrush scBrush;

			Pen newPen;
			if ((color = props[EditorFormatDefinition.ForegroundColorId] as Color?) != null) {
				var brush = new SolidColorBrush(color.Value);
				brush.Freeze();
				newPen = InitializePen(new Pen(brush, PEN_THICKNESS), lineKind);
				newPen.Freeze();
			}
			else if ((scBrush = props[EditorFormatDefinition.ForegroundBrushId] as SolidColorBrush) != null) {
				if (scBrush.CanFreeze)
					scBrush.Freeze();
				newPen = InitializePen(new Pen(scBrush, PEN_THICKNESS), lineKind);
				newPen.Freeze();
			}
			else if ((newPen = props[MarkerFormatDefinition.BorderId] as Pen) != null) {
				if (newPen.CanFreeze)
					newPen.Freeze();
			}

			return newPen;
		}

		Pen InitializePen(Pen pen, BlockStructureLineKind lineKind) {
			switch (lineKind) {
			case BlockStructureLineKind.Solid:
				break;

			case BlockStructureLineKind.Dotted_2_2:
				pen.DashStyle = new DashStyle(dotted_2_2_DashStyle, 1);
				pen.DashCap = PenLineCap.Flat;
				break;

			default:
				Debug.Fail($"Unknown line kind: {lineKind}");
				break;
			}
			return pen;
		}
		static readonly IEnumerable<double> dotted_2_2_DashStyle = new ReadOnlyCollection<double>(new double[] { 2, 2 });

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			if (e.OptionId == DefaultTextViewOptions.ShowBlockStructureName)
				UpdateEnabled();
			else if (enabled && e.OptionId == DefaultDsTextViewOptions.BlockStructureLineKindName)
				RefreshLinesAndColorInfos();
		}

		void UpdateEnabled() {
			var newValue = wpfTextView.Options.GetOptionValue(DefaultTextViewOptions.ShowBlockStructureId);
			if (newValue == enabled)
				return;
			enabled = newValue;
			if (enabled) {
				if (layer == null)
					layer = wpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.BlockStructure);
				if (editorFormatMap == null)
					editorFormatMap = editorFormatMapService.GetEditorFormatMap(wpfTextView);
				RegisterEvents();
				RefreshLinesAndColorInfos();
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

		sealed class StructureVisualizerDataComparer : IEqualityComparer<StructureVisualizerData> {
			public static readonly StructureVisualizerDataComparer Instance = new StructureVisualizerDataComparer();

			public bool Equals(StructureVisualizerData x, StructureVisualizerData y) =>
				x.BlockKind == y.BlockKind &&
				x.Top == y.Top &&
				x.Bottom == y.Bottom;

			public int GetHashCode(StructureVisualizerData obj) =>
				obj.Top.GetHashCode() ^ obj.Bottom.GetHashCode() ^ (int)obj.BlockKind;
		}

		void AddLineElements(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				return;
			var list = new List<StructureVisualizerData>();
			var updated = new HashSet<StructureVisualizerData>(StructureVisualizerDataComparer.Instance);
			foreach (var span in spans) {
				list.Clear();
				structureVisualizerServiceDataProvider.GetData(GetLineExtent(span), list);

				foreach (var info in list) {
					if (updated.Contains(info))
						continue;
					updated.Add(info);

					var lineElement = FindLineElement(info);
					if (lineElement != null) {
						layer.RemoveAdornment(lineElement);
						Debug.Assert(!lineElements.Contains(lineElement));
					}
					if (lineElement == null)
						lineElement = new LineElement(info);

					var lines = wpfTextView.TextViewLines.GetTextViewLinesIntersectingSpan(lineElement.Span);
					if (lines.Count == 0)
						continue;

					int lineStartIndex = 0;
					int lineEndIndex = lines.Count - 1;

					// Don't add a vertical line to the line containing the start or end block
					if (LineContainsSpan(info.Top, lines[lineStartIndex]))
						lineStartIndex++;
					if (LineContainsSpan(info.Bottom, lines[lineEndIndex]))
						lineEndIndex--;

					if (lineStartIndex > lineEndIndex)
						continue;

					double top = lines[lineStartIndex].Top;
					double bottom = lines[lineEndIndex].Bottom;
					if (bottom - top < 0.5)
						continue;
					double x = GetLineXPosition(info);
					var pen = GetPen(info.BlockKind);
					lineElement.Update(x, bottom, top, pen);

					bool added = layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, lineElement.Span, null, lineElement, onRemovedDelegate);
					if (added)
						lineElements.Add(lineElement);
				}
			}
		}

		static bool LineContainsSpan(SnapshotSpan point, ITextViewLine line) {
			SnapshotSpan span;
			if (line.IsFirstTextViewLineForSnapshotLine && line.IsLastTextViewLineForSnapshotLine)
				span = line.ExtentIncludingLineBreak;
			else
				span = line.Start.GetContainingLine().ExtentIncludingLineBreak;
			if (span.Start <= point.Start && (point.End < span.End || (point.End == span.End && line.IsLastDocumentLine())))
				return true;
			return false;
		}

		LineElement FindLineElement(StructureVisualizerData info) {
			foreach (var lineElement in lineElements) {
				if (StructureVisualizerDataComparer.Instance.Equals(lineElement.StructureVisualizerData, info))
					return lineElement;
			}
			return null;
		}

		static SnapshotSpan GetLineExtent(SnapshotSpan span) {
			if (span.Length == 0) {
				var line = span.Start.GetContainingLine();
				return line.ExtentIncludingLineBreak;
			}
			else {
				var startLine = span.Start.GetContainingLine();
				var endLine = span.End.GetContainingLine();
				if (endLine.Start == span.End)
					return new SnapshotSpan(startLine.Start, span.End);
				return new SnapshotSpan(startLine.Start, endLine.EndIncludingLineBreak);
			}
		}

		readonly AdornmentRemovedCallback onRemovedDelegate;
		void OnRemoved(object tag, UIElement element) => lineElements.Remove((LineElement)element);

		void UpdateRange(NormalizedSnapshotSpanCollection spans) => AddLineElements(spans);

		void RemoveAllLineElements() {
			lineElements.Clear();
			layer?.RemoveAllAdornments();
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
			case StructureVisualizerDataBlockKind.Module:		return ThemeClassificationTypeNameKeys.StructureVisualizerModule;
			case StructureVisualizerDataBlockKind.ValueType:	return ThemeClassificationTypeNameKeys.StructureVisualizerValueType;
			case StructureVisualizerDataBlockKind.Interface:	return ThemeClassificationTypeNameKeys.StructureVisualizerInterface;
			case StructureVisualizerDataBlockKind.Method:		return ThemeClassificationTypeNameKeys.StructureVisualizerMethod;
			case StructureVisualizerDataBlockKind.Accessor:		return ThemeClassificationTypeNameKeys.StructureVisualizerAccessor;
			case StructureVisualizerDataBlockKind.AnonymousMethod:return ThemeClassificationTypeNameKeys.StructureVisualizerAnonymousMethod;
			case StructureVisualizerDataBlockKind.Constructor:	return ThemeClassificationTypeNameKeys.StructureVisualizerConstructor;
			case StructureVisualizerDataBlockKind.Destructor:	return ThemeClassificationTypeNameKeys.StructureVisualizerDestructor;
			case StructureVisualizerDataBlockKind.Operator:		return ThemeClassificationTypeNameKeys.StructureVisualizerOperator;
			case StructureVisualizerDataBlockKind.Conditional:	return ThemeClassificationTypeNameKeys.StructureVisualizerConditional;
			case StructureVisualizerDataBlockKind.Loop:			return ThemeClassificationTypeNameKeys.StructureVisualizerLoop;
			case StructureVisualizerDataBlockKind.Property:		return ThemeClassificationTypeNameKeys.StructureVisualizerProperty;
			case StructureVisualizerDataBlockKind.Event:		return ThemeClassificationTypeNameKeys.StructureVisualizerEvent;
			case StructureVisualizerDataBlockKind.Try:			return ThemeClassificationTypeNameKeys.StructureVisualizerTry;
			case StructureVisualizerDataBlockKind.Catch:		return ThemeClassificationTypeNameKeys.StructureVisualizerCatch;
			case StructureVisualizerDataBlockKind.Filter:		return ThemeClassificationTypeNameKeys.StructureVisualizerFilter;
			case StructureVisualizerDataBlockKind.Finally:		return ThemeClassificationTypeNameKeys.StructureVisualizerFinally;
			case StructureVisualizerDataBlockKind.Fault:		return ThemeClassificationTypeNameKeys.StructureVisualizerFault;
			case StructureVisualizerDataBlockKind.Lock:			return ThemeClassificationTypeNameKeys.StructureVisualizerLock;
			case StructureVisualizerDataBlockKind.Using:		return ThemeClassificationTypeNameKeys.StructureVisualizerUsing;
			case StructureVisualizerDataBlockKind.Fixed:		return ThemeClassificationTypeNameKeys.StructureVisualizerFixed;
			case StructureVisualizerDataBlockKind.Switch:		return ThemeClassificationTypeNameKeys.StructureVisualizerSwitch;
			case StructureVisualizerDataBlockKind.Case:			return ThemeClassificationTypeNameKeys.StructureVisualizerCase;
			case StructureVisualizerDataBlockKind.LocalFunction:return ThemeClassificationTypeNameKeys.StructureVisualizerLocalFunction;
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

		sealed class LineElement : UIElement {
			public StructureVisualizerData StructureVisualizerData { get; }
			public SnapshotSpan Span => new SnapshotSpan(StructureVisualizerData.Top.Start, StructureVisualizerData.Bottom.End);
			double x;
			double top;
			double bottom;
			Pen pen;

			public LineElement(StructureVisualizerData info) {
				StructureVisualizerData = info;
			}

			protected override void OnRender(DrawingContext drawingContext) {
				base.OnRender(drawingContext);
				drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, bottom - top));
			}

			public void Update(double x, double bottom, double top, Pen pen) {
				Canvas.SetTop(this, top);
				this.x = x;
				this.bottom = bottom;
				this.top = top;
				this.pen = pen;
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
