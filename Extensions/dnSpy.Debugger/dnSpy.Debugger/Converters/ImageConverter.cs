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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Windows.Data;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;

namespace dnSpy.Debugger.Converters {
	sealed class ImageConverter : IValueConverter {
		static IImageService imageService;

		[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforeExtensions)]
		sealed class Loader : IAutoLoaded {
			[ImportingConstructor]
			Loader(IImageService imageService) {
				ImageConverter.imageService = imageService;
			}
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var ary = ((string)parameter).Split(seps, 2);
			var bgType = (BackgroundType)Enum.Parse(typeof(BackgroundType), ary[0]);
			var asm = GetType().Assembly;
			return imageService.GetImage(new ImageReference(asm, ary[1]), bgType);
		}
		static readonly char[] seps = new char[1] { '_' };

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
