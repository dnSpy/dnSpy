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

using System;
using System.ComponentModel;

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Base class of view models
	/// </summary>
	public abstract class ViewModelBase : INotifyPropertyChanged, IDataErrorInfo {
		/// <inheritdoc/>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propName">Name of property that got changed</param>
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="e">Changed event args</param>
		protected void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

		string IDataErrorInfo.Error { get { throw new NotImplementedException(); } }
		string IDataErrorInfo.this[string columnName] => Verify(columnName);

		/// <summary>
		/// true if there's an error
		/// </summary>
		public virtual bool HasError => false;

		/// <summary>
		/// Called to check if a property is valid. Returns null or an empty string if there's no error,
		/// else an error string that can be shown to the user
		/// </summary>
		/// <param name="columnName">Name of property</param>
		/// <returns></returns>
		protected virtual string Verify(string columnName) => string.Empty;

		/// <summary>
		/// Call this method if some property's error state changed
		/// </summary>
		protected void HasErrorUpdated() => OnPropertyChanged(nameof(HasError));
	}
}
