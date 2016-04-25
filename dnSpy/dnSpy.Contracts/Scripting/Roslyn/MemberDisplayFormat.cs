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

namespace dnSpy.Contracts.Scripting.Roslyn {
	/// <summary>
	/// Member display format (<see cref="T:Microsoft.CodeAnalysis.Scripting.Hosting.MemberDisplayFormat"/>)
	/// </summary>
	public enum MemberDisplayFormat {
		// IMPORTANT: Must be identical to Microsoft.CodeAnalysis.Scripting.Hosting.MemberDisplayFormat

		/// <summary>
		/// Display structure of the object on a single line.
		/// </summary>
		SingleLine,

		/// <summary>
		/// Displays a simple description of the object followed by list of members. Each member is displayed on a separate line.
		/// </summary>
		SeparateLines,

		/// <summary>
		/// Display just a simple description of the object, like type name or ToString(). Don't display any members of the object.
		/// </summary>
		Hidden,
	}
}
