/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// A reference in code
	/// </summary>
	public sealed class CodeReferenceSegment {
		/// <summary>
		/// The reference or null
		/// </summary>
		public object Reference;

		/// <summary>
		/// true if it's a local, parameter, or label
		/// </summary>
		public bool IsLocal;

		/// <summary>
		/// true if it's the target of a click, eg. the definition of a type, method, etc
		/// </summary>
		public bool IsLocalTarget;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ref">Referece</param>
		/// <param name="isLocal">See <see cref="IsLocal"/></param>
		/// <param name="isLocalTarget"><see cref="IsLocalTarget"/></param>
		public CodeReferenceSegment(object @ref, bool isLocal = false, bool isLocalTarget = false) {
			this.Reference = @ref;
			this.IsLocal = isLocal;
			this.IsLocalTarget = isLocalTarget;
		}
	}
}
