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
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Content shown in the options dialog box
	/// </summary>
	public abstract class AppSettingsPage {
		/// <summary>
		/// Parent <see cref="System.Guid"/> or <see cref="System.Guid.Empty"/> if the root element is the parent
		/// </summary>
		public abstract Guid ParentGuid { get; }

		/// <summary>
		/// Gets the <see cref="System.Guid"/>
		/// </summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// Gets the order, eg. <see cref="AppSettingsConstants.ORDER_DECOMPILER"/>
		/// </summary>
		public abstract double Order { get; }

		/// <summary>
		/// Gets the title shown in the UI
		/// </summary>
		public abstract string Title { get; }

		/// <summary>
		/// Gets the icon shown in the UI (eg. <see cref="DsImages.Assembly"/>) or <see cref="ImageReference.None"/>
		/// </summary>
		public abstract ImageReference Icon { get; }

		/// <summary>
		/// Gets the UI object
		/// </summary>
		public abstract object UIObject { get; }

		/// <summary>
		/// Called when all settings should be saved
		/// </summary>
		public abstract void OnApply();

		/// <summary>
		/// Called when the dialog box has been closed
		/// </summary>
		public virtual void OnClosed() {
		}

		/// <summary>
		/// Returns the object (or the <see cref="Type"/> of the object) with a <see cref="DataTemplate"/>
		/// that is shown in the UI. This <see cref="DataTemplate"/> is scanned for strings the
		/// user can search for.
		/// If this method doesn't return null, the value should equal <see cref="UIObject"/>.
		/// See also <see cref="GetSearchableStrings"/>.
		/// </summary>
		/// <returns></returns>
		public virtual object GetDataTemplateObject() => null;

		/// <summary>
		/// Returns an array of strings shown in the UI that can be searched. This method
		/// isn't needed if <see cref="GetDataTemplateObject"/> is overridden.
		/// </summary>
		/// <returns></returns>
		public virtual string[] GetSearchableStrings() => null;
	}

	/// <summary>
	/// Content shown in the options dialog box
	/// </summary>
	public interface IAppSettingsPage2 {
		/// <summary>
		/// Called when all settings should be saved. <see cref="AppSettingsPage.OnApply"/> is
		/// never called.
		/// </summary>
		/// <param name="appRefreshSettings">Add anything that needs to be refreshed, eg. re-decompile code</param>
		void OnApply(IAppRefreshSettings appRefreshSettings);
	}
}
