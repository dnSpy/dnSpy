﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dnSpy.Debugger.UI {
	public abstract class FormatterObjectVMComparer<TVM>
		: IComparer<TVM>, IComparer where TVM : class {


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

			var c = doCompare(x, y);
			return Direction == ListSortDirection.Descending ? c * -1 : c;
		}

		protected abstract int doCompare(TVM x, TVM y);

		protected virtual string TranslateVMPropertyToClassifierTags(string vmPropertyName, TVM instance) {
			var formatter = typeof(TVM).GetProperty(VMPropertyName)
					?.GetGetMethod().Invoke(instance, null)
					as FormatterObject<TVM>;

			if (formatter == null) {
				Debug.Fail($"Unknown vw property name: {VMPropertyName}");
			}

			return formatter.Tag;
		}

		int IComparer.Compare(object x, object y) {
			return Compare(x as TVM, y as TVM);
		}
	}
}
