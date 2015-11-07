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

using System.Windows;
using System.Windows.Input;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.ToolBars {
	sealed class ToolBarButtonVM : ViewModelBase {
		public ICommand Command {
			get { return command; }
		}
		readonly ICommand command;

		public IInputElement CommandTarget {
			get { return commandTarget; }
		}
		readonly IInputElement commandTarget;

		public string Header {
			get { return header; }
		}
		readonly string header;

		public bool HasHeader {
			get { return !string.IsNullOrEmpty(Header); }
		}

		public string ToolTip {
			get { return toolTip; }
		}
		readonly string toolTip;

		public object Image {
			get { return image; }
		}
		readonly object image;

		public ToolBarButtonVM(ICommand command, IInputElement commandTarget, string header, string toolTip, object image) {
			this.command = command;
			this.commandTarget = commandTarget;
			this.header = string.IsNullOrEmpty(header) ? null : header;
			this.toolTip = string.IsNullOrEmpty(toolTip) ? null : toolTip;
			this.image = image;
		}
	}
}
