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
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.SaveModule {
	abstract class SaveOptionsVM : ViewModelBase {
		public abstract SaveOptionsType Type { get; }
		public abstract object UndoDocument { get; }

		public string FileName {
			get { return filename; }
			set {
				filename = value ?? throw new ArgumentNullException(nameof(value));
				OnPropertyChanged(nameof(FileName));
				HasErrorUpdated();
			}
		}
		string filename = string.Empty;

		public IPickSaveFilename PickSaveFilename {
			set { pickSaveFilename = value; }
		}
		IPickSaveFilename pickSaveFilename;

		public ICommand PickNetExecutableFileNameCommand => new RelayCommand(a => OnPickNetExecutableFileName());

		void OnPickNetExecutableFileName() {
			if (pickSaveFilename == null)
				throw new InvalidOperationException();
			var newFileName = pickSaveFilename.GetFilename(FileName, GetExtension(FileName), PickFilenameConstants.DotNetAssemblyOrModuleFilter);
			if (newFileName == null)
				return;
			FileName = newFileName;
		}

		protected abstract string GetExtension(string filename);

		protected override string Verify(string columnName) {
			if (columnName == nameof(FileName))
				return filename.ValidateFileName() ?? string.Empty;

			return string.Empty;
		}

		public override bool HasError {
			get {
				if (!string.IsNullOrEmpty(Verify(nameof(FileName))))
					return true;

				return false;
			}
		}
	}
}
