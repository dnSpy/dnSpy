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

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Controls;

namespace dnSpy.Contracts.ToolWindows {
	/// <summary>
	/// Tool window content. If any of the properties can change, you must implement <see cref="INotifyPropertyChanged"/>
	/// </summary>
	public abstract class ToolWindowContent : IUIObjectProvider {
		/// <summary>
		/// UI object; a WPF UI element or an object with a <see cref="DataTemplate"/>.
		/// If this property can change, you must implement <see cref="INotifyPropertyChanged"/>
		/// </summary>
		public abstract object? UIObject { get; }

		/// <summary>
		/// The element that gets focus or null if none, see also <see cref="IFocusable"/>
		/// </summary>
		public abstract IInputElement? FocusedElement { get; }

		/// <summary>
		/// Gets the element that gets the <see cref="ScaleTransform"/> or null if none,
		/// see also <see cref="IZoomableProvider"/> and <see cref="IZoomable"/>.
		/// If this property can change, you must implement <see cref="INotifyPropertyChanged"/>
		/// </summary>
		public abstract FrameworkElement? ZoomElement { get; }

		/// <summary>
		/// Gets the guid of this content
		/// </summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// Title. If this property can change, you must implement <see cref="INotifyPropertyChanged"/>
		/// </summary>
		public abstract string Title { get; }

		/// <summary>
		/// ToolTip or null. If this property can change, you must implement <see cref="INotifyPropertyChanged"/>
		/// </summary>
		public virtual object? ToolTip => null;

		/// <summary>
		/// Called when the visibility changes
		/// </summary>
		/// <param name="visEvent">Event</param>
		public virtual void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) { }
	}
}
