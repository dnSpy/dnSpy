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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Decompiler manager
	/// </summary>
	public interface IDecompilerManager {
		/// <summary>
		/// Gets all languages
		/// </summary>
		IEnumerable<IDecompiler> AllDecompilers { get; }

		/// <summary>
		/// Current default decompiler
		/// </summary>
		IDecompiler Decompiler { get; set; }

		/// <summary>
		/// Raised when <see cref="Decompiler"/> has been updated
		/// </summary>
		event EventHandler<EventArgs> DecompilerChanged;

		/// <summary>
		/// Finds a <see cref="IDecompiler"/> instance. null is returned if it wasn't found
		/// </summary>
		/// <param name="guid">Language guid, see <see cref="IDecompiler.UniqueGuid"/> and <see cref="IDecompiler.GenericGuid"/></param>
		/// <returns></returns>
		IDecompiler Find(Guid guid);

		/// <summary>
		/// Finds a <see cref="IDecompiler"/> instance. Returns the first one if the language wasn't found
		/// </summary>
		/// <param name="guid">Language guid, see <see cref="IDecompiler.UniqueGuid"/> and <see cref="IDecompiler.GenericGuid"/></param>
		/// <returns></returns>
		IDecompiler FindOrDefault(Guid guid);
	}
}
