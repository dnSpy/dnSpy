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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.ILSpy.AsmEditor
{
	sealed class EnumVM
	{
		readonly object value;
		readonly string name;

		public object Value {
			get { return value; }
		}

		public string Name {
			get { return name; }
		}

		public EnumVM(object value)
		{
			this.value = value;
			this.name = Enum.GetName(value.GetType(), value);
		}

		public EnumVM(object value, string name)
		{
			this.value = value;
			this.name = name;
		}

		public static EnumVM[] Create(Type enumType, params object[] values)
		{
			var list = new List<EnumVM>();
			foreach (var value in enumType.GetEnumValues()) {
				if (values.Any(a => a.Equals(value)))
					continue;
				list.Add(new EnumVM(value));
			}
			list.Sort((a, b) => a.Name.ToUpperInvariant().CompareTo(b.Name.ToUpperInvariant()));
			for (int i = 0; i < values.Length; i++)
				list.Insert(i, new EnumVM(values[i]));
			return list.ToArray();
		}
	}

	sealed class EnumListVM : INotifyPropertyChanged
	{
		readonly ObservableCollection<EnumVM> list;
		readonly Action onChanged;
		int index;

		public IList<EnumVM> Items {
			get { return list; }
		}

		public int SelectedIndex {
			get { return index; }
			set {
				if (index != value) {
					Debug.Assert(value >= 0 && value < list.Count);
					index = value;
					OnPropertyChanged("SelectedIndex");
					OnPropertyChanged("SelectedItem");
					if (onChanged != null)
						onChanged();
				}
			}
		}

		public object SelectedItem {
			get {
				if (index < 0 || index >= list.Count)
					return null;
				return list[index].Value;
			}
			set {
				if (SelectedItem != value)
					SelectedIndex = GetIndex(value);
			}
		}

		public EnumListVM(IList<EnumVM> list, Action onChanged = null)
		{
			this.list = new ObservableCollection<EnumVM>(list);
			this.index = 0;
			this.onChanged = onChanged;
		}

		public int GetIndex(object value)
		{
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Value.Equals(value))
					return i;
			}

			list.Add(new EnumVM(value, string.Format("0x{0:X}", value)));
			return list.Count - 1;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}
}
