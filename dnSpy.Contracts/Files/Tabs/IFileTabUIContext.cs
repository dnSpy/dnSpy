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

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// UI content shared by some <see cref="IFileTabContent"/> instances, eg. it could contain
	/// the text editor. Only one instance per tab is allocated and stored in a <see cref="WeakReference"/>.
	/// </summary>
	public interface IFileTabUIContext {
		/// <summary>
		/// Gets the UI object
		/// </summary>
		object UIObject { get; }

		/// <summary>
		/// Gets the element that should should get focus when the tab is selected, or null
		/// </summary>
		UIElement FocusedElement { get; }

		/// <summary>
		/// Save UI state, eg. line number, caret position, etc
		/// </summary>
		/// <returns></returns>
		object Serialize();

		/// <summary>
		/// Restore UI state. <paramref name="obj"/> was created by <see cref="Serialize()"/>
		/// </summary>
		/// <param name="obj">Serialized UI state</param>
		void Deserialize(object obj);

		/// <summary>
		/// Called before a new <see cref="IFileTabContent"/> is shown, even if the new instance
		/// will use this same instance.
		/// </summary>
		void Clear();

		/// <summary>
		/// Initialized by the <see cref="IFileTabManager"/> owner
		/// </summary>
		IFileTab FileTab { get; set; }
	}
}
