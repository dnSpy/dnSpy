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

		#region Up

		/// <summary>
		/// Returns a parent object out of the parent hierarchy which matches a given condition
		/// </summary>
		/// <returns>The found parent or null</returns>
		/// <example>
		/// Button myButton;
		/// Window myWindow = myButton.Up&lt;Window&gt;();
		/// </example>
		public static T Up<T>(this FrameworkElement @this, Func<T, bool> predicate = null) where T : FrameworkElement {
			if (@this == null) throw new ArgumentNullException(nameof(@this));
			if (predicate == null) predicate = x => true;

			FrameworkElement current = @this;
			while ((current = current.Up()) != null) {
				if (@this.Is<T>(predicate)) { return (T)current; }
			}

			return null;
		}

		/// <summary>
		/// Returns the parent of the actual element. Same as this.Parent;
		/// </summary>
		/// <returns>The parent or null</returns>
		public static FrameworkElement Up(this FrameworkElement @this) {
			if (@this == null) throw new ArgumentNullException(nameof(@this));

			return @this.Parent as FrameworkElement;
		}

		#endregion

		#region Down

		/// <summary>
		/// Returns the first child which matches a given condition.
		/// </summary>
		public static T Down<T>(this DependencyObject @this, QueryMode mode, Func<T, bool> predicate = null) where T : DependencyObject {
			if (predicate == null) predicate = x => true;

			return @this.Query<T>(mode, predicate).FirstOrDefault();
		}

		/// <summary>
		/// Returns the first child which matches a given condition.
		/// </summary>
		public static T Down<T>(this DependencyObject @this, Func<T, bool> predicate = null) where T : DependencyObject {
			return @this.Down<T>(QueryMode.Visual, predicate);
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
				if (child.Is<T>(predicate)) {
					yield return (T)child;
				}
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
