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
using System.ComponentModel;

namespace dnSpy.Shared.UI.MVVM {
	public abstract class ViewModelBase : INotifyPropertyChanged, IDataErrorInfo {
		public event PropertyChangedEventHandler PropertyChanged;

		protected bool HasPropertyChangedHandlers {
			get { return PropertyChanged != null; }
		}

		protected void OnPropertyChanged(string propName) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		protected void OnPropertyChanged(PropertyChangedEventArgs e) {
			if (PropertyChanged != null)
				PropertyChanged(this, e);
		}

		public string Error {
			get { throw new NotImplementedException(); }
		}

		public string this[string columnName] {
			get { return Verify(columnName); }
		}

		public virtual bool HasError {
			get { return false; }
		}

		protected virtual string Verify(string columnName) {
			return string.Empty;
		}

		protected void HasErrorUpdated() {
			OnPropertyChanged("HasError");
		}
	}
}
