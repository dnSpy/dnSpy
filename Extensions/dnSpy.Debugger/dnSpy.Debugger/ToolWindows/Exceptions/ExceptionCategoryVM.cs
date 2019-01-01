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
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	sealed class ExceptionCategoryVM : ViewModelBase {
		public string DisplayName { get; }
		public string ShortDisplayName { get; }
		public DbgExceptionCategoryDefinition? Definition { get; }

		public ExceptionCategoryVM(string displayName) {
			Definition = null;
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
			ShortDisplayName = DisplayName;
		}

		public ExceptionCategoryVM(DbgExceptionCategoryDefinition definition) {
			Definition = definition;
			DisplayName = definition.DisplayName;
			ShortDisplayName = definition.ShortDisplayName;
		}
	}
}
