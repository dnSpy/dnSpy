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
using dnSpy.Contracts.BackgroundImage;
using dnSpy.Contracts.Themes;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.BackgroundImage {
	interface IImageSourceServiceProvider {
		IImageSourceService Create(IWpfTextView wpfTextView);
	}

	[Export(typeof(IImageSourceServiceProvider))]
	sealed class ImageSourceServiceProvider : IImageSourceServiceProvider {
		readonly IThemeManager themeManager;
		readonly IBackgroundImageOptionDefinitionService backgroundImageOptionDefinitionService;
		readonly IBackgroundImageSettingsService backgroundImageSettingsService;
		readonly Dictionary<IBackgroundImageOptionDefinition, IImageSourceService> imageSourceServices;

		[ImportingConstructor]
		ImageSourceServiceProvider(IThemeManager themeManager, IBackgroundImageOptionDefinitionService backgroundImageOptionDefinitionService, IBackgroundImageSettingsService backgroundImageSettingsService) {
			this.themeManager = themeManager;
			this.backgroundImageOptionDefinitionService = backgroundImageOptionDefinitionService;
			this.backgroundImageSettingsService = backgroundImageSettingsService;
			this.imageSourceServices = new Dictionary<IBackgroundImageOptionDefinition, IImageSourceService>();
		}

		public IImageSourceService Create(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			var lazy = backgroundImageOptionDefinitionService.GetOptionDefinition(wpfTextView);
			IImageSourceService imageSourceService;
			if (!imageSourceServices.TryGetValue(lazy.Value, out imageSourceService))
				imageSourceServices.Add(lazy.Value, imageSourceService = new ImageSourceService(themeManager, backgroundImageSettingsService.GetSettings(lazy)));
			return imageSourceService;
		}
	}
}
