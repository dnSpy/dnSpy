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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.UI;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.CorDebug.Dialogs.DebugProgram {
	[Export(typeof(StartDebuggingOptionsPageProvider))]
	sealed class StartDebuggingOptionsPageProviderImpl : StartDebuggingOptionsPageProvider {
		readonly IPickFilename pickFilename;
		readonly IPickDirectory pickDirectory;

		[ImportingConstructor]
		StartDebuggingOptionsPageProviderImpl(IPickFilename pickFilename, IPickDirectory pickDirectory) {
			this.pickFilename = pickFilename;
			this.pickDirectory = pickDirectory;
		}

		public override IEnumerable<StartDebuggingOptionsPage> Create(StartDebuggingOptionsPageContext context) {
			yield return new DotNetFrameworkStartDebuggingOptionsPage(pickFilename, pickDirectory);
			yield return new DotNetCoreStartDebuggingOptionsPage(pickFilename, pickDirectory);
		}
	}
}
