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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Invokes code on another thread.
	/// It's used if the underlying .NET metadata reader is a COM interface.
	/// </summary>
	public abstract class DmdDispatcher {
		/// <summary>
		/// Checks whether the current thread is the dispatcher thread
		/// </summary>
		/// <returns></returns>
		public abstract bool CheckAccess();

		/// <summary>
		/// Throws if the current thread isn't the dispatcher thread
		/// </summary>
		public void VerifyAccess() {
			if (!CheckAccess())
				throw new InvalidOperationException("Wrong dispatcher thread");
		}

		/// <summary>
		/// Switches to the dispatcher thread and executes <paramref name="callback"/>
		/// </summary>
		/// <param name="callback">Code to execute</param>
		public abstract void Invoke(Action callback);

		/// <summary>
		/// Switches to the dispatcher thread and executes <paramref name="callback"/>
		/// </summary>
		/// <typeparam name="T">Type of return data</typeparam>
		/// <param name="callback">Code to execute</param>
		/// <returns></returns>
		public abstract T Invoke<T>(Func<T> callback);
	}
}
