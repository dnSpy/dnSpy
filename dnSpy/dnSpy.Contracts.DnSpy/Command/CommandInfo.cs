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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Command data
	/// </summary>
	public readonly struct CommandInfo {
		/// <summary>
		/// Gets the group
		/// </summary>
		public Guid Group { get; }

		/// <summary>
		/// Gets the command id
		/// </summary>
		public int ID { get; }

		/// <summary>
		/// Gets the arguments or null
		/// </summary>
		public object? Arguments { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="group">Command group, eg. <see cref="CommandConstants.StandardGroup"/></param>
		/// <param name="id">Command id</param>
		/// <param name="arguments">Command arguments or null</param>
		public CommandInfo(Guid group, int id, object? arguments = null) {
			Group = group;
			ID = id;
			Arguments = arguments;
		}
	}
}
