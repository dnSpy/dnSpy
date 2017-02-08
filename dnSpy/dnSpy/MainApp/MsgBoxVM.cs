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
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.MVVM;

namespace dnSpy.MainApp {
	sealed class MsgBoxVM : ViewModelBase {
		public ICommand OKCommand => new RelayCommand(a => listener(MsgBoxButton.OK));
		public ICommand YesCommand => new RelayCommand(a => listener(MsgBoxButton.Yes));
		public ICommand NoCommand => new RelayCommand(a => listener(MsgBoxButton.No));
		public ICommand CancelCommand => new RelayCommand(a => listener(MsgBoxButton.Cancel));
		public string Message { get; }

		public bool DontShowAgain {
			get { return dontShowAgain; }
			set {
				if (dontShowAgain != value) {
					dontShowAgain = value;
					OnPropertyChanged(nameof(DontShowAgain));
				}
			}
		}
		bool dontShowAgain;

		public bool HasDontShowAgain {
			get { return hasDontShowAgain; }
			set {
				if (hasDontShowAgain != value) {
					hasDontShowAgain = value;
					OnPropertyChanged(nameof(HasDontShowAgain));
				}
			}
		}
		bool hasDontShowAgain;

		public bool HasOKButton {
			get { return hasOKButton; }
			set {
				if (hasOKButton != value) {
					hasOKButton = value;
					OnPropertyChanged(nameof(HasOKButton));
				}
			}
		}
		bool hasOKButton;

		public bool HasYesButton {
			get { return hasYesButton; }
			set {
				if (hasYesButton != value) {
					hasYesButton = value;
					OnPropertyChanged(nameof(HasYesButton));
				}
			}
		}
		bool hasYesButton;

		public bool HasNoButton {
			get { return hasNoButton; }
			set {
				if (hasNoButton != value) {
					hasNoButton = value;
					OnPropertyChanged(nameof(HasNoButton));
				}
			}
		}
		bool hasNoButton;

		public bool HasCancelButton {
			get { return hasCancelButton; }
			set {
				if (hasCancelButton != value) {
					hasCancelButton = value;
					OnPropertyChanged(nameof(HasCancelButton));
				}
			}
		}
		bool hasCancelButton;

		readonly Action<MsgBoxButton> listener;

		public MsgBoxVM(string message, Action<MsgBoxButton> listener) {
			Message = message;
			this.listener = listener;
		}
	}
}
