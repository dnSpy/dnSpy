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

namespace dnSpy.Contracts.Debugger.Exceptions {
	/// <summary>
	/// Exception group definition
	/// </summary>
	public struct DbgExceptionGroupDefinition {
		/// <summary>
		/// Gets the flags
		/// </summary>
		public DbgExceptionGroupDefinitionFlags Flags { get; }

		/// <summary>
		/// Name of group, see also <see cref="PredefinedExceptionGroups"/>
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Localized name shown in the UI
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <param name="name">Name of group, see also <see cref="PredefinedExceptionGroups"/></param>
		/// <param name="displayName">Localized name shown in the UI</param>
		public DbgExceptionGroupDefinition(DbgExceptionGroupDefinitionFlags flags, string name, string displayName) {
			Flags = flags;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
		}

		/// <summary>
		/// Returns <see cref="DisplayName"/>
		/// </summary>
		/// <returns></returns>
		public override string ToString() => DisplayName;
	}
}
