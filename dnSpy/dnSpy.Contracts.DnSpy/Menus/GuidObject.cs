/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// Object with a <see cref="System.Guid"/>
	/// </summary>
	public readonly struct GuidObject {
		/// <summary>Object</summary>
		public object Object { get; }

		/// <summary>Guid of object</summary>
		public Guid Guid { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="guid">Guid of object (eg. <see cref="MenuConstants.GUIDOBJ_WPF_TEXTVIEW_GUID"/>)</param>
		/// <param name="obj">Object</param>
		public GuidObject(string guid, object obj) {
			Object = obj;
			Guid = new Guid(guid);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="guid">Guid of object (eg. <see cref="MenuConstants.GUIDOBJ_WPF_TEXTVIEW_GUID"/>)</param>
		/// <param name="obj">Object</param>
		public GuidObject(Guid guid, object obj) {
			Object = obj;
			Guid = guid;
		}
	}
}
