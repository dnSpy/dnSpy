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

using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class MethodDefVM : ViewModelBase {
		readonly MethodDef origMethod;

		public MethodDef Method {
			get { return method; }
			set {
				if (method != value) {
					method = value;
					OnPropertyChanged(nameof(Method));
					HasErrorUpdated();
				}
			}
		}
		MethodDef method;

		public string FullName {
			get {
				var md = Method;
				return md == null ? "null" : md.FullName;
			}
		}

		public MethodDefVM(MethodDef method) {
			origMethod = method;

			Reinitialize();
		}

		void Reinitialize() => Method = origMethod;

		protected override string Verify(string columnName) {
			if (columnName == nameof(Method)) {
				if (Method == null)
					return dnSpy_AsmEditor_Resources.MethodCantBeNull;
				return string.Empty;
			}

			return string.Empty;
		}

		public override bool HasError => Method != null;
	}
}
