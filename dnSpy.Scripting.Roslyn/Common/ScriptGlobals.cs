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
using System.Windows.Threading;

namespace dnSpy.Scripting.Roslyn.Common {
	/// <summary>
	/// The script's global class
	/// </summary>
	public sealed class ScriptGlobals {	// Must be public so the scripts can access it
		readonly IScriptGlobalsHelper owner;
		readonly Dispatcher dispatcher;
		readonly CancellationToken token;

		internal ScriptGlobals(IScriptGlobalsHelper owner, CancellationToken token) {
			if (owner == null)
				throw new ArgumentNullException();
			this.owner = owner;
			this.token = token;
			this.dispatcher = Dispatcher.CurrentDispatcher;
		}

		/// <summary>
		/// Cancellation token that gets signalled when the script gets reset
		/// </summary>
		public CancellationToken Token {
			get { return token; }
		}

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="text">Text</param>
		public void Print(string text) {
			// Method is thread safe
			owner.Print(this, text);
		}

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		public void Print(string fmt, params object[] args) {
			Print(string.Format(fmt, args));
		}

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="text">Text or null</param>
		public void PrintLine(string text = null) {
			// Method is thread safe
			owner.PrintLine(this, text);
		}

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		public void PrintLine(string fmt, params object[] args) {
			PrintLine(string.Format(fmt, args));
		}

		/// <summary>
		/// Formats and prints a value to the screen
		/// </summary>
		/// <param name="value">Value, can be null</param>
		public void Print(object value) {
			// Method is thread safe
			owner.Print(this, value);
		}

		/// <summary>
		/// Formats and prints a value followed by a new line to the screen
		/// </summary>
		/// <param name="value">Value or null</param>
		public void PrintLine(object value) {
			// Method is thread safe
			owner.PrintLine(this, value);
		}

		/// <summary>
		/// UI thread dispatcher
		/// </summary>
		public Dispatcher UIDispatcher {
			get { return dispatcher; }
		}

		/// <summary>
		/// Executes <paramref name="action"/> in the UI thread
		/// </summary>
		/// <param name="action">Code</param>
		public void UI(Action action) {
			UIDispatcher.Invoke(action);
		}

		/// <summary>
		/// Executes <paramref name="func"/> in the UI thread
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="func">Code</param>
		/// <returns></returns>
		public T UI<T>(Func<T> func) {
			return UIDispatcher.Invoke(func);
		}

		/// <summary>
		/// Calls <see cref="Debugger.Break"/>. Use dnSpy to debug itself (dnSpy --multiple) and
		/// then call this method from your script in the debugged dnSpy process.
		/// </summary>
		public void Break() {
			Debugger.Break();
		}

		/// <summary>
		/// Resolves a service, and throws if it wasn't found
		/// </summary>
		/// <typeparam name="T">Type of service</typeparam>
		/// <returns></returns>
		public T Resolve<T>() {
			if (UIDispatcher.CheckAccess())
				return owner.ServiceLocator.Resolve<T>();
			return UI(() => owner.ServiceLocator.Resolve<T>());
		}

		/// <summary>
		/// Resolves a service or returns null if not found
		/// </summary>
		/// <typeparam name="T">Type of service</typeparam>
		/// <returns></returns>
		public T TryResolve<T>() {
			if (UIDispatcher.CheckAccess())
				return owner.ServiceLocator.TryResolve<T>();
			return UI(() => owner.ServiceLocator.TryResolve<T>());
		}
	}
}
