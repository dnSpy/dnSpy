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
using System.Globalization;
using System.Windows.Data;
using dnSpy.Contracts.Images;

namespace dnSpy.Output {
	sealed class OutputControlConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as IOutputServiceInternal;
			var s = parameter as string;
			if (vm == null || s == null)
				return null;

			if (s == "ClearAllImage")
				return GetImage(vm, DsImages.ClearWindowContent);
			if (s == "SaveImage")
				return GetImage(vm, DsImages.Save);
			if (s == "ToggleWordWrapImage")
				return GetImage(vm, DsImages.WordWrap);

			return null;
		}

		object GetImage(IOutputServiceInternal vm, ImageReference imageReference) {
			if (vm.ImageOptions == null)
				return null;
			var options = new ImageOptions {
				BackgroundType = BackgroundType.CommandBar,
				Zoom = vm.ImageOptions.Zoom,
				DpiObject = vm.ImageOptions.DpiObject,
				Dpi = vm.ImageOptions.Dpi,
			};
			return vm.ImageService.GetImage(imageReference, options);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
