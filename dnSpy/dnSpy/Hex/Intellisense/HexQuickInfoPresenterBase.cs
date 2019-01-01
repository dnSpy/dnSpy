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
using System.Collections.Generic;
using System.ComponentModel;
using dnSpy.Contracts.Hex.Intellisense;
using VSLI = Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Hex.Intellisense {
	abstract class HexQuickInfoPresenterBase : HexIntellisensePresenter, VSLI.IIntellisenseCommandTarget, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }
		public override HexIntellisenseSession Session => session;
		public IList<object> QuickInfoContent => session.QuickInfoContent;

		protected readonly HexQuickInfoSession session;
		protected readonly HexQuickInfoPresenterControl control;

		protected HexQuickInfoPresenterBase(HexQuickInfoSession session) {
			this.session = session ?? throw new ArgumentNullException(nameof(session));
			control = new HexQuickInfoPresenterControl { DataContext = this };
			session.Dismissed += Session_Dismissed;
		}

		bool VSLI.IIntellisenseCommandTarget.ExecuteKeyboardCommand(VSLI.IntellisenseKeyboardCommand command) {
			switch (command) {
			case VSLI.IntellisenseKeyboardCommand.Escape:
				session.Dismiss();
				return true;

			case VSLI.IntellisenseKeyboardCommand.Up:
			case VSLI.IntellisenseKeyboardCommand.Down:
			case VSLI.IntellisenseKeyboardCommand.PageUp:
			case VSLI.IntellisenseKeyboardCommand.PageDown:
			case VSLI.IntellisenseKeyboardCommand.Home:
			case VSLI.IntellisenseKeyboardCommand.End:
			case VSLI.IntellisenseKeyboardCommand.TopLine:
			case VSLI.IntellisenseKeyboardCommand.BottomLine:
			case VSLI.IntellisenseKeyboardCommand.Enter:
			case VSLI.IntellisenseKeyboardCommand.IncreaseFilterLevel:
			case VSLI.IntellisenseKeyboardCommand.DecreaseFilterLevel:
			default:
				return false;
			}
		}

		void Session_Dismissed(object sender, EventArgs e) {
			session.Dismissed -= Session_Dismissed;
			OnSessionDismissed();
		}

		protected virtual void OnSessionDismissed() { }
	}
}
