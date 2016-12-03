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

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Enum value
	/// </summary>
	public sealed class EnumVM {
		readonly object value;
		readonly string name;

		/// <summary>
		/// Gets the value
		/// </summary>
		public object Value => value;

		/// <summary>
		/// Gets the name
		/// </summary>
		public string Name => name;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		public EnumVM(object value) {
			this.value = value;
			name = Enum.GetName(value.GetType(), value);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="name">Name</param>
		public EnumVM(object value, string name) {
			this.value = value;
			this.name = name;
		}

		/// <summary>
		/// Creates an array of <see cref="EnumVM"/>s
		/// </summary>
		/// <param name="enumType">Type of enum</param>
		/// <param name="values">Values that will be shown first</param>
		/// <returns></returns>
		public static EnumVM[] Create(Type enumType, params object[] values) => Create(true, enumType, values);

		/// <summary>
		/// Creates an array of <see cref="EnumVM"/>s
		/// </summary>
		/// <param name="sort">true to sort the array</param>
		/// <param name="enumType">Type of enum</param>
		/// <param name="values">Values that will be shown first</param>
		/// <returns></returns>
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

		/// <summary>
		/// Gets the name
		/// </summary>
		/// <returns></returns>
		public override string ToString() => name;
	}

	/// <summary>
	/// List of enum values
	/// </summary>
	public sealed class EnumListVM : ListVM<EnumVM> {
		/// <summary>
		/// Gets the selected item
		/// </summary>
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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="list">Initial value</param>
		public EnumListVM(IList<EnumVM> list)
			: this(list, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="list">Initial value</param>
		/// <param name="onChanged">Called when the selected item gets changed</param>
		public EnumListVM(IEnumerable<EnumVM> list, Action<int, int> onChanged)
			: base(list, onChanged) {
		}

		/// <summary>
		/// Checks whether the list contains a value
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public bool Has(object value) {
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Value.Equals(value))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Gets the index of the value. If it doesn't exist, it's automatically added to the list
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
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
