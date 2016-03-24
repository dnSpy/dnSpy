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
using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace dnSpy.Shared.Scripting {
	public static class UIUtils {
		/// <summary>
		/// Executes <paramref name="a"/> in the UI thread and then returns
		/// </summary>
		/// <param name="dispatcher">UI dispatcher</param>
		/// <param name="a">Action</param>
		public static void UI(this Dispatcher dispatcher, Action a) {
			if (dispatcher.CheckAccess()) {
				a();
				return;
			}

			Debugger.NotifyOfCrossThreadDependency();

			ExceptionDispatchInfo exInfo = null;
			dispatcher.Invoke(new Action(() => {
				try {
					a();
				}
				catch (Exception ex) {
					exInfo = ExceptionDispatchInfo.Capture(ex);
					return;
				}
			}), DispatcherPriority.Send);
			if (exInfo != null)
				exInfo.Throw();
		}

		/// <summary>
		/// Executes <paramref name="f"/> in the UI thread and returns the result
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="dispatcher">UI dispatcher</param>
		/// <param name="f">Func</param>
		/// <returns></returns>
		public static T UI<T>(this Dispatcher dispatcher, Func<T> f) {
			if (dispatcher.CheckAccess())
				return f();

			Debugger.NotifyOfCrossThreadDependency();

			ExceptionDispatchInfo exInfo = null;
			var res = (T)dispatcher.Invoke(new Func<T>(() => {
				try {
					return f();
				}
				catch (Exception ex) {
					exInfo = ExceptionDispatchInfo.Capture(ex);
					return default(T);
				}
			}), DispatcherPriority.Send);
			if (exInfo != null)
				exInfo.Throw();
			return res;
		}

		/// <summary>
		/// Returns the result of an <see cref="IEnumerable{T}"/>. <paramref name="getIter"/> is
		/// only called on the UI thread.
		/// </summary>
		/// <typeparam name="T">Type to return</typeparam>
		/// <param name="dispatcher">UI dispatcher</param>
		/// <param name="getIter">Called on the UI thread to return the result</param>
		/// <returns></returns>
		public static IEnumerable<T> UIIter<T>(this Dispatcher dispatcher, Func<IEnumerable<T>> getIter) {
			if (dispatcher.CheckAccess()) {
				foreach (var o in getIter())
					yield return o;
				yield break;
			}

			Debugger.NotifyOfCrossThreadDependency();

			IEnumerator<T> enumerator = null;
			for (;;) {
				bool canContinue = false;
				ExceptionDispatchInfo exInfo = null;
				var res = (T)dispatcher.Invoke(new Func<T>(() => {
					try {
						if (enumerator == null)
							enumerator = getIter().GetEnumerator();
						if (!(canContinue = enumerator.MoveNext()))
							return default(T);
						return enumerator.Current;
					}
					catch (Exception ex) {
						canContinue = false;
						exInfo = ExceptionDispatchInfo.Capture(ex);
						return default(T);
					}
				}), DispatcherPriority.Send);
				if (exInfo != null)
					exInfo.Throw();
				if (!canContinue)
					break;
				yield return res;
			}
		}
	}
}
