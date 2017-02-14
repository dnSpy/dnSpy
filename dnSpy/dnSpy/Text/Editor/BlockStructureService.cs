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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IBlockStructureServiceProvider))]
	sealed class BlockStructureServiceProvider : IBlockStructureServiceProvider {
		readonly IEditorFormatMapService editorFormatMapService;

		[ImportingConstructor]
		BlockStructureServiceProvider(IEditorFormatMapService editorFormatMapService) => this.editorFormatMapService = editorFormatMapService;

		public IBlockStructureService GetService(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			return wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(BlockStructureService), () => new BlockStructureService(wpfTextView, editorFormatMapService));
		}
	}

	sealed class BlockStructureService : IBlockStructureService {
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
		readonly LineColorInfo[] lineColorInfos;
		IAdornmentLayer layer;
		IEditorFormatMap editorFormatMap;
		IBlockStructureServiceDataProvider blockStructureServiceDataProvider;
		bool enabled;

		sealed class NullBlockStructureServiceDataProvider : IBlockStructureServiceDataProvider {
			public static readonly NullBlockStructureServiceDataProvider Instance = new NullBlockStructureServiceDataProvider();
			public void GetData(SnapshotSpan lineExtent, List<BlockStructureData> list) { }
		}

		public BlockStructureService(IWpfTextView wpfTextView, IEditorFormatMapService editorFormatMapService) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			this.editorFormatMapService = editorFormatMapService ?? throw new ArgumentNullException(nameof(editorFormatMapService));
			blockStructureServiceDataProvider = NullBlockStructureServiceDataProvider.Instance;
			onRemovedDelegate = OnRemoved;
			lineElements = new List<LineElement>();
			xPosCache = new XPosCache(wpfTextView);
			lineColorInfos = new LineColorInfo[TextColor.BlockStructureXaml - TextColor.BlockStructureNamespace + 1] {
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureNamespace),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureType),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureModule),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureValueType),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureInterface),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureMethod),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureAccessor),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureAnonymousMethod),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureConstructor),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureDestructor),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureOperator),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureConditional),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureLoop),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureProperty),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureEvent),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureTry),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureCatch),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureFilter),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureFinally),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureFault),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureLock),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureUsing),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureFixed),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureSwitch),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureCase),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureLocalFunction),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureOther),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureXml),
				new LineColorInfo(ThemeClassificationTypeNameKeys.BlockStructureXaml),
			};
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.Options.OptionChanged += Options_OptionChanged;
			UpdateEnabled();
		}

		sealed class LineColorInfo {
			public string Type { get; }
			public Pen Pen { get; set; }

			public LineColorInfo(string type) => Type = type;
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
		static Pen GetPen(ResourceDictionary props, BlockStructureLineKind lineKind) {
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

		static Pen InitializePen(Pen pen, BlockStructureLineKind lineKind) {
			switch (lineKind) {
			case BlockStructureLineKind.Solid:
				break;

			case BlockStructureLineKind.Dashed_1_1:
				pen.DashStyle = new DashStyle(dashed_1_1_DashStyle, 1);
				pen.DashCap = PenLineCap.Flat;
				break;

			case BlockStructureLineKind.Dashed_2_2:
				pen.DashStyle = new DashStyle(dashed_2_2_DashStyle, 1);
				pen.DashCap = PenLineCap.Flat;
				break;

			case BlockStructureLineKind.Dashed_3_3:
				pen.DashStyle = new DashStyle(dashed_3_3_DashStyle, 1);
				pen.DashCap = PenLineCap.Flat;
				break;

			case BlockStructureLineKind.Dashed_4_4:
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

		public void SetDataProvider(IBlockStructureServiceDataProvider dataProvider) {
			if (wpfTextView.IsClosed)
				return;
			blockStructureServiceDataProvider = dataProvider ?? NullBlockStructureServiceDataProvider.Instance;
			if (enabled) {
				ClearXPosCache();
				RepaintAllLines();
			}
		}

		void RepaintAllLines() {
			RemoveAllLineElements();
			UpdateRange(new NormalizedSnapshotSpanCollection(wpfTextView.TextViewLines.FormattedSpan));
		}

		sealed class BlockStructureDataComparer : IEqualityComparer<BlockStructureData> {
			public static readonly BlockStructureDataComparer Instance = new BlockStructureDataComparer();

			public bool Equals(BlockStructureData x, BlockStructureData y) =>
				x.BlockKind == y.BlockKind &&
				x.Top == y.Top &&
				x.Bottom == y.Bottom;

			public int GetHashCode(BlockStructureData obj) =>
				obj.Top.GetHashCode() ^ obj.Bottom.GetHashCode() ^ (int)obj.BlockKind;
		}

		void AddLineElements(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				return;
			var list = new List<BlockStructureData>();
			var updated = new HashSet<BlockStructureData>(BlockStructureDataComparer.Instance);
			foreach (var span in spans) {
				list.Clear();
				blockStructureServiceDataProvider.GetData(GetLineExtent(span), list);

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

		LineElement FindLineElement(BlockStructureData info) {
			foreach (var lineElement in lineElements) {
				if (BlockStructureDataComparer.Instance.Equals(lineElement.BlockStructureData, info))
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

		Pen GetPen(BlockStructureKind blockKind) => GetLineColorInfo(GetColorInfoType(blockKind)).Pen;

		LineColorInfo GetLineColorInfo(string type) {
			foreach (var info in lineColorInfos) {
				if (info.Type == type)
					return info;
			}
			Debug.Fail($"Unknown type: {type}");
			return lineColorInfos[0];
		}

		string GetColorInfoType(BlockStructureKind blockKind) {
			switch (blockKind) {
			case BlockStructureKind.Namespace:		return ThemeClassificationTypeNameKeys.BlockStructureNamespace;
			case BlockStructureKind.Type:			return ThemeClassificationTypeNameKeys.BlockStructureType;
			case BlockStructureKind.Module:			return ThemeClassificationTypeNameKeys.BlockStructureModule;
			case BlockStructureKind.ValueType:		return ThemeClassificationTypeNameKeys.BlockStructureValueType;
			case BlockStructureKind.Interface:		return ThemeClassificationTypeNameKeys.BlockStructureInterface;
			case BlockStructureKind.Method:			return ThemeClassificationTypeNameKeys.BlockStructureMethod;
			case BlockStructureKind.Accessor:		return ThemeClassificationTypeNameKeys.BlockStructureAccessor;
			case BlockStructureKind.AnonymousMethod:return ThemeClassificationTypeNameKeys.BlockStructureAnonymousMethod;
			case BlockStructureKind.Constructor:	return ThemeClassificationTypeNameKeys.BlockStructureConstructor;
			case BlockStructureKind.Destructor:		return ThemeClassificationTypeNameKeys.BlockStructureDestructor;
			case BlockStructureKind.Operator:		return ThemeClassificationTypeNameKeys.BlockStructureOperator;
			case BlockStructureKind.Conditional:	return ThemeClassificationTypeNameKeys.BlockStructureConditional;
			case BlockStructureKind.Loop:			return ThemeClassificationTypeNameKeys.BlockStructureLoop;
			case BlockStructureKind.Property:		return ThemeClassificationTypeNameKeys.BlockStructureProperty;
			case BlockStructureKind.Event:			return ThemeClassificationTypeNameKeys.BlockStructureEvent;
			case BlockStructureKind.Try:			return ThemeClassificationTypeNameKeys.BlockStructureTry;
			case BlockStructureKind.Catch:			return ThemeClassificationTypeNameKeys.BlockStructureCatch;
			case BlockStructureKind.Filter:			return ThemeClassificationTypeNameKeys.BlockStructureFilter;
			case BlockStructureKind.Finally:		return ThemeClassificationTypeNameKeys.BlockStructureFinally;
			case BlockStructureKind.Fault:			return ThemeClassificationTypeNameKeys.BlockStructureFault;
			case BlockStructureKind.Lock:			return ThemeClassificationTypeNameKeys.BlockStructureLock;
			case BlockStructureKind.Using:			return ThemeClassificationTypeNameKeys.BlockStructureUsing;
			case BlockStructureKind.Fixed:			return ThemeClassificationTypeNameKeys.BlockStructureFixed;
			case BlockStructureKind.Switch:			return ThemeClassificationTypeNameKeys.BlockStructureSwitch;
			case BlockStructureKind.Case:			return ThemeClassificationTypeNameKeys.BlockStructureCase;
			case BlockStructureKind.LocalFunction:	return ThemeClassificationTypeNameKeys.BlockStructureLocalFunction;
			case BlockStructureKind.Other:			return ThemeClassificationTypeNameKeys.BlockStructureOther;
			case BlockStructureKind.Xml:			return ThemeClassificationTypeNameKeys.BlockStructureXml;
			case BlockStructureKind.Xaml:			return ThemeClassificationTypeNameKeys.BlockStructureXaml;
			default:
				Debug.Fail($"Unknown block kind: {blockKind}");
				return ThemeClassificationTypeNameKeys.BlockStructureOther;
			}
		}

		double GetLineXPosition(BlockStructureData data) => xPosCache.GetXPosition(data);
		void ClearXPosCache() => xPosCache.Clear();
		readonly XPosCache xPosCache;

		sealed class XPosCache {
			readonly IWpfTextView wpfTextView;
			readonly Dictionary<int, double> toXPosDict;
			ITextSnapshot toXPosDictSnapshot;
			IFormattedLineSource formattedLineSource;

			public XPosCache(IWpfTextView wpfTextView) {
				this.wpfTextView = wpfTextView;
				toXPosDict = new Dictionary<int, double>();
			}

			public double GetXPosition(BlockStructureData data) {
				TryUpdateState();
				var topPoint = data.Top.Start.TranslateTo(toXPosDictSnapshot, PointTrackingMode.Negative);
				if (toXPosDict.TryGetValue(topPoint.Position, out double x))
					return x;

				var point = GetBlockStartPoint(topPoint, data.Bottom.Start.TranslateTo(toXPosDictSnapshot, PointTrackingMode.Negative));
				var line = wpfTextView.GetTextViewLineContainingBufferPosition(point);
				var bounds = line.GetExtendedCharacterBounds(point);
				x = Math.Round(bounds.Left + bounds.Width / 2 - PEN_THICKNESS / 2) + 0.5;
				toXPosDict[topPoint.Position] = x;
				return x;
			}

			SnapshotPoint GetBlockStartPoint(SnapshotPoint top, SnapshotPoint bottom) {
				var topPoint = GetPositionOfNonWhitespace(top, out int topColumn);
				var bottomPoint = GetPositionOfNonWhitespace(bottom, out int bottomColumn);
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
			public BlockStructureData BlockStructureData { get; }
			public SnapshotSpan Span => new SnapshotSpan(BlockStructureData.Top.Start, BlockStructureData.Bottom.End);
			double x;
			double top;
			double bottom;
			Pen pen;

			public LineElement(BlockStructureData info) => BlockStructureData = info;

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
			blockStructureServiceDataProvider = NullBlockStructureServiceDataProvider.Instance;
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.Options.OptionChanged -= Options_OptionChanged;
		}
	}
}
