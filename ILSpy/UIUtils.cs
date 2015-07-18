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

using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy
{
	public static class UIUtils
	{
		public static DependencyObject GetParent(DependencyObject depo)
		{
			if (depo is Visual || depo is Visual3D)
				return VisualTreeHelper.GetParent(depo);
			else if (depo is FrameworkContentElement)
				return ((FrameworkContentElement)depo).Parent;
			return null;
		}

		public static T GetItem<T>(DependencyObject view, object o) where T : class
		{
			var depo = o as DependencyObject;
			while (depo != null && !(depo is T) && depo != view)
				depo = GetParent(depo);
			return depo as T;
		}

		public static bool IsLeftDoubleClick<T>(DependencyObject view, MouseButtonEventArgs e) where T : class
		{
			if (MouseButton.Left != e.ChangedButton)
				return false;
			return GetItem<T>(view, e.OriginalSource) != null;
		}

		public static string EscapeMenuItemHeader(string s)
		{
			return CleanUpName(s).Replace("_", "__");
		}

		public static string CleanUpName(string n)
		{
			if (n == null)
				return n;
			const int MAX_LEN = 0x100;
			if (n.Length > MAX_LEN)
				n = n.Substring(0, MAX_LEN);
			var sb = new StringBuilder(n.Length);
			for (int i = 0; i < n.Length; i++) {
				var c = n[i];
				if ((ushort)c < 0x20)
					c = '_';
				sb.Append(c);
			}
			return sb.ToString();
		}

		public static string CleanUpIdentifier(string id)
		{
			if (id == null)
				return id;
			id = IdentifierEscaper.Escape(id);
			return CleanUpName(id);
		}
	}
}
