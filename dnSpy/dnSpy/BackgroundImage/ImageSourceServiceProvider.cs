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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.BackgroundImage;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Themes;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.BackgroundImage {
	interface IImageSourceServiceProvider {
		IImageSourceService Create(IWpfTextView wpfTextView);
		IImageSourceService Create(WpfHexView wpfHexView);
	}

	[Export(typeof(IImageSourceServiceProvider))]
	sealed class ImageSourceServiceProvider : IImageSourceServiceProvider {
		readonly IThemeService themeService;
		readonly IBackgroundImageOptionDefinitionService backgroundImageOptionDefinitionService;
		readonly IBackgroundImageSettingsService backgroundImageSettingsService;
		readonly Dictionary<IBackgroundImageOptionDefinition, IImageSourceService> imageSourceServices;

		[ImportingConstructor]
		ImageSourceServiceProvider(IThemeService themeService, IBackgroundImageOptionDefinitionService backgroundImageOptionDefinitionService, IBackgroundImageSettingsService backgroundImageSettingsService) {
			this.themeService = themeService;
			this.backgroundImageOptionDefinitionService = backgroundImageOptionDefinitionService;
			this.backgroundImageSettingsService = backgroundImageSettingsService;
			imageSourceServices = new Dictionary<IBackgroundImageOptionDefinition, IImageSourceService>();
		}

		public IImageSourceService Create(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			return Create(backgroundImageOptionDefinitionService.GetOptionDefinition(wpfTextView));
		}

		public IImageSourceService Create(WpfHexView wpfHexView) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			return Create(backgroundImageOptionDefinitionService.GetOptionDefinition(wpfHexView));
		}

		IImageSourceService Create(Lazy<IBackgroundImageOptionDefinition, IBackgroundImageOptionDefinitionMetadata> lazy) {
			if (!imageSourceServices.TryGetValue(lazy.Value, out var imageSourceService))
				imageSourceServices.Add(lazy.Value, imageSourceService = new ImageSourceService(themeService, backgroundImageSettingsService.GetSettings(lazy)));
			return imageSourceService;
		}
	}
}
