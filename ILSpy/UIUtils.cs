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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ICSharpCode.ILSpy
{
	public static class UIUtils
	{
		static T GetItem<T>(DependencyObject view, object o) where T : class
		{
			var depo = o as DependencyObject;
			while (depo != null && !(depo is T) && depo != view)
				depo = VisualTreeHelper.GetParent(depo);
			return depo as T;
		}

		public static bool IsLeftDoubleClick<T>(DependencyObject view, MouseButtonEventArgs e) where T : class
		{
			if (MouseButton.Left != e.ChangedButton)
				return false;
			return GetItem<T>(view, e.OriginalSource) != null;
		}
	}
}
