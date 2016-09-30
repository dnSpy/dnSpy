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

using System.Windows.Media;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;

namespace dnSpy.Search {
	sealed class SearchTypeVM : ViewModelBase {
		public ImageSource Image {
			get {
				var options = new ImageOptions {
					BackgroundType = BackgroundType.ComboBox,
					Zoom = imageOptions.Zoom,
					DpiObject = imageOptions.DpiObject,
					Dpi = imageOptions.Dpi,
				};
				return imageService.GetImage(imageReference, options);
			}
		}

		readonly ImageReference imageReference;

		public string Name { get; }
		public string ToolTip { get; }
		public SearchType SearchType { get; }
		public VisibleMembersFlags Flags { get; }

		readonly IImageService imageService;
		ImageOptions imageOptions;

		public SearchTypeVM(IImageService imageService, ImageOptions imageOptions, SearchType searchType, string name, string toolTip, ImageReference imageReference, VisibleMembersFlags flags) {
			this.imageService = imageService;
			this.imageOptions = imageOptions;
			this.SearchType = searchType;
			this.Name = name;
			this.ToolTip = toolTip;
			this.imageReference = imageReference;
			this.Flags = flags;
		}

		public void RefreshUI(ImageOptions imageOptions) {
			this.imageOptions = imageOptions;
			OnPropertyChanged(nameof(Image));
			OnPropertyChanged(nameof(Name));
		}
	}
}
