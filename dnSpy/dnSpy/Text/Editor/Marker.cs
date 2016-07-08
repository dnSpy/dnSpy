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
using System.Windows.Media;
using dnSpy.Text.WPF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class Marker {
		public Brush BackgroundBrush {
			get { return backgroundBrush; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (!BrushComparer.Equals(value, backgroundBrush)) {
					backgroundBrush = value;
					Debug.Assert(backgroundBrush.IsFrozen);
					foreach (var markedLine in toMarkedLine.Values)
						markedLine.BackgroundBrush = backgroundBrush;
				}
			}
		}
		Brush backgroundBrush;

		public Pen Pen {
			get { return pen; }
			set {
				if (pen != value) {
					pen = value;
					Debug.Assert(pen == null || pen.IsFrozen);
					foreach (var markedLine in toMarkedLine.Values)
						markedLine.Pen = pen;
				}
			}
		}
		Pen pen;

		readonly IWpfTextView textView;
		readonly IAdornmentLayer adornmentLayer;
		readonly Dictionary<object, MarkedLine> toMarkedLine;
		IMarkerSpanCollection markedSpans;
		readonly AdornmentRemovedCallback adornmentRemovedCallback;

		public Marker(IWpfTextView textView, IAdornmentLayer adornmentLayer) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (adornmentLayer == null)
				throw new ArgumentNullException(nameof(adornmentLayer));
			this.textView = textView;
			this.adornmentLayer = adornmentLayer;
			this.toMarkedLine = new Dictionary<object, MarkedLine>();
			this.markedSpans = NullMarkerSpanCollection.Instance;
			this.adornmentRemovedCallback = OnAdornmentRemoved;
		}

		sealed class MarkedLine : UIElement {
			public Brush BackgroundBrush {
				get { return backgroundBrush; }
				set {
					if (value == null)
						throw new ArgumentNullException(nameof(value));
					if (!BrushComparer.Equals(value, backgroundBrush)) {
						backgroundBrush = value;
						InvalidateVisual();
					}
				}
			}
			Brush backgroundBrush;

			public Pen Pen {
				get { return pen; }
				set {
					if (pen != value) {
						pen = value;
						InvalidateVisual();
					}
				}
			}
			Pen pen;

			public object IdentityTag { get; }
			public VirtualSnapshotSpan Span { get; }
			readonly Geometry geometry;

			public MarkedLine(VirtualSnapshotSpan span, Geometry geometry, object identityTag, Brush backgroundBrush, Pen pen) {
				if (span.Snapshot == null)
					throw new ArgumentException();
				if (geometry == null)
					throw new ArgumentNullException(nameof(geometry));
				if (identityTag == null)
					throw new ArgumentNullException(nameof(identityTag));
				if (backgroundBrush == null)
					throw new ArgumentNullException(nameof(backgroundBrush));
				Span = span;
				this.geometry = geometry;
				this.IdentityTag = identityTag;
				this.backgroundBrush = backgroundBrush;
				this.pen = pen;
			}

			protected override void OnRender(DrawingContext drawingContext) {
				base.OnRender(drawingContext);
				drawingContext.DrawGeometry(BackgroundBrush, Pen, geometry);
			}
		}

		public void SetSpans(IMarkerSpanCollection newMarkerSpans) {
			if (newMarkerSpans.IsEmpty) {
				RemoveAllAdornments();
				markedSpans = NullMarkerSpanCollection.Instance;
			}
			else {
				if (toMarkedLine.Count != 0) {
					var dict = new Dictionary<VirtualSnapshotSpan, MarkedLine>(toMarkedLine.Count, VirtualSnapshotSpanEqualityComparer.Instance);
					foreach (var markedLine in toMarkedLine.Values)
						dict.Add(markedLine.Span, markedLine);
					var infos = new List<MarkedLineInfo>();
					foreach (var markedSpan in newMarkerSpans.VisibleSpans) {
						foreach (var info in GetMarkedLineInfos(infos, markedSpan))
							dict.Remove(info.Span);
					}
					foreach (var markedLine in dict.Values)
						adornmentLayer.RemoveAdornment(markedLine);
				}
				markedSpans = newMarkerSpans;
			}
			AddMissingLines();
		}

		sealed class VirtualSnapshotSpanEqualityComparer : IEqualityComparer<VirtualSnapshotSpan> {
			public static readonly VirtualSnapshotSpanEqualityComparer Instance = new VirtualSnapshotSpanEqualityComparer();
			bool IEqualityComparer<VirtualSnapshotSpan>.Equals(VirtualSnapshotSpan x, VirtualSnapshotSpan y) => x == y;
			int IEqualityComparer<VirtualSnapshotSpan>.GetHashCode(VirtualSnapshotSpan obj) => obj.GetHashCode();
		}

		public void OnLayoutChanged(IList<ITextViewLine> newOrReformattedLines) {
			if (newOrReformattedLines.Count == 0)
				return;
			foreach (var line in newOrReformattedLines) {
				if (toMarkedLine.Count == 0)
					break;
				MarkedLine markedLine;
				if (toMarkedLine.TryGetValue(line.IdentityTag, out markedLine))
					adornmentLayer.RemoveAdornment(markedLine);
			}
			AddMissingLines();
		}

		void AddMissingLines() {
			if (markedSpans.IsEmpty)
				return;
			var infos = new List<MarkedLineInfo>();
			foreach (var markedSpan in markedSpans.VisibleSpans) {
				foreach (var info in GetMarkedLineInfos(infos, markedSpan)) {
					if (toMarkedLine.ContainsKey(info.Line.IdentityTag))
						continue;
					var geo = MarkerHelper.CreateGeometry(textView, info.Span, markedSpans.IsBoxMode, info.IsMultiLineSpan);
					if (geo == null)
						continue;
					var markedLine = new MarkedLine(info.Span, geo, info.Line.IdentityTag, BackgroundBrush, Pen);
					bool added = adornmentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, markedLine.Span.SnapshotSpan, markedLine, markedLine, adornmentRemovedCallback);
					if (added)
						toMarkedLine.Add(markedLine.IdentityTag, markedLine);
				}
			}
		}

		struct MarkedLineInfo {
			public ITextViewLine Line { get; }
			public VirtualSnapshotSpan Span { get; }
			public bool IsMultiLineSpan { get; }

			public MarkedLineInfo(ITextViewLine line, VirtualSnapshotSpan span, bool isMultiLineSpan) {
				Line = line;
				Span = span;
				IsMultiLineSpan = isMultiLineSpan;
			}
		}

		List<MarkedLineInfo> GetMarkedLineInfos(List<MarkedLineInfo> infos, VirtualSnapshotSpan markedSpan) {
			infos.Clear();
			var formattedSpan = textView.TextViewLines.FormattedSpan;
			bool useVspace = formattedSpan.End == markedSpan.End.Position && markedSpan.End.Position.Position == markedSpan.Snapshot.Length;
			var virtFormattedSpan = new VirtualSnapshotSpan(new VirtualSnapshotPoint(formattedSpan.Start), new VirtualSnapshotPoint(formattedSpan.End, useVspace ? markedSpan.End.VirtualSpaces : 0));
			var intersectionTmp = virtFormattedSpan.Intersection(markedSpan);
			if (intersectionTmp == null)
				return infos;
			var span = intersectionTmp.Value;

			bool isMultiLineSpan = MarkerHelper.IsMultiLineSpan(textView, span.SnapshotSpan);
			var pos = span.Start;
			int lines = 0;
			while (pos <= span.End) {
				if (lines > 0 && pos == span.End)
					break;
				var line = textView.TextViewLines.GetTextViewLineContainingBufferPosition(pos.Position);
				if (line == null)
					break;
				bool useVspace2 = useVspace && line.IsLastDocumentLine();
				var lineSpan = new VirtualSnapshotSpan(new VirtualSnapshotPoint(line.Start), new VirtualSnapshotPoint(line.EndIncludingLineBreak, useVspace2 ? markedSpan.End.VirtualSpaces : 0));
				var lineIntersectionTmp = lineSpan.Intersection(span);
				Debug.Assert(lineIntersectionTmp != null);
				if (lineIntersectionTmp != null)
					infos.Add(new MarkedLineInfo(line, lineIntersectionTmp.Value, isMultiLineSpan));

				if (line.IsLastDocumentLine())
					break;
				pos = new VirtualSnapshotPoint(line.EndIncludingLineBreak);
				lines++;
			}
			return infos;
		}

		void OnAdornmentRemoved(object tag, UIElement element) {
			var markedLine = (MarkedLine)tag;
			bool b = toMarkedLine.Remove(markedLine.IdentityTag);
			Debug.Assert(b);
		}

		void RemoveAllAdornments() {
			if (toMarkedLine.Count != 0) {
				foreach (var markedLine in toMarkedLine.Values.ToArray())
					adornmentLayer.RemoveAdornment(markedLine);
			}
			Debug.Assert(toMarkedLine.Count == 0);
		}

		public void Dispose() {
			RemoveAllAdornments();
			toMarkedLine.Clear();
		}
	}
}
