/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Language.Intellisense {
	sealed class FilterVM : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public ImageReference ImageReference { get; }

		public bool IsChecked {
			get { return filter.IsChecked; }
			set {
				if (filter.IsChecked != value) {
					filter.IsChecked = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
					owner.OnIsCheckedChanged(this);
				}
			}
		}

		public bool IsEnabled {
			get { return filter.IsEnabled; }
			set {
				if (filter.IsEnabled != value) {
					filter.IsEnabled = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
				}
			}
		}

		public string ToolTip => filter.ToolTip;
		public string AccessKey => filter.AccessKey;

		readonly CompletionPresenter owner;
		readonly IIntellisenseFilter filter;

		public FilterVM(IIntellisenseFilter filter, CompletionPresenter owner, ImageReference imageReference) {
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			this.filter = filter;
			this.owner = owner;
			ImageReference = imageReference;
		}

		public void Dispose() { }
	}
}
