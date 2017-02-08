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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.ToolWindows.App;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.Memory {
	interface IMemoryWindowService {
		void Show(HexSpan span);
		void Show(HexSpan span, int windowIndex);
	}

	//[Export(typeof(IMemoryWindowService))]
	sealed class MemoryWindowService : IMemoryWindowService {
		readonly Lazy<MemoryToolWindowContentProvider> memoryToolWindowContentProvider;
		readonly IDsToolWindowService toolWindowService;

		[ImportingConstructor]
		MemoryWindowService(Lazy<MemoryToolWindowContentProvider> memoryToolWindowContentProvider, IDsToolWindowService toolWindowService) {
			this.memoryToolWindowContentProvider = memoryToolWindowContentProvider;
			this.toolWindowService = toolWindowService;
		}

		public void Show(HexSpan span) {
			var mc = GetMemoryToolWindowContent(span);
			if (mc == null)
				mc = memoryToolWindowContentProvider.Value.Contents[0].Content;
			ShowInMemoryWindow(mc, span);
		}

		public void Show(HexSpan span, int windowIndex) {
			var mc = GetMemoryToolWindowContent(windowIndex);
			Debug.Assert(mc != null);
			if (mc == null)
				return;
			ShowInMemoryWindow(mc, span);
		}

		void ShowInMemoryWindow(MemoryToolWindowContent mc, HexSpan span) {
			MakeSureAddressCanBeShown(mc, span);
			toolWindowService.Show(mc);
			SelectAndMoveCaret(mc.HexView, span);
		}

		static void SelectAndMoveCaret(WpfHexView hexView, HexSpan span) {
			if (!hexView.VisualElement.IsLoaded) {
				RoutedEventHandler loaded = null;
				loaded = (s, e) => {
					hexView.VisualElement.Loaded -= loaded;
					InitializeHexView(hexView, span);
				};
				hexView.VisualElement.Loaded += loaded;
			}
			else
				InitializeHexView(hexView, span);
		}

		static void InitializeHexView(HexView hexView, HexSpan span) {
			if (!IsVisible(hexView, span))
				return;
			var bufferSpan = new HexBufferSpan(hexView.Buffer, span);
			hexView.Selection.Select(bufferSpan.Start, bufferSpan.End, alignPoints: false);
			var column = hexView.Caret.IsValuesCaretPresent ? HexColumnType.Values : HexColumnType.Ascii;
			hexView.Caret.MoveTo(column, bufferSpan.Start);
			var flags = column == HexColumnType.Values ? HexSpanSelectionFlags.Values : HexSpanSelectionFlags.Ascii;
			hexView.ViewScroller.EnsureSpanVisible(bufferSpan, flags, VSTE.EnsureSpanVisibleOptions.ShowStart);
		}

		static bool IsVisible(HexView hexView, HexSpan span) =>
			span.Start >= hexView.BufferLines.StartPosition && span.End <= hexView.BufferLines.EndPosition;

		void MakeSureAddressCanBeShown(MemoryToolWindowContent mc, HexSpan span) {
			if (CanShowAll(mc, span))
				return;
			mc.HexView.Options.SetOptionValue(DefaultHexViewOptions.StartPositionId, mc.HexView.Buffer.Span.Start);
			mc.HexView.Options.SetOptionValue(DefaultHexViewOptions.EndPositionId, mc.HexView.Buffer.Span.End);
			RedisplayHexLines(mc.HexView);
		}

		static void RedisplayHexLines(HexView hexView) {
			var line = hexView.HexViewLines.FirstVisibleLine;
			var verticalDistance = line.Top - hexView.ViewportTop;
			var bufferPosition = line.BufferStart;
			hexView.DisplayHexLineContainingBufferPosition(bufferPosition, verticalDistance, VSTE.ViewRelativePosition.Top, null, null, DisplayHexLineOptions.CanRecreateBufferLines);
		}

		bool CanShowAll(MemoryToolWindowContent mc, HexSpan span) {
			if (span.Length == 0) {
				if (span.Start >= HexPosition.MaxEndPosition)
					return false;
				span = new HexSpan(span.Start, 1);
			}
			var hb = mc.HexView;
			return span.Start >= hb.BufferLines.StartPosition && span.End <= hb.BufferLines.EndPosition;
		}

		MemoryToolWindowContent GetMemoryToolWindowContent(HexSpan span) {
			foreach (var info in memoryToolWindowContentProvider.Value.Contents) {
				var mc = info.Content;
				if (CanShowAll(mc, span))
					return mc;
			}
			return null;
		}

		MemoryToolWindowContent GetMemoryToolWindowContent(int windowIndex) {
			if ((uint)windowIndex >= memoryToolWindowContentProvider.Value.Contents.Length)
				return null;
			return memoryToolWindowContentProvider.Value.Contents[windowIndex].Content;
		}
	}
}
