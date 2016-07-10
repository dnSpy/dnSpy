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

using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;

namespace dnSpy.MainApp {
	sealed partial class MainWindow : MetroWindow {
		readonly IImageManager imageManager;

		public MainWindow(IThemeManager themeManager, IImageManager imageManager, object content) {
			this.imageManager = imageManager;
			InitializeComponent();
			this.contentPresenter.Content = content;
			UpdateSystemMenuImage();
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			this.Activated += (s, e) => UpdateSystemMenuImage();
			this.Deactivated += (s, e) => UpdateSystemMenuImage();
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close(), (s, e) => e.CanExecute = true));
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) => UpdateSystemMenuImage();

		void UpdateSystemMenuImage() {
			var bgType = IsActive ? BackgroundType.TitleAreaActive : BackgroundType.TitleAreaInactive;
			SystemMenuImage = imageManager.GetImage(new ImageReference(GetType().Assembly, "Assembly"), bgType);
		}
	}
}
