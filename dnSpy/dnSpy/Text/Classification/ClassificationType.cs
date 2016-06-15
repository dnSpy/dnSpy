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
using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Text.Classification {
	sealed class ClassificationType : IClassificationType {
		public IEnumerable<IClassificationType> BaseTypes {
			get {
				foreach (var bt in baseTypes)
					yield return bt;
			}
		}
		readonly IClassificationType[] baseTypes;

		public Guid Classification { get; }
		public string DisplayName { get; }

		public ClassificationType(Guid type, string displayName, IEnumerable<IClassificationType> baseTypes) {
			Classification = type;
			DisplayName = displayName;
			this.baseTypes = baseTypes.ToArray();
		}

		public bool IsOfType(string type) => IsOfType(Guid.Parse(type));
		public bool IsOfType(Guid type) {
			if (Classification == type)
				return true;

			foreach (var bt in baseTypes) {
				if (bt.IsOfType(type))
					return true;
			}

			return false;
		}

		public override string ToString() => DisplayName;
	}
}
