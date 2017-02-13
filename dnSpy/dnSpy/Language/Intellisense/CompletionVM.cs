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
using System.Windows.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionVM {
		public object ImageUIObject { get; }
		public object DisplayTextObject => this;
		public object SuffixObject => this;
		public Completion Completion { get; }

		public IEnumerable<CompletionIconVM> AttributeIcons => attributeIcons ?? (attributeIcons = CreateAttributeIcons());
		IEnumerable<CompletionIconVM> attributeIcons;
		readonly IImageMonikerService imageMonikerService;

		public CompletionVM(Completion completion, IImageMonikerService imageMonikerService) {
			if (imageMonikerService == null)
				throw new ArgumentNullException(nameof(imageMonikerService));
			Completion = completion ?? throw new ArgumentNullException(nameof(completion));
			Completion.Properties.AddProperty(typeof(CompletionVM), this);
			ImageUIObject = CreateImageUIObject(completion, imageMonikerService);
			this.imageMonikerService = imageMonikerService;
		}

		static object CreateImageUIObject(Completion completion, IImageMonikerService imageMonikerService) {
			var c3 = completion as Completion3;
			if (c3 == null) {
				var iconSource = completion.IconSource;
				if (iconSource == null)
					return null;
				return new Image {
					Width = 16,
					Height = 16,
					Source = iconSource,
				};
			}

			var imageReference = imageMonikerService.ToImageReference(c3.IconMoniker);
			if (imageReference.IsDefault)
				return null;
			return new DsImage { ImageReference = imageReference };
		}

		static object CreateImageUIObject(CompletionIcon icon, IImageMonikerService imageMonikerService) {
			var icon2 = icon as CompletionIcon2;
			if (icon2 == null) {
				var iconSource = icon.IconSource;
				if (iconSource == null)
					return null;
				return new Image {
					Width = 16,
					Height = 16,
					Source = iconSource,
				};
			}

			var imageReference = imageMonikerService.ToImageReference(icon2.IconMoniker);
			if (imageReference.IsDefault)
				return null;
			var image = new DsImage { ImageReference = imageReference };
			if (!((icon as IDsCompletionIcon)?.ThemeImage ?? false)) {
				DsImage.SetBackgroundColor(image, null);
				DsImage.SetBackgroundBrush(image, null);
			}
			return image;
		}

		public static CompletionVM TryGet(Completion completion) {
			if (completion == null)
				return null;
			if (completion.Properties.TryGetProperty(typeof(CompletionVM), out CompletionVM vm))
				return vm;
			return null;
		}

		IEnumerable<CompletionIconVM> CreateAttributeIcons() {
			var icons = (Completion as Completion2)?.AttributeIcons;
			if (icons == null)
				return Array.Empty<CompletionIconVM>();
			var list = new List<CompletionIconVM>();
			foreach (var icon in icons) {
				var imageUIObject = CreateImageUIObject(icon, imageMonikerService);
				if (imageUIObject != null)
					list.Add(new CompletionIconVM(imageUIObject));
			}
			return list;
		}
	}
}
