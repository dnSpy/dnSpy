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
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Scripting.Roslyn;
using dnSpy.Contracts.TextEditor;
using dnSpy.Shared.Scripting;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class ScriptGlobals : IScriptGlobals {
		readonly IScriptGlobalsHelper owner;
		readonly Dispatcher dispatcher;
		readonly CancellationToken token;
		readonly PrintOptionsImpl printOptionsImpl;

		public ScriptGlobals(IScriptGlobalsHelper owner, Dispatcher dispatcher, CancellationToken token) {
			if (owner == null)
				throw new ArgumentNullException();
			this.owner = owner;
			this.token = token;
			this.dispatcher = dispatcher;
			this.printOptionsImpl = new PrintOptionsImpl();
		}

		public event EventHandler ScriptReset;
		public void RaiseScriptReset() => ScriptReset?.Invoke(this, EventArgs.Empty);
		public IScriptGlobals Instance => this;
		public CancellationToken Token => token;
		public IPrintOptions PrintOptions => printOptionsImpl;
		public PrintOptionsImpl PrintOptionsImpl => printOptionsImpl;
		public void Write(string text, OutputColor color) => Print(color, text);
		public void PrintError(string text) => Print(OutputColor.Error, text);
		public void PrintError(string fmt, params object[] args) => Print(OutputColor.Error, fmt, args);
		public void PrintLineError(string text) => PrintLine(OutputColor.Error, text);
		public void PrintLineError(string fmt, params object[] args) => PrintLine(OutputColor.Error, fmt, args);
		public void Print(OutputColor color, string text) => owner.Print(this, color, text);
		public void Print(string text) => Print(OutputColor.ReplScriptOutputText, text);
		public void Print(OutputColor color, string fmt, params object[] args) => Print(color, string.Format(fmt, args));
		public void Print(string fmt, params object[] args) => Print(OutputColor.ReplScriptOutputText, fmt, args);
		public void PrintLine(OutputColor color, string text) => owner.PrintLine(this, color, text);
		public void PrintLine(string text = null) => PrintLine(OutputColor.ReplScriptOutputText, text);
		public void PrintLine(OutputColor color, string fmt, params object[] args) => PrintLine(color, string.Format(fmt, args));
		public void PrintLine(string fmt, params object[] args) => PrintLine(OutputColor.ReplScriptOutputText, fmt, args);
		public void Print(object value, OutputColor color) => owner.Print(this, color, printOptionsImpl, value);
		public void PrintLine(object value, OutputColor color) => owner.PrintLine(this, color, printOptionsImpl, value);
		public void Print(Exception ex, OutputColor color) => owner.Print(this, color, ex);
		public void PrintLine(Exception ex, OutputColor color) => owner.PrintLine(this, color, ex);
		public ICachedWriter CreateWriter() => new CachedWriter(this);
		public void Write(List<ColorAndText> list) => owner.Write(this, list);
		public Dispatcher UIDispatcher => dispatcher;
		public void UI(Action action) => dispatcher.UI(action);
		public T UI<T>(Func<T> func) => dispatcher.UI(func);
		public void Break() => Debugger.Break();
		public T Resolve<T>() => owner.ServiceLocator.Resolve<T>();
		public T TryResolve<T>() => owner.ServiceLocator.TryResolve<T>();

		public void Print(CachedWriter cw, Exception ex, OutputColor color) => owner.Print(this, cw, color, ex);
		public void Print(CachedWriter cw, object value, OutputColor color) => owner.Print(this, cw, color, cw.PrintOptions, value);

		public void PrintLine(CachedWriter cw, Exception ex, OutputColor color) {
			Print(cw, ex, color);
			PrintLine();
		}

		public void PrintLine(CachedWriter cw, object value, OutputColor color) {
			Print(cw, value, color);
			PrintLine();
		}

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
