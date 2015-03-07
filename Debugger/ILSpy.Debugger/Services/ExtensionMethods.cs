// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger.Services
{
	/// <summary>
	/// Extension methods used in ILSpy debugger.
	/// </summary>
	static class ExtensionMethods
	{
		#region System.Drawing <-> WPF conversions
		public static Point TransformFromDevice(this Point point, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
			return new Point(point.X * matrix.M11, point.Y * matrix.M22);
		}
		#endregion
	}
	
	/// <summary>
	/// Scrolling related helpers.
	/// </summary>
	public static class ScrollUtils
	{
		/// <summary>
		/// Searches VisualTree of given object for a ScrollViewer.
		/// </summary>
		public static ScrollViewer GetScrollViewer(this DependencyObject o)
		{
			var scrollViewer = o as ScrollViewer;
			if (scrollViewer != null)	{
				return scrollViewer;
			}

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
			{
				var child = VisualTreeHelper.GetChild(o, i);
				var result = GetScrollViewer(child);
				if (result != null) {
					return result;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Scrolls ScrollViewer up by given offset.
		/// </summary>
		/// <param name="offset">Offset by which to scroll up. Should be positive.</param>
		public static void ScrollUp(this ScrollViewer scrollViewer, double offset)
		{
			ScrollUtils.ScrollByVerticalOffset(scrollViewer, -offset);
		}
		
		/// <summary>
		/// Scrolls ScrollViewer down by given offset.
		/// </summary>
		/// <param name="offset">Offset by which to scroll down. Should be positive.</param>
		public static void ScrollDown(this ScrollViewer scrollViewer, double offset)
		{
			ScrollUtils.ScrollByVerticalOffset(scrollViewer, offset);
		}
		
		/// <summary>
		/// Scrolls ScrollViewer by given vertical offset.
		/// </summary>
		public static void ScrollByVerticalOffset(this ScrollViewer scrollViewer, double offset)
		{
			scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + offset);
		}
	}
}
