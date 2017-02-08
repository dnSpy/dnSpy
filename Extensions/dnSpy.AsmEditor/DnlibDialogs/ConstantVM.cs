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

using System.ComponentModel;
using dnlib.DotNet;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class ConstantVM : ViewModelBase {
		public ConstantTypeVM ConstantTypeVM { get; }

		public object Value {
			get { return ConstantTypeVM.ValueNoSpecialNull; }
			set { ConstantTypeVM.Value = value; }
		}

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					OnPropertyChanged(nameof(IsEnabled));
					ConstantTypeVM.IsEnabled = value;
					HasErrorUpdated();
				}
			}
		}
		bool isEnabled = true;

		public string ConstantCheckBoxToolTip {
			get { return constantCheckBoxToolTip; }
			set {
				if (constantCheckBoxToolTip != value) {
					constantCheckBoxToolTip = value;
					OnPropertyChanged(nameof(ConstantCheckBoxToolTip));
				}
			}
		}
		string constantCheckBoxToolTip;

		static readonly ConstantType[] constantTypes = new ConstantType[] {
			ConstantType.Null,
			ConstantType.Boolean,
			ConstantType.Char,
			ConstantType.SByte,
			ConstantType.Byte,
			ConstantType.Int16,
			ConstantType.UInt16,
			ConstantType.Int32,
			ConstantType.UInt32,
			ConstantType.Int64,
			ConstantType.UInt64,
			ConstantType.Single,
			ConstantType.Double,
			ConstantType.String,
		};

		public ConstantVM(ModuleDef ownerModule, object value, string constantCheckBoxToolTip) {
			ConstantTypeVM = new ConstantTypeVM(ownerModule, value, constantTypes, true, false);
			ConstantCheckBoxToolTip = constantCheckBoxToolTip;
			ConstantTypeVM.PropertyChanged += ConstantTypeVM_PropertyChanged;

			IsEnabled = ConstantTypeVM.IsEnabled;
		}

		void ConstantTypeVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(ConstantTypeVM.Value))
				OnPropertyChanged(nameof(Value));
			else if (e.PropertyName == nameof(ConstantTypeVM.IsEnabled))
				IsEnabled = ConstantTypeVM.IsEnabled;
			HasErrorUpdated();
		}

		public override bool HasError {
			get {
				if (!IsEnabled)
					return false;

				return ConstantTypeVM.HasError;
			}
		}
	}
}
