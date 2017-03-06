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
using System.Windows.Controls;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.BackgroundImage {
	sealed class TextViewBackgroundImageService : BackgroundImageService {
		readonly IWpfTextView wpfTextView;
		IAdornmentLayer adornmentLayer;

#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedDsAdornmentLayers.BackgroundImage)]
		[LayerKind(LayerKind.Underlay)]
		static AdornmentLayerDefinition backgroundImageAdornmentLayerDefinition;
#pragma warning restore 0169

		TextViewBackgroundImageService(IWpfTextView wpfTextView, IImageSourceService imageSourceService)
			: base(imageSourceService) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			Initialize();
			wpfTextView.Closed += WpfTextView_Closed;
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth)
				UpdateImagePosition();
			else if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				UpdateImagePosition();
		}

		public static void InstallService(IWpfTextView wpfTextView, IImageSourceService imageSourceService) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (imageSourceService == null)
				throw new ArgumentNullException(nameof(imageSourceService));
			wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(BackgroundImageService), () => new TextViewBackgroundImageService(wpfTextView, imageSourceService));
		}

		protected override double ViewportWidth => wpfTextView.ViewportWidth;
		protected override double ViewportHeight => wpfTextView.ViewportHeight;

		protected override void OnEnabledCore() {
			if (adornmentLayer == null)
				adornmentLayer = wpfTextView.GetAdornmentLayer(PredefinedDsAdornmentLayers.BackgroundImage);
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
		}

		protected override void OnDisabledCore() {
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			if (adornmentLayer != null)
				adornmentLayer.RemoveAllAdornments();
		}

		protected override void AddImageToAdornmentLayerCore(Image image) =>
			adornmentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, image, null);

		void WpfTextView_Closed(object sender, EventArgs e) {
			ViewClosed();
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
		}
	}
}
