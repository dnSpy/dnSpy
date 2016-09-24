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

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Language.Intellisense {
	abstract class QuickInfoPresenterBase : IIntellisensePresenter, IIntellisenseCommandTarget {
		public IIntellisenseSession Session => session;
		public IList<object> QuickInfoContent => session.QuickInfoContent;

		protected readonly IQuickInfoSession session;
		protected readonly QuickInfoPresenterControl control;

		protected QuickInfoPresenterBase(IQuickInfoSession session) {
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			this.session = session;
			this.control = new QuickInfoPresenterControl { DataContext = this };
			session.Dismissed += Session_Dismissed;
		}

		bool IIntellisenseCommandTarget.ExecuteKeyboardCommand(IntellisenseKeyboardCommand command) {
			switch (command) {
			case IntellisenseKeyboardCommand.Escape:
				session.Dismiss();
				return true;

			case IntellisenseKeyboardCommand.Up:
			case IntellisenseKeyboardCommand.Down:
			case IntellisenseKeyboardCommand.PageUp:
			case IntellisenseKeyboardCommand.PageDown:
			case IntellisenseKeyboardCommand.Home:
			case IntellisenseKeyboardCommand.End:
			case IntellisenseKeyboardCommand.TopLine:
			case IntellisenseKeyboardCommand.BottomLine:
			case IntellisenseKeyboardCommand.Enter:
			case IntellisenseKeyboardCommand.IncreaseFilterLevel:
			case IntellisenseKeyboardCommand.DecreaseFilterLevel:
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
