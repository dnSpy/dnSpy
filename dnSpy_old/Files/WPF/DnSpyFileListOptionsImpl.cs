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

using System.Windows.Threading;
using dnSpy.Options;
using ICSharpCode.ILSpy.Options;

namespace dnSpy.Files.WPF {
	sealed class DnSpyFileListOptionsImpl : IDnSpyFileListOptions {
		public bool UseDebugSymbols {
			get { return DecompilerSettingsPanel.CurrentDecompilerSettings.UseDebugSymbols; }
		}

		public bool UseMemoryMappedIO {
			get { return OtherSettings.Instance.UseMemoryMappedIO; }
		}

		public IDispatcher Dispatcher {
			get { return dispatcher; }
		}

		public bool UseGAC {
			get { return true; }
		}

		readonly IDispatcher dispatcher;

		public DnSpyFileListOptionsImpl(Dispatcher disp) {
			this.dispatcher = new DispatcherImpl(disp);
		}
	}
}
