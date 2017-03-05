/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// A class that can update a <see cref="DbgAppDomain"/>
	/// </summary>
	public abstract class DbgEngineAppDomain {
		/// <summary>
		/// Gets the app domain
		/// </summary>
		public abstract DbgAppDomain AppDomain { get; }

		/// <summary>
		/// Removes the app domain and disposes of it
		/// </summary>
		public abstract void Remove();

		/// <summary>
		/// Properties to update
		/// </summary>
		[Flags]
		public enum UpdateOptions {
			/// <summary>
			/// No option is enabled
			/// </summary>
			None				= 0,

			/// <summary>
			/// Update <see cref="DbgAppDomain.Name"/>
			/// </summary>
			Name				= 0x00000001,

			/// <summary>
			/// Update <see cref="DbgAppDomain.Id"/>
			/// </summary>
			Id					= 0x00000002,
		}

		/// <summary>
		/// Updates <see cref="DbgAppDomain.Name"/>
		/// </summary>
		/// <param name="name">New value</param>
		public void UpdateName(string name) => Update(UpdateOptions.Name, name: name);

		/// <summary>
		/// Updates <see cref="DbgAppDomain.Id"/>
		/// </summary>
		/// <param name="id">New value</param>
		public void UpdateId(int id) => Update(UpdateOptions.Id, id: id);

		/// <summary>
		/// Updates <see cref="DbgAppDomain"/> properties
		/// </summary>
		/// <param name="options">Options</param>
		/// <param name="name">New <see cref="DbgAppDomain.Name"/> value</param>
		/// <param name="id">New <see cref="DbgAppDomain.Id"/> value</param>
		public abstract void Update(UpdateOptions options, string name = null, int id = 0);
	}
}
