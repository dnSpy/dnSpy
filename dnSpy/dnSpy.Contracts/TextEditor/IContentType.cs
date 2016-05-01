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
	/// Content type
	/// </summary>
	public interface IContentType {
		/// <summary>
		/// Gets all base types
		/// </summary>
		IEnumerable<IContentType> BaseTypes { get; }

		/// <summary>
		/// Gets the guid
		/// </summary>
		Guid Guid { get; }

		/// <summary>
		/// Gets the display name
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Returns true if derives from or is the specified content type
		/// </summary>
		/// <param name="guid">Content type guid</param>
		/// <returns></returns>
		bool IsOfType(Guid guid);

		/// <summary>
		/// Returns true if derives from or is the specified content type
		/// </summary>
		/// <param name="guid">Content type guid</param>
		/// <returns></returns>
		bool IsOfType(string guid);
	}
}
