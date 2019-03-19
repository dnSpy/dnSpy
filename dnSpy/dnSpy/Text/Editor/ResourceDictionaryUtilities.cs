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

using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Text.Editor {
	static class ResourceDictionaryUtilities {
		public static Brush GetForegroundBrush(ResourceDictionary dict, Brush defaultBrush = null) => GetBrush(dict, EditorFormatDefinition.ForegroundBrushId, defaultBrush);
		public static Brush GetBackgroundBrush(ResourceDictionary dict, Brush defaultBrush = null) => GetBrush(dict, EditorFormatDefinition.BackgroundBrushId, defaultBrush);

		public static Brush GetBrush(ResourceDictionary dict, string prop, Brush defaultBrush = null) {
			var brush = dict[prop] as Brush ?? defaultBrush;
			if (brush != null && brush.CanFreeze)
				brush.Freeze();
			return brush;
		}
	}
}
