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

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// WPF command manager
	/// </summary>
	public interface IWpfCommandManager {
		/// <summary>
		/// Adds an element. The element is stored in a <see cref="WeakReference"/>
		/// </summary>
		/// <param name="guid">Guid, eg. <see cref="CommandConstants.GUID_MAINWINDOW"/></param>
		/// <param name="elem">Element</param>
		void Add(Guid guid, UIElement elem);

		/// <summary>
		/// Removes an element
		/// </summary>
		/// <param name="guid">Guid, eg. <see cref="CommandConstants.GUID_MAINWINDOW"/></param>
		/// <param name="elem">Element</param>
		void Remove(Guid guid, UIElement elem);

		/// <summary>
		/// Gets a <see cref="IWpfCommands"/> instance
		/// </summary>
		/// <param name="guid">Guid, eg. <see cref="CommandConstants.GUID_MAINWINDOW"/></param>
		/// <returns></returns>
		IWpfCommands GetCommands(Guid guid);
	}
}
