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
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.Exceptions {
	interface IExceptionContext {
		IExceptionService ExceptionService { get; }
		IClassificationFormatMap ClassificationFormatMap { get; }
		ITextElementProvider TextElementProvider { get; }
		bool SyntaxHighlight { get; }
	}

	sealed class ExceptionContext : IExceptionContext {
		public IExceptionService ExceptionService { get; }
		public IClassificationFormatMap ClassificationFormatMap { get; }
		public ITextElementProvider TextElementProvider { get; }
		public bool SyntaxHighlight { get; set; }

		public ExceptionContext(IExceptionService exceptionService, IClassificationFormatMap classificationFormatMap, ITextElementProvider textElementProvider) {
			ExceptionService = exceptionService;
			ClassificationFormatMap = classificationFormatMap;
			TextElementProvider = textElementProvider;
		}
	}

	sealed class ExceptionVM : ViewModelBase {
		public bool BreakOnFirstChance {
			get { return ExceptionInfo.BreakOnFirstChance; }
			set {
				if (ExceptionInfo.BreakOnFirstChance != value) {
					ExceptionInfo.BreakOnFirstChance = value;
					OnPropertyChanged(nameof(BreakOnFirstChance));
					Context.ExceptionService.BreakOnFirstChanceChanged(ExceptionInfo);
				}
			}
		}

		public object NameObject => this;
		public string Name => ExceptionInfo.Name;
		public ExceptionInfo ExceptionInfo { get; }
		public IExceptionContext Context { get; }

		public ExceptionVM(ExceptionInfo info, IExceptionContext context) {
			ExceptionInfo = info;
			Context = context;
		}

		internal void RefreshThemeFields() => OnPropertyChanged(nameof(NameObject));
	}
}
