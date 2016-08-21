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

using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Exceptions {
	interface IExceptionContext {
		IExceptionManager ExceptionManager { get; }
		bool SyntaxHighlight { get; }
	}

	sealed class ExceptionContext : IExceptionContext {
		public IExceptionManager ExceptionManager { get; }
		public bool SyntaxHighlight { get; set; }

		public ExceptionContext(IExceptionManager exceptionManager) {
			this.ExceptionManager = exceptionManager;
		}
	}

	sealed class ExceptionVM : ViewModelBase {
		public bool BreakOnFirstChance {
			get { return ExceptionInfo.BreakOnFirstChance; }
			set {
				if (ExceptionInfo.BreakOnFirstChance != value) {
					ExceptionInfo.BreakOnFirstChance = value;
					OnPropertyChanged(nameof(BreakOnFirstChance));
					Context.ExceptionManager.BreakOnFirstChanceChanged(ExceptionInfo);
				}
			}
		}

		public object NameObject => this;
		public string Name => ExceptionInfo.Name;
		public ExceptionInfo ExceptionInfo { get; }
		public IExceptionContext Context { get; }

		public ExceptionVM(ExceptionInfo info, IExceptionContext context) {
			this.ExceptionInfo = info;
			this.Context = context;
		}

		internal void RefreshThemeFields() => OnPropertyChanged(nameof(NameObject));
	}
}
