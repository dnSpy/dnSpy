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
using dnSpy.Contracts.Languages;

namespace dnSpy.Languages.Settings {
	public sealed class DecompilerOption<T> : IDecompilerOption {
		public string Description { get; set; }
		public string Name { get; set; }

		public Guid Guid {
			get { return guid; }
		}
		readonly Guid guid;

		public Type Type {
			get { return typeof(T); }
		}

		public object Value {
			get { return getter(); }
			set { setter((T)value); }
		}
		readonly Func<T> getter;
		readonly Action<T> setter;

		public DecompilerOption(Guid guid, Func<T> getter, Action<T> setter) {
			if (getter == null || setter == null)
				throw new ArgumentNullException();
			this.guid = guid;
			this.getter = getter;
			this.setter = setter;
		}
	}
}
