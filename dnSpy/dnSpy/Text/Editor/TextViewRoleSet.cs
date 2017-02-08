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
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class TextViewRoleSet : ITextViewRoleSet {
		readonly HashSet<string> roles;

		public TextViewRoleSet(IEnumerable<string> textViewRoles) {
			if (textViewRoles == null)
				throw new ArgumentNullException(nameof(textViewRoles));
			roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in textViewRoles) {
				if (s == null)
					throw new ArgumentNullException(nameof(textViewRoles));
				roles.Add(s.ToUpperInvariant());// VS returns upper case strings
			}
		}

		TextViewRoleSet(HashSet<string> roles) {
			this.roles = roles;
		}

		public bool Contains(string textViewRole) {
			if (textViewRole == null)
				throw new ArgumentNullException(nameof(textViewRole));
			return roles.Contains(textViewRole);
		}

		public bool ContainsAll(IEnumerable<string> textViewRoles) {
			if (textViewRoles == null)
				throw new ArgumentNullException(nameof(textViewRoles));
			foreach (var s in textViewRoles) {
				if (!roles.Contains(s))
					return false;
			}
			return true;
		}

		public bool ContainsAny(IEnumerable<string> textViewRoles) {
			if (textViewRoles == null)
				throw new ArgumentNullException(nameof(textViewRoles));
			foreach (var s in textViewRoles) {
				if (roles.Contains(s))
					return true;
			}
			return false;
		}

		public ITextViewRoleSet UnionWith(ITextViewRoleSet roleSet) {
			if (roleSet == null)
				throw new ArgumentNullException(nameof(roleSet));
			if (this == roleSet)
				return this;
			return new TextViewRoleSet(new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase));
		}

		public IEnumerator<string> GetEnumerator() => roles.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public override string ToString() => string.Join(",", roles);
	}
}
