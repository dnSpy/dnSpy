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
using dnSpy.Contracts.Documents.Tabs.DocViewer;

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Creates and caches <see cref="IDocumentTabUIContext"/> instances. These are only used in a
	/// single tab.
	/// </summary>
	public interface IDocumentTabUIContextLocator {
		/// <summary>
		/// Creates or returns an existing cached instance of a certain type. This instance is
		/// cached per tab and is stored in either a <see cref="WeakReference"/> or a strong
		/// reference (see <see cref="ExportDocumentTabUIContextProviderAttribute.UseStrongReference"/>)
		/// </summary>
		/// <typeparam name="T">Type, eg. <see cref="IDocumentViewer"/>. There must be an exported
		/// <see cref="IDocumentTabUIContextProvider"/> that can create the type.</typeparam>
		/// <returns></returns>
		T Get<T>() where T : class, IDocumentTabUIContext;

		/// <summary>
		/// Creates or returns an existing cached instance of a certain type. This instance is
		/// cached per tab and is stored in a <see cref="WeakReference"/>.
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <param name="key">Key</param>
		/// <param name="creator">Called if the value hasn't been cached or if it has been GC'd</param>
		/// <returns></returns>
		T Get<T>(object key, Func<T> creator) where T : class, IDocumentTabUIContext;

		/// <summary>
		/// Creates or returns an existing cached instance of a certain type. This instance is
		/// cached per tab and is stored in a <see cref="WeakReference"/> or a strong reference
		/// depending on the value of <paramref name="useStrongReference"/>.
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <param name="key">Key</param>
		/// <param name="useStrongReference">true to store the result in a strong reference instead of a weak reference</param>
		/// <param name="creator">Called if the value hasn't been cached or if it has been GC'd</param>
		/// <returns></returns>
		T Get<T>(object key, bool useStrongReference, Func<T> creator) where T : class, IDocumentTabUIContext;
	}
}
