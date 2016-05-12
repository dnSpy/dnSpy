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
using System.Collections.Generic;
using System.Linq;
using dnSpy.Contracts.Text;

namespace dnSpy.Text {
	sealed class ContentType : IContentType {
		public IEnumerable<IContentType> BaseTypes {
			get {
				foreach (var bt in baseTypes)
					yield return bt;
			}
		}
		readonly IContentType[] baseTypes;

		public Guid Guid { get; }
		public string DisplayName { get; }

		public ContentType(Guid guid, string displayName, IEnumerable<IContentType> baseTypes) {
			Guid = guid;
			DisplayName = displayName;
			this.baseTypes = baseTypes.ToArray();
		}

		public bool IsOfType(string guid) => IsOfType(Guid.Parse(guid));

		public bool IsOfType(Guid guid) {
			if (Guid == guid)
				return true;

			foreach (var bt in baseTypes) {
				if (bt.IsOfType(guid))
					return true;
			}

			return false;
		}

		public override string ToString() => DisplayName;
	}
}
