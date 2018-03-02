/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Helper extensions for Wpf Elements
	/// </summary>
	public static class WpfExtensions {

		#region Is 

		/// <summary>
		/// Checks if a FrameworkElement matches a given condition
		/// </summary>
		public static bool Is(this DependencyObject @this, Func<DependencyObject, bool> predicate) {
			return predicate(@this);
		}

		/// <summary>
		/// Checks if a FrameworkElement matches a given condition
		/// </summary>
		public static bool Is<T>(this DependencyObject @this, Func<T, bool> predicate) where T : DependencyObject {
			var x = @this as T;
			return x != null && predicate(x);
		}

		#endregion

		#region Query

		/// <summary>
		/// Returns a list of items which matches a given condition
		/// </summary>
		public static IEnumerable<T> Query<T>(this DependencyObject @this, QueryMode mode, Func<T, bool> predicate = null) where T : DependencyObject {
			if (predicate == null) predicate = x => true;

			var children = GetChildren(@this, mode);
			foreach (var child in children) {
				if (child.Is<T>(predicate))
					yield return (T)child;
			}

			foreach (var child in children) {
				foreach (var descendant in child.Query<T>(mode, predicate)) {
					yield return descendant;
				}
			}
		}

		/// <summary>
		/// Returns a list of items which matches a given condition
		/// </summary>
		public static IEnumerable<T> Query<T>(this DependencyObject @this, Func<T, bool> predicate = null) where T : DependencyObject {
			return @this.Query<T>(QueryMode.Visual, predicate);
		}

		/// <summary>
		/// QueryMode
		/// </summary>
		public enum QueryMode {
			/// <summary>
			/// Uses VisualTreeHelper
			/// </summary>
			Visual,
			/// <summary>
			/// Uses LogicalTreeHelper
			/// </summary>
			Logical
		}

		private static IEnumerable<DependencyObject> GetChildren(DependencyObject @this, QueryMode mode) {
			if (mode == QueryMode.Logical) {
				var children = LogicalTreeHelper.GetChildren(@this).OfType<DependencyObject>();
				foreach (var child in children) {
					yield return child;
				}
			}
			else {
				var childrenCount = VisualTreeHelper.GetChildrenCount(@this);
				for (var i = 0; i < childrenCount; ++i) {
					yield return VisualTreeHelper.GetChild(@this, i);
				}
			}
		}


		#endregion
	}
}
