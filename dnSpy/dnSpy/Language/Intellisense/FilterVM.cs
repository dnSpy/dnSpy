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
using System.ComponentModel;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;

namespace dnSpy.Language.Intellisense {
	sealed class FilterVM : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsChecked {
			get { return filter.IsChecked; }
			set { filter.IsChecked = value; }
		}

		public bool IsEnabled {
			get { return filter.IsEnabled; }
			set { filter.IsEnabled = value; }
		}

		public ImageReference Image => filter.Image;
		public string ToolTip => filter.ToolTip;
		public string AccessKey => filter.AccessKey;

		readonly CompletionPresenter owner;
		readonly IIntellisenseFilter filter;

		public FilterVM(IIntellisenseFilter filter, CompletionPresenter owner) {
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			this.filter = filter;
			this.owner = owner;
			filter.PropertyChanged += Filter_PropertyChanged;
		}

		void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(filter.IsChecked)) {
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
				owner.OnIsCheckedChanged(this);
			}
			else if (e.PropertyName == nameof(filter.IsEnabled))
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
		}

		public void Dispose() => filter.PropertyChanged -= Filter_PropertyChanged;
	}
}
