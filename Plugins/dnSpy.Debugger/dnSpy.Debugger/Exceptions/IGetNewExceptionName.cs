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

using System.ComponentModel.Composition;
using dnSpy.Contracts.App;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Exceptions {
	interface IGetNewExceptionName {
		string GetName();
	}

	[Export(typeof(IGetNewExceptionName))]
	sealed class GetNewExceptionName : IGetNewExceptionName {
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		GetNewExceptionName(IMessageBoxManager messageBoxManager) {
			this.messageBoxManager = messageBoxManager;
		}

		public string GetName() {
			var res = messageBoxManager.Ask(dnSpy_Debugger_Resources.Exceptions_Add_Label, null,
				dnSpy_Debugger_Resources.Exceptions_Add_Title,
				s => s.Trim(),
				s => !string.IsNullOrWhiteSpace(s) ? null : dnSpy_Debugger_Resources.Exceptions_Add_Error_Name);
			return string.IsNullOrEmpty(res) ? null : res;
		}
	}
}
