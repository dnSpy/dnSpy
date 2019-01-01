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

using System;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.BackgroundImage {
	sealed class HexViewBackgroundImageService : BackgroundImageService {
		readonly WpfHexView wpfHexView;
		HexAdornmentLayer adornmentLayer;

#pragma warning disable CS0169
		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.BackgroundImage)]
		[HexLayerKind(HexLayerKind.Underlay)]
		static HexAdornmentLayerDefinition backgroundImageAdornmentLayerDefinition;
#pragma warning restore CS0169

		HexViewBackgroundImageService(WpfHexView wpfHexView, IImageSourceService imageSourceService)
			: base(imageSourceService) {
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			Initialize();
			wpfHexView.Closed += WpfHexView_Closed;
		}

		void WpfHexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			if (e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth)
				UpdateImagePosition();
			else if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				UpdateImagePosition();
		}

		public static void InstallService(WpfHexView wpfHexView, IImageSourceService imageSourceService) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			if (imageSourceService == null)
				throw new ArgumentNullException(nameof(imageSourceService));
			wpfHexView.Properties.GetOrCreateSingletonProperty(typeof(BackgroundImageService), () => new HexViewBackgroundImageService(wpfHexView, imageSourceService));
		}

		protected override double ViewportWidth => wpfHexView.ViewportWidth;
		protected override double ViewportHeight => wpfHexView.ViewportHeight;

		protected override void OnEnabledCore() {
			if (adornmentLayer == null)
				adornmentLayer = wpfHexView.GetAdornmentLayer(PredefinedHexAdornmentLayers.BackgroundImage);
			wpfHexView.LayoutChanged += WpfHexView_LayoutChanged;
		}

		protected override void OnDisabledCore() {
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
			if (adornmentLayer != null)
				adornmentLayer.RemoveAllAdornments();
		}

		protected override void AddImageToAdornmentLayerCore(Image image) =>
			adornmentLayer.AddAdornment(VSTE.AdornmentPositioningBehavior.OwnerControlled, (HexBufferSpan?)null, null, image, null);

		void WpfHexView_Closed(object sender, EventArgs e) {
			ViewClosed();
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
		}
	}
}
