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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.CallStack {
	sealed class CallStackFrameColumnConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = value as ICallStackFrameVM;
			if (vm == null)
				return null;
			var s = parameter as string;
			if (s == null)
				return null;

			if (StringComparer.OrdinalIgnoreCase.Equals(s, "Image")) {
				if (vm.Index == 0)
					return GetImage(vm, DsImages.CurrentInstructionPointer);
				if (vm.IsCurrentFrame)
					return GetImage(vm, DsImages.CallReturnInstructionPointer);
				return null;
			}

			var gen = ColorizedTextElementProvider.Create(vm.Context.SyntaxHighlight);
			if (StringComparer.OrdinalIgnoreCase.Equals(s, "Name"))
				CreateContent(gen.Output, vm.CachedOutput, vm.Context.SyntaxHighlight);
			else
				return null;

			return gen.CreateResult(true);
		}

		object GetImage(ICallStackFrameVM vm, ImageReference imageReference) {
			if (vm.Context.ImageOptions == null)
				return null;
			var options = new ImageOptions {
				BackgroundType = BackgroundType.GridViewItem,
				Zoom = vm.Context.ImageOptions.Zoom,
				DpiObject = vm.Context.ImageOptions.DpiObject,
				Dpi = vm.Context.ImageOptions.Dpi,
			};
			return vm.Context.ImageService.GetImage(imageReference, options);
		}

		void CreateContent(ITextColorWriter output, CachedOutput cachedOutput, bool highlight) {
			var conv = new OutputConverter(output);
			foreach (var t in cachedOutput.data)
				conv.Write(t.Item1, t.Item2);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
