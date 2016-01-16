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
using dnSpy.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.MainApp {
	sealed class AskVM : ViewModelBase {

		public string LabelMessage {
			get { return labelMessage; }
		}
		readonly string labelMessage;

		public string Text {
			get { return text; }
			set {
				if (text != value) {
					text = value;
					OnPropertyChanged("Text");
					HasErrorUpdated();
				}
			}
		}
		string text = string.Empty;

		public object Value {
			get { return converter(Text); }
		}

		readonly Func<string, object> converter;
		readonly Func<string, string> verifier;

		public AskVM(string labelMessage, Func<string, object> converter, Func<string, string> verifier) {
			if (converter == null || verifier == null)
				throw new ArgumentNullException();
			this.labelMessage = labelMessage;
			this.converter = converter;
			this.verifier = verifier;
		}

		protected override string Verify(string columnName) {
			if (columnName == "Text") {
				try {
					var error = verifier(Text);
					if (!string.IsNullOrEmpty(error))
						return error;
					var v = Value;	// Make sure converter() works
					return string.Empty;
				}
				catch (Exception ex) {
					return string.Format(dnSpy_Resources.CantConvertInputToType, ex.Message);
				}
			}
			return string.Empty;
		}

		public override bool HasError {
			get { return !string.IsNullOrEmpty(Verify("Text")); }
		}
	}
}
