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

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// The logical classification type of a span of text
	/// </summary>
	public interface IClassificationType {
		/// <summary>
		/// Gets all base types
		/// </summary>
		IEnumerable<IClassificationType> BaseTypes { get; }

		/// <summary>
		/// Gets the classification
		/// </summary>
		Guid Classification { get; }

		/// <summary>
		/// Gets the display name
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Returns true if derives from or is the specified classification type
		/// </summary>
		/// <param name="type">Classification type</param>
		/// <returns></returns>
		bool IsOfType(Guid type);

		/// <summary>
		/// Returns true if derives from or is the specified classification type
		/// </summary>
		/// <param name="type">Classification type</param>
		/// <returns></returns>
		bool IsOfType(string type);
	}
}
