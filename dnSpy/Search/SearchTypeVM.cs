/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Shared.UI.MVVM;
using dnSpy.Shared.UI.Search;

namespace dnSpy.Search {
	sealed class SearchTypeVM : ViewModelBase {
		public ImageSource Image {
			get { return imageManager.GetImage(imageReference.Assembly, imageReference.Name, BackgroundType.ComboBox); }
		}
		readonly ImageReference imageReference;

		public string Name {
			get { return name; }
		}
		readonly string name;

		public string ToolTip {
			get { return toolTip; }
		}
		string toolTip;

		public SearchType SearchType {
			get { return searchType; }
		}
		readonly SearchType searchType;

		public VisibleMembersFlags Flags {
			get { return flags; }
		}
		readonly VisibleMembersFlags flags;

		readonly IImageManager imageManager;

		public SearchTypeVM(IImageManager imageManager, SearchType searchType, string name, string toolTip, string imageName, VisibleMembersFlags flags) {
			this.imageManager = imageManager;
			this.searchType = searchType;
			this.name = name;
			this.toolTip = toolTip;
			this.imageReference = new ImageReference(GetType().Assembly, imageName);
			this.flags = flags;
		}

		public void RefreshUI() {
			OnPropertyChanged("Image");
			OnPropertyChanged("Name");
		}
	}
}
