/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.ComponentModel;
using System.Diagnostics;

namespace dnSpy.Debugger.UI {
	abstract class FormatterObjectVMComparer<TVM> : IComparer<TVM>, IComparer where TVM : class {

		public readonly string VMPropertyName;
		public readonly ListSortDirection Direction;
		public string Tag;

		public FormatterObjectVMComparer(string vmPropertyName, ListSortDirection direction) {
			VMPropertyName = vmPropertyName;
			Direction = direction;
		}

		public int Compare(TVM x, TVM y) {
			if (x == null && y == null) return 0;
			if (x == null) return -1;
			if (y == null) return 1;

			if (String.IsNullOrEmpty(Tag) && !String.IsNullOrEmpty(VMPropertyName)) {
				// we get from view "ConditionObject". Translate to "Condition"
				// translate "ConditionObject" -> "Condition"

				Tag = TranslateVMPropertyToClassifierTags(VMPropertyName, x ?? y);
			}

			var c = CompareCore(x, y);
			return Direction == ListSortDirection.Descending ? c * -1 : c;
		}

		protected abstract int CompareCore(TVM x, TVM y);

		protected virtual string TranslateVMPropertyToClassifierTags(string vmPropertyName, TVM instance) {
			var formatter = typeof(TVM).GetProperty(VMPropertyName)
					?.GetGetMethod().Invoke(instance, null)
					as FormatterObject<TVM>;

			if (formatter == null)
				Debug.Fail($"Unknown vw property name: {VMPropertyName}");

			return formatter.Tag;
		}

		int IComparer.Compare(object x, object y) {
			return Compare(x as TVM, y as TVM);
		}
	}
}
