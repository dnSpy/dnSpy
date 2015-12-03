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

using System;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Settings;

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// UI content shared by some <see cref="IFileTabContent"/> instances, eg. it could contain
	/// the text editor. Only one instance per tab is allocated and stored in a <see cref="WeakReference"/>.
	/// Implement <see cref="IDisposable"/> to get called when the tab is removed (only called if
	/// this instance hasn't been GC'd)
	/// </summary>
	public interface IFileTabUIContext {
		/// <summary>
		/// Gets the UI object
		/// </summary>
		object UIObject { get; }

		/// <summary>
		/// Gets the element that should get focus when the tab is selected, or null
		/// </summary>
		IInputElement FocusedElement { get; }

		/// <summary>
		/// Gets the element that gets the <see cref="ScaleTransform"/> or null if none
		/// </summary>
		FrameworkElement ScaleElement { get; }

		/// <summary>
		/// Saves UI state, eg. line number, caret position, etc
		/// </summary>
		/// <returns></returns>
		object Serialize();

		/// <summary>
		/// Restores UI state. <paramref name="obj"/> was created by <see cref="Serialize()"/> but
		/// could also be null or an invalid value. The callee is responsible for verifying
		/// <paramref name="obj"/>.
		/// </summary>
		/// <param name="obj">Serialized UI state</param>
		void Deserialize(object obj);

		/// <summary>
		/// Creates a serialized UI object, same type as returned by <see cref="Serialize()"/>.
		/// </summary>
		/// <param name="section">Serialized data</param>
		/// <returns></returns>
		object CreateSerialized(ISettingsSection section);

		/// <summary>
		/// Saves serialized data to <paramref name="section"/>. <paramref name="obj"/> was created
		/// by <see cref="Serialize()"/> but should be verified by the callee.
		/// </summary>
		/// <param name="section">Destination</param>
		/// <param name="obj">Serialized data, created by <see cref="Serialize()"/></param>
		void SaveSerialized(ISettingsSection section, object obj);

		/// <summary>
		/// Called when this instance will be shown in a tab
		/// </summary>
		void OnShow();

		/// <summary>
		/// Called when another <see cref="IFileTabUIContext"/> instance will be shown
		/// </summary>
		void OnHide();

		/// <summary>
		/// Initialized by the <see cref="IFileTabManager"/> owner
		/// </summary>
		IFileTab FileTab { get; set; }
	}
}
