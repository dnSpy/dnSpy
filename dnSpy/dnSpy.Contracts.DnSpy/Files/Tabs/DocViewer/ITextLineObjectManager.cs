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

namespace dnSpy.Contracts.Files.Tabs.DocViewer {
	/// <summary>
	/// Keeps track of text line objects, eg. code breakpoints, current line markers, etc
	/// </summary>
	public interface ITextLineObjectManager {
		/// <summary>
		/// Notified when the list has been modified
		/// </summary>
		event EventHandler<TextLineObjectListModifiedEventArgs> OnListModified;

		/// <summary>
		/// Gets all objects
		/// </summary>
		ITextLineObject[] Objects { get; }

		/// <summary>
		/// Finds an object of a certain type or returns null if none found
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <returns></returns>
		T[] GetObjectsOfType<T>() where T : ITextLineObject;

		/// <summary>
		/// Adds a new object
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		ITextLineObject Add(ITextLineObject obj);

		/// <summary>
		/// Removes an object
		/// </summary>
		/// <param name="obj">Object</param>
		void Remove(ITextLineObject obj);
	}
}
