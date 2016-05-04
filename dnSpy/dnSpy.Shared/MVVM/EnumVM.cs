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

namespace dnSpy.Shared.MVVM {
	public sealed class EnumVM {
		readonly object value;
		readonly string name;

		public object Value => value;
		public string Name => name;

		public EnumVM(object value) {
			this.value = value;
			this.name = Enum.GetName(value.GetType(), value);
		}

		public EnumVM(object value, string name) {
			this.value = value;
			this.name = name;
		}

		public static EnumVM[] Create(Type enumType, params object[] values) => Create(true, enumType, values);

		public static EnumVM[] Create(bool sort, Type enumType, params object[] values) {
			var list = new List<EnumVM>();
			foreach (var value in enumType.GetEnumValues()) {
				if (values.Any(a => a.Equals(value)))
					continue;
				list.Add(new EnumVM(value));
			}
			if (sort)
				list.Sort((a, b) => StringComparer.InvariantCultureIgnoreCase.Compare(a.Name, b.Name));
			for (int i = 0; i < values.Length; i++)
				list.Insert(i, new EnumVM(values[i]));
			return list.ToArray();
		}

		public override string ToString() => name;
	}

	public sealed class EnumListVM : ListVM<EnumVM> {
		public new object SelectedItem {
			get {
				if (Index < 0 || Index >= list.Count)
					return null;
				return list[Index].Value;
			}
			set {
				if (!object.Equals(SelectedItem, value))
					SelectedIndex = GetIndex(value);
			}
		}

		public EnumListVM(IList<EnumVM> list)
			: this(list, null) {
		}

		public EnumListVM(IEnumerable<EnumVM> list, Action<int, int> onChanged)
			: base(list, onChanged) {
		}

		public bool Has(object value) {
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Value.Equals(value))
					return true;
			}
			return false;
		}

		public int GetIndex(object value) {
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Value.Equals(value))
					return i;
			}

			list.Add(new EnumVM(value, $"0x{value:X}"));
			return list.Count - 1;
		}
	}
}
