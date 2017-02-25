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

using System;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Executes code in a thread
	/// </summary>
	public abstract class DispatcherThread {
		/// <summary>
		/// Verifies that the current code is running in the dispatcher thread. <see cref="InvalidOperationException"/>
		/// is thrown if it's the wrong thread.
		/// </summary>
		public void VerifyAccess() {
			if (!CheckAccess())
				throw new InvalidOperationException("Wrong dispatcher thread");
		}

		/// <summary>
		/// Checks whether the current thread is the dispatcher thread
		/// </summary>
		/// <returns></returns>
		public abstract bool CheckAccess();

		/// <summary>
		/// Executes code asynchronously in the dispatcher thread. This method returns immediately even if
		/// it happens to be called in the dispatcher thread.
		/// </summary>
		/// <param name="action">Code to execute</param>
		public abstract void BeginInvoke(Action action);

		/// <summary>
		/// Executes the code synchronously in the dispatcher thread. This method returns as soon as
		/// <paramref name="func"/> has been executed.
		/// </summary>
		/// <param name="func">Code to execute</param>
		/// <returns></returns>
		public abstract T Invoke<T>(Func<T> func);

		/// <summary>
		/// Executes the code synchronously in the dispatcher thread. This method returns as soon as
		/// <paramref name="action"/> has been executed.
		/// </summary>
		/// <param name="action">Code to execute</param>
		/// <returns></returns>
		public void Invoke(Action action) => Invoke<object>(() => { action(); return null; });
	}
}
