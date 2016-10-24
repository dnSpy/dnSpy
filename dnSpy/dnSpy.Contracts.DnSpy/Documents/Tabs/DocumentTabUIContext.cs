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
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Settings;

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// UI content shared by some <see cref="DocumentTabContent"/> instances, eg. it could contain
	/// the text editor. Only one instance per tab is allocated and stored in a <see cref="WeakReference"/>.
	/// Implement <see cref="IDisposable"/> to get called when the tab is removed (only called if
	/// this instance hasn't been GC'd)
	/// </summary>
	public abstract class DocumentTabUIContext : IUIObjectProvider {
		/// <summary>
		/// Gets the UI object
		/// </summary>
		public abstract object UIObject { get; }

		/// <summary>
		/// Gets the element that gets focused or null
		/// </summary>
		public abstract IInputElement FocusedElement { get; }

		/// <summary>
		/// Gets the element that gets zoomed or null
		/// </summary>
		public abstract FrameworkElement ZoomElement { get; }

		/// <summary>
		/// Saves UI state, eg. line number, caret position, etc
		/// </summary>
		/// <returns></returns>
		public virtual object CreateUIState() => null;

		/// <summary>
		/// Restores UI state. <paramref name="obj"/> was created by <see cref="CreateUIState()"/> but
		/// could also be null or an invalid value. The callee is responsible for verifying
		/// <paramref name="obj"/>.
		/// </summary>
		/// <param name="obj">Serialized UI state</param>
		public virtual void RestoreUIState(object obj) { }

		/// <summary>
		/// Creates UI state from serialized data
		/// </summary>
		/// <param name="section">Serialized data</param>
		/// <returns></returns>
		public virtual object DeserializeUIState(ISettingsSection section) => null;

		/// <summary>
		/// Saves UI state to <paramref name="section"/>. <paramref name="obj"/> was created
		/// by <see cref="CreateUIState()"/> but should be verified by the callee.
		/// </summary>
		/// <param name="section">Destination</param>
		/// <param name="obj">UI state, created by <see cref="CreateUIState()"/></param>
		public virtual void SerializeUIState(ISettingsSection section, object obj) { }

		/// <summary>
		/// Called when this instance will be shown in a tab
		/// </summary>
		public virtual void OnShow() { }

		/// <summary>
		/// Called when another <see cref="DocumentTabUIContext"/> instance will be shown
		/// </summary>
		public virtual void OnHide() { }

		/// <summary>
		/// Gets the owner tab
		/// </summary>
		public IDocumentTab DocumentTab {
			get { return documentTab; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (documentTab == null)
					documentTab = value;
				else if (documentTab != value)
					throw new InvalidOperationException();
			}
		}
		IDocumentTab documentTab;
	}
}
