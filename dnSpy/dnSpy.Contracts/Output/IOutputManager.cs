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
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Output {
	/// <summary>
	/// Output manager
	/// </summary>
	public interface IOutputManager {
		/// <summary>
		/// Creates a <see cref="IOutputTextPane"/>. Returns an existing one if it's already been
		/// created.
		/// </summary>
		/// <param name="guid">Guid of text pane</param>
		/// <param name="name">Name shown in the UI</param>
		/// <param name="contentType">Content type or null</param>
		/// <returns></returns>
		IOutputTextPane Create(Guid guid, string name, IContentType contentType = null);

		/// <summary>
		/// Creates a <see cref="IOutputTextPane"/>. Returns an existing one if it's already been
		/// created.
		/// </summary>
		/// <param name="guid">Guid of text pane</param>
		/// <param name="name">Name shown in the UI</param>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		IOutputTextPane Create(Guid guid, string name, string contentType);

		/// <summary>
		/// Returns a <see cref="IOutputTextPane"/>
		/// </summary>
		/// <param name="guid">Guid of text pane</param>
		/// <returns></returns>
		IOutputTextPane GetTextPane(Guid guid);

		/// <summary>
		/// Selects a <see cref="IOutputTextPane"/>
		/// </summary>
		/// <param name="guid">Guid of text pane</param>
		void Select(Guid guid);
	}
}
