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
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Roslyn;
using dnSpy.Contracts.Text;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class ScriptGlobals : IScriptGlobals {
		readonly IScriptGlobalsHelper owner;
		readonly Dispatcher dispatcher;
		readonly CancellationToken token;
		readonly PrintOptionsImpl printOptionsImpl;

		public ScriptGlobals(IScriptGlobalsHelper owner, Dispatcher dispatcher, CancellationToken token) {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (dispatcher == null)
				throw new ArgumentNullException(nameof(dispatcher));
			this.owner = owner;
			this.token = token;
			this.dispatcher = dispatcher;
			printOptionsImpl = new PrintOptionsImpl();
		}

		public event EventHandler ScriptResetting;
		public void RaiseScriptResetting() => ScriptResetting?.Invoke(this, EventArgs.Empty);
		public IScriptGlobals Instance => this;
		public CancellationToken Token => token;
		public IPrintOptions PrintOptions => printOptionsImpl;
		public PrintOptionsImpl PrintOptionsImpl => printOptionsImpl;
		public void Write(string text, object color) => Print(color ?? BoxedTextColor.ReplScriptOutputText, text);
		public void Write(string text, TextColor color) => Write(text, color.Box());
		public void PrintError(string text) => Print(BoxedTextColor.Error, text);
		public void PrintError(string fmt, params object[] args) => Print(BoxedTextColor.Error, fmt, args);
		public void PrintLineError(string text) => PrintLine(BoxedTextColor.Error, text);
		public void PrintLineError(string fmt, params object[] args) => PrintLine(BoxedTextColor.Error, fmt, args);
		public void Print(object color, string text) => owner.Print(this, color ?? BoxedTextColor.ReplScriptOutputText, text);
		public void Print(TextColor color, string text) => owner.Print(this, color.Box(), text);
		public void Print(string text) => Print(BoxedTextColor.ReplScriptOutputText, text);
		public void Print(object color, string fmt, params object[] args) => Print(color ?? BoxedTextColor.ReplScriptOutputText, string.Format(fmt, args));
		public void Print(TextColor color, string fmt, params object[] args) => Print(color, string.Format(fmt, args));
		public void Print(string fmt, params object[] args) => Print(BoxedTextColor.ReplScriptOutputText, fmt, args);
		public void PrintLine(object color, string text) => owner.PrintLine(this, color ?? BoxedTextColor.ReplScriptOutputText, text);
		public void PrintLine(TextColor color, string text) => owner.PrintLine(this, color.Box(), text);
		public void PrintLine(string text = null) => PrintLine(BoxedTextColor.ReplScriptOutputText, text);
		public void PrintLine(object color, string fmt, params object[] args) => PrintLine(color ?? BoxedTextColor.ReplScriptOutputText, string.Format(fmt, args));
		public void PrintLine(TextColor color, string fmt, params object[] args) => PrintLine(color, string.Format(fmt, args));
		public void PrintLine(string fmt, params object[] args) => PrintLine(BoxedTextColor.ReplScriptOutputText, fmt, args);
		public void Print(object value, object color) => owner.Print(this, color ?? BoxedTextColor.ReplScriptOutputText, printOptionsImpl, value);
		public void PrintLine(object value, object color) => owner.PrintLine(this, color ?? BoxedTextColor.ReplScriptOutputText, printOptionsImpl, value);
		public void Print(Exception ex, object color) => owner.Print(this, color ?? BoxedTextColor.Error, ex);
		public void PrintLine(Exception ex, object color) => owner.PrintLine(this, color ?? BoxedTextColor.Error, ex);
		public void Print(object value, TextColor color) => owner.Print(this, color.Box(), printOptionsImpl, value);
		public void PrintLine(object value, TextColor color) => owner.PrintLine(this, color.Box(), printOptionsImpl, value);
		public void Print(Exception ex, TextColor color) => owner.Print(this, color.Box(), ex);
		public void PrintLine(Exception ex, TextColor color) => owner.PrintLine(this, color.Box(), ex);
		public ICachedWriter CreateWriter() => new CachedWriter(this);
		public void Write(List<ColorAndText> list) => owner.Write(this, list);
		public Dispatcher UIDispatcher => dispatcher;
		public void UI(Action action) => dispatcher.UI(action);
		public T UI<T>(Func<T> func) => dispatcher.UI(func);
		public void Break() => Debugger.Break();
		public T Resolve<T>() => owner.ServiceLocator.Resolve<T>();
		public T TryResolve<T>() => owner.ServiceLocator.TryResolve<T>();

		public void Print(CachedWriter cw, Exception ex, object color) => owner.Print(this, cw, color, ex);
		public void Print(CachedWriter cw, object value, object color) => owner.Print(this, cw, color, cw.PrintOptions, value);

		public void PrintLine(CachedWriter cw, Exception ex, object color) {
			Print(cw, ex, color);
			PrintLine();
		}

		public void PrintLine(CachedWriter cw, object value, object color) {
			Print(cw, value, color);
			PrintLine();
		}

		public MsgBoxButton Show(string message, MsgBoxButton buttons = MsgBoxButton.OK, Window ownerWindow = null) =>
			dispatcher.UI(() => MsgBox.Instance.Show(message, buttons, ownerWindow));

		public MsgBoxButton ShowOKCancel(string message, Window ownerWindow = null) =>
			dispatcher.UI(() => MsgBox.Instance.Show(message, MsgBoxButton.OK | MsgBoxButton.Cancel, ownerWindow));

		public MsgBoxButton ShowOC(string message, Window ownerWindow = null) => ShowOKCancel(message, ownerWindow);

		public MsgBoxButton ShowYesNo(string message, Window ownerWindow = null) =>
			dispatcher.UI(() => MsgBox.Instance.Show(message, MsgBoxButton.Yes | MsgBoxButton.No, ownerWindow));

		public MsgBoxButton ShowYN(string message, Window ownerWindow = null) => ShowYesNo(message, ownerWindow);

		public MsgBoxButton ShowYesNoCancel(string message, Window ownerWindow = null) =>
			dispatcher.UI(() => MsgBox.Instance.Show(message, MsgBoxButton.Yes | MsgBoxButton.No | MsgBoxButton.Cancel, ownerWindow));

		public MsgBoxButton ShowYNC(string message, Window ownerWindow = null) => ShowYesNoCancel(message, ownerWindow);

		public T Ask<T>(string labelMessage, string defaultText = null, string title = null, Func<string, T> converter = null, Func<string, string> verifier = null, Window ownerWindow = null) =>
			dispatcher.UI(() => MsgBox.Instance.Ask(labelMessage, defaultText, title, converter, verifier, ownerWindow));

		public void Show(Exception exception, string msg = null, Window ownerWindow = null) =>
			dispatcher.UI(() => MsgBox.Instance.Show(exception, msg, ownerWindow));
	}
}
