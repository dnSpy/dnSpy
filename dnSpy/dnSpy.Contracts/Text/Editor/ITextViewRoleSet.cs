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

using System.Collections.Generic;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Text view role set
	/// </summary>
	public interface ITextViewRoleSet : IEnumerable<string> {
		/// <summary>
		/// Returns true if <paramref name="textViewRole"/> is a member of the set
		/// </summary>
		/// <param name="textViewRole">Text view role</param>
		/// <returns></returns>
		bool Contains(string textViewRole);

		/// <summary>
		/// Returns true if all of <paramref name="textViewRoles"/> are members of the set
		/// </summary>
		/// <param name="textViewRoles">Text view roles</param>
		/// <returns></returns>
		bool ContainsAll(IEnumerable<string> textViewRoles);

		/// <summary>
		/// Returns true if any of <paramref name="textViewRoles"/> are members of the set
		/// </summary>
		/// <param name="textViewRoles">Text view roles</param>
		/// <returns></returns>
		bool ContainsAny(IEnumerable<string> textViewRoles);

		/// <summary>
		/// Gets the union
		/// </summary>
		/// <param name="roleSet">Other text view role set</param>
		/// <returns></returns>
		ITextViewRoleSet UnionWith(ITextViewRoleSet roleSet);
	}
}
