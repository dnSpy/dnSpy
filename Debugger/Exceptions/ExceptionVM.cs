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

using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Debugger.Exceptions {
	interface IExceptionContext {
		IExceptionManager ExceptionManager { get; }
		bool SyntaxHighlight { get; }
	}

	sealed class ExceptionContext : IExceptionContext {
		public IExceptionManager ExceptionManager { get; private set; }
		public bool SyntaxHighlight { get; set; }

		public ExceptionContext(IExceptionManager exceptionManager) {
			this.ExceptionManager = exceptionManager;
		}
	}

	sealed class ExceptionVM : ViewModelBase {
		public object NameObject { get { return this; } }

		public bool BreakOnFirstChance {
			get { return info.BreakOnFirstChance; }
			set {
				if (info.BreakOnFirstChance != value) {
					info.BreakOnFirstChance = value;
					OnPropertyChanged("BreakOnFirstChance");
					Context.ExceptionManager.BreakOnFirstChanceChanged(info);
				}
			}
		}

		public string Name {
			get { return info.Name; }
		}

		public ExceptionInfo ExceptionInfo {
			get { return info; }
		}
		readonly ExceptionInfo info;

		public IExceptionContext Context {
			get { return context; }
		}
		readonly IExceptionContext context;

		public ExceptionVM(ExceptionInfo info, IExceptionContext context) {
			this.info = info;
			this.context = context;
		}

		internal void RefreshThemeFields() {
			OnPropertyChanged("NameObject");
		}
	}
}
