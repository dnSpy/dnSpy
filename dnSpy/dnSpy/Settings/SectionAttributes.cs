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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace dnSpy.Settings {
	sealed class SectionAttributes {
		readonly Dictionary<string, string> attributes;

		public Tuple<string, string>[] Attributes => attributes.Select(a => Tuple.Create(a.Key, a.Value)).ToArray();

		public SectionAttributes() => attributes = new Dictionary<string, string>(StringComparer.Ordinal);

		public T Attribute<T>(string name) {
			Debug.Assert(name != null);
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			if (!attributes.TryGetValue(name, out string stringValue))
				return default(T);

			var c = TypeDescriptor.GetConverter(typeof(T));
			try {
				return (T)c.ConvertFromInvariantString(stringValue);
			}
			catch (FormatException) {
			}
			catch (NotSupportedException) {
			}
			return default(T);
		}

		public void Attribute<T>(string name, T value) {
			Debug.Assert(name != null);
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var c = TypeDescriptor.GetConverter(typeof(T));
			var stringValue = c.ConvertToInvariantString(value);
			attributes[name] = stringValue;
		}

		public void RemoveAttribute(string name) {
			Debug.Assert(name != null);
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			attributes.Remove(name);
		}
	}
}
