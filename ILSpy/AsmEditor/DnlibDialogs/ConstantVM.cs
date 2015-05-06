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

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class ConstantVM : ViewModelBase
	{
		public ConstantTypeVM ConstantTypeVM {
			get { return constantTypeVM; }
		}
		ConstantTypeVM constantTypeVM;

		public object Value {
			get { return ConstantTypeVM.ValueNoSpecialNull; }
			set { ConstantTypeVM.Value = value; }
		}

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					OnPropertyChanged("IsEnabled");
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
					OnPropertyChanged("ConstantCheckBoxToolTip");
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

		public ConstantVM(object value, string constantCheckBoxToolTip)
		{
			this.constantTypeVM = new ConstantTypeVM(value, constantTypes, true, false);
			this.ConstantCheckBoxToolTip = constantCheckBoxToolTip;
			this.ConstantTypeVM.PropertyChanged += ConstantTypeVM_PropertyChanged;

			IsEnabled = ConstantTypeVM.IsEnabled;
		}

		void ConstantTypeVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Value")
				OnPropertyChanged("Value");
			else if (e.PropertyName == "IsEnabled")
				IsEnabled = ConstantTypeVM.IsEnabled;
			HasErrorUpdated();
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
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
