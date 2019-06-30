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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Text.Classification {
	sealed class ClassificationType : IClassificationType {
		public IEnumerable<IClassificationType> BaseTypes {
			get {
				foreach (var bt in baseTypes)
					yield return bt;
			}
		}
		readonly IClassificationType[] baseTypes;

		public string Classification { get; }

		public ClassificationType(string type, IEnumerable<IClassificationType> baseTypes) {
			Classification = type;
			this.baseTypes = baseTypes.ToArray();
		}

		public bool IsOfType(string type) {
			if (Classification == type)
				return true;

			foreach (var bt in baseTypes) {
				if (bt.IsOfType(type))
					return true;
			}

			return false;
		}

		public override string ToString() => Classification;
	}
}
