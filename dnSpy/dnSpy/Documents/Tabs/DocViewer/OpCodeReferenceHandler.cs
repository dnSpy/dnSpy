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

using System.ComponentModel.Composition;
using System.Diagnostics;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Properties;

namespace dnSpy.Documents.Tabs.DocViewer {
	[ExportReferenceHandler]
	sealed class OpCodeReferenceHandler : IReferenceHandler {
		const string msdnUrlFormat = "https://msdn.microsoft.com/library/system.reflection.emit.opcodes.{0}.aspx";
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		OpCodeReferenceHandler(IMessageBoxService messageBoxService) => this.messageBoxService = messageBoxService;

		public bool OnFollowReference(IReferenceHandlerContext context) {
			if ((context.Reference as TextReference)?.Reference is OpCode opCode) {
				var url = string.Format(msdnUrlFormat, GetMsdnOpCode(opCode));
				StartBrowser(url);
				return true;
			}

			return false;
		}

		static string GetMsdnOpCode(OpCode opCode) =>
			opCode.Name.ToLowerInvariant().Replace('.', '_');

		void StartBrowser(string url) {
			try {
				Process.Start(url);
			}
			catch {
				messageBoxService.Show(dnSpy_Resources.CouldNotStartBrowser);
			}
		}
	}
}
