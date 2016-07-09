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

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Decompiler option
	/// </summary>
	public interface IDecompilerOption {
		/// <summary>
		/// Guid, eg. <see cref="DecompilerOptionConstants.ShowILComments_GUID"/>
		/// </summary>
		Guid Guid { get; }

		/// <summary>
		/// Name or null, eg. <see cref="DecompilerOptionConstants.ShowILComments_NAME"/>
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Description or null
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Type
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// Gets/sets the value
		/// </summary>
		object Value { get; set; }
	}
}
