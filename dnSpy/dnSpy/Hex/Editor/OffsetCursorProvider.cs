/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

// Changes the cursor when it hovers over the offset column

using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(HexMouseProcessorProvider))]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	[VSUTIL.Name("Offset Mouse Cursor Provider")]
	sealed class OffsetHexMouseProcessorProvider : HexMouseProcessorProvider {
		readonly OffsetCursorProviderService offsetCursorProviderService;

		[ImportingConstructor]
		OffsetHexMouseProcessorProvider(OffsetCursorProviderService offsetCursorProviderService) => this.offsetCursorProviderService = offsetCursorProviderService;

		public override HexMouseProcessor? GetAssociatedProcessor(WpfHexView wpfHexView) =>
			new OffsetHexMouseProcessor(offsetCursorProviderService.Get(wpfHexView), wpfHexView);
	}

	sealed class OffsetHexMouseProcessor : HexMouseProcessor {
		readonly OffsetCursorProvider offsetCursorProvider;
		readonly WpfHexView wpfHexView;

		public OffsetHexMouseProcessor(OffsetCursorProvider offsetCursorProvider, WpfHexView wpfHexView) {
			this.offsetCursorProvider = offsetCursorProvider ?? throw new ArgumentNullException(nameof(offsetCursorProvider));
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
		}

		HexMouseLocation GetMouseLocation(MouseEventArgs e) => HexMouseLocation.Create(wpfHexView, e, insertionPosition: false);

		public override void PostprocessMouseMove(MouseEventArgs e) => UpdateMouse(e);
		public override void PostprocessMouseEnter(MouseEventArgs e) => UpdateMouse(e);

		void UpdateMouse(MouseEventArgs e) {
			if (!wpfHexView.BufferLines.ShowOffset)
				return;
			var loc = GetMouseLocation(e);
			if (wpfHexView.BufferLines.OffsetSpan.Contains(loc.Position))
				offsetCursorProvider.CursorInfo = new HexCursorInfo(Cursors.Arrow, PredefinedHexCursorPriorities.Offset);
			else
				ResetCursorInfo();
		}

		void ResetCursorInfo() => offsetCursorProvider.CursorInfo = default;

		public override void PostprocessMouseLeave(MouseEventArgs e) => ResetCursorInfo();
	}

	[Export(typeof(HexCursorProviderFactory))]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	sealed class OffsetHexCursorProviderFactory : HexCursorProviderFactory {
		readonly OffsetCursorProviderService offsetCursorProviderService;

		[ImportingConstructor]
		OffsetHexCursorProviderFactory(OffsetCursorProviderService offsetCursorProviderService) => this.offsetCursorProviderService = offsetCursorProviderService;

		public override HexCursorProvider? Create(WpfHexView wpfHexView) =>
			new OffsetHexCursorProvider(offsetCursorProviderService.Get(wpfHexView));
	}

	sealed class OffsetHexCursorProvider : HexCursorProvider {
		public override event EventHandler? CursorInfoChanged;
		public override HexCursorInfo CursorInfo => offsetCursorProvider.CursorInfo;
		readonly OffsetCursorProvider offsetCursorProvider;

		public OffsetHexCursorProvider(OffsetCursorProvider offsetCursorProvider) {
			this.offsetCursorProvider = offsetCursorProvider ?? throw new ArgumentNullException(nameof(offsetCursorProvider));
			offsetCursorProvider.CursorInfoChanged += OffsetCursorProvider_CursorInfoChanged;
		}

		void OffsetCursorProvider_CursorInfoChanged(object? sender, EventArgs e) => CursorInfoChanged?.Invoke(this, EventArgs.Empty);
	}

	abstract class OffsetCursorProviderService {
		public abstract OffsetCursorProvider Get(WpfHexView wpfHexView);
	}

	[Export(typeof(OffsetCursorProviderService))]
	sealed class OffsetCursorProviderServiceImpl : OffsetCursorProviderService {
		public override OffsetCursorProvider Get(WpfHexView wpfHexView) =>
			wpfHexView.Properties.GetOrCreateSingletonProperty(typeof(OffsetCursorProvider), () => new OffsetCursorProvider());
	}

	sealed class OffsetCursorProvider {
		public event EventHandler? CursorInfoChanged;

		public HexCursorInfo CursorInfo {
			get => cursorInfo;
			set {
				if (cursorInfo != value) {
					cursorInfo = value;
					CursorInfoChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		HexCursorInfo cursorInfo;
	}
}
