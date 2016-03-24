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
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Scripting.Roslyn;
using dnSpy.Shared.Scripting;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class ScriptGlobals : IScriptGlobals {
		readonly IScriptGlobalsHelper owner;
		readonly Dispatcher dispatcher;
		readonly CancellationToken token;

		public ScriptGlobals(IScriptGlobalsHelper owner, Dispatcher dispatcher, CancellationToken token) {
			if (owner == null)
				throw new ArgumentNullException();
			this.owner = owner;
			this.token = token;
			this.dispatcher = dispatcher;
		}

		public event EventHandler ScriptReset;
		public void RaiseScriptReset() => ScriptReset?.Invoke(this, EventArgs.Empty);
		public IScriptGlobals Instance => this;
		public CancellationToken Token => token;
		public void Print(string text) => owner.Print(this, text);
		public void Print(string fmt, params object[] args) => Print(string.Format(fmt, args));
		public void PrintLine(string text = null) => owner.PrintLine(this, text);
		public void PrintLine(string fmt, params object[] args) => PrintLine(string.Format(fmt, args));
		public void Print(object value) => owner.Print(this, value);
		public void PrintLine(object value) => owner.PrintLine(this, value);
		public Dispatcher UIDispatcher => dispatcher;
		public void UI(Action action) => dispatcher.UI(action);
		public T UI<T>(Func<T> func) => dispatcher.UI(func);
		public void Break() => Debugger.Break();
		public T Resolve<T>() => owner.ServiceLocator.Resolve<T>();
		public T TryResolve<T>() => owner.ServiceLocator.TryResolve<T>();

		public MsgBoxButton Show(string message, MsgBoxButton buttons = MsgBoxButton.OK, Window ownerWindow = null) =>
			dispatcher.UI(() => Shared.App.MsgBox.Instance.Show(message, buttons, ownerWindow));

		public MsgBoxButton ShowOKCancel(string message, Window ownerWindow = null) =>
			dispatcher.UI(() => Shared.App.MsgBox.Instance.Show(message, MsgBoxButton.OK | MsgBoxButton.Cancel, ownerWindow));

		public MsgBoxButton ShowOC(string message, Window ownerWindow = null) => ShowOKCancel(message, ownerWindow);

		public MsgBoxButton ShowYesNo(string message, Window ownerWindow = null) =>
			dispatcher.UI(() => Shared.App.MsgBox.Instance.Show(message, MsgBoxButton.Yes | MsgBoxButton.No, ownerWindow));

		public MsgBoxButton ShowYN(string message, Window ownerWindow = null) => ShowYesNo(message, ownerWindow);

		public MsgBoxButton ShowYesNoCancel(string message, Window ownerWindow = null) =>
			dispatcher.UI(() => Shared.App.MsgBox.Instance.Show(message, MsgBoxButton.Yes | MsgBoxButton.No | MsgBoxButton.Cancel, ownerWindow));

		public MsgBoxButton ShowYNC(string message, Window ownerWindow = null) => ShowYesNoCancel(message, ownerWindow);

		public T Ask<T>(string labelMessage, string defaultText = null, string title = null, Func<string, T> converter = null, Func<string, string> verifier = null, Window ownerWindow = null) =>
			dispatcher.UI(() => Shared.App.MsgBox.Instance.Ask(labelMessage, defaultText, title, converter, verifier, ownerWindow));

		public void Show(Exception exception, string msg = null, Window ownerWindow = null) =>
			dispatcher.UI(() => Shared.App.MsgBox.Instance.Show(exception, msg, ownerWindow));
	}
}
