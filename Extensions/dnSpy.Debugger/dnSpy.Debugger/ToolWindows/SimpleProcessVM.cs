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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.ToolWindows {
	sealed class SimpleProcessVM : ViewModelBase {
		public string Name { get; private set; }
		public DbgProcess? Process { get; }
		public SimpleProcessVM(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));
		public SimpleProcessVM(DbgProcess process, bool useHex) {
			Process = process ?? throw new ArgumentNullException(nameof(process));
			Name = GetProcessName(process, useHex);
		}
		static string GetProcessName(DbgProcess process, bool useHex) =>
			"[" + (useHex ? "0x" + process.Id.ToString("X") : process.Id.ToString()) + "]" + (string.IsNullOrEmpty(process.Name) ? string.Empty : " " + process.Name);
		public void UpdateName(bool useHex) {
			if (Process is not null) {
				var newName = GetProcessName(Process, useHex);
				if (Name != newName) {
					Name = newName;
					OnPropertyChanged(nameof(Name));
				}
			}
		}
	}
}
