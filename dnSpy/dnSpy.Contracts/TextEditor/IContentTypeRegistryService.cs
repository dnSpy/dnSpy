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
using System.Collections.Generic;

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// Content type registry service
	/// </summary>
	public interface IContentTypeRegistryService {
		/// <summary>
		/// Gets all content types
		/// </summary>
		IEnumerable<IContentType> ContentTypes { get; }

		/// <summary>
		/// Gets the unknown content type
		/// </summary>
		IContentType UnknownContentType { get; }

		/// <summary>
		/// Adds a new content type
		/// </summary>
		/// <param name="guid">Guid of content type</param>
		/// <param name="baseTypeGuids">Guids of all base content types</param>
		/// <returns></returns>
		IContentType AddContentType(string guid, IEnumerable<string> baseTypeGuids);

		/// <summary>
		/// Adds a new content type
		/// </summary>
		/// <param name="guid">Guid of content type</param>
		/// <param name="baseTypeGuids">Guids of all base content types</param>
		/// <returns></returns>
		IContentType AddContentType(Guid guid, IEnumerable<Guid> baseTypeGuids);

		/// <summary>
		/// Returns a content type or null if it wasn't found
		/// </summary>
		/// <param name="guid">Guid of content type</param>
		/// <returns></returns>
		IContentType GetContentType(string guid);

		/// <summary>
		/// Returns a content type or null if it wasn't found
		/// </summary>
		/// <param name="guid">Guid of content type</param>
		/// <returns></returns>
		IContentType GetContentType(Guid guid);

		/// <summary>
		/// Returns a content type or null if it wasn't found
		/// </summary>
		/// <param name="contentType">A content type guid, string, or <see cref="IContentType"/></param>
		/// <returns></returns>
		IContentType GetContentType(object contentType);
	}
}
