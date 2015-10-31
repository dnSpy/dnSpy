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

using System;

namespace dnSpy.Files {
	public enum DispatcherPrio {
		ContextIdle,
		Background,
		Loaded,
		Send,
	}

	public interface IDispatcher {
		/// <summary>
		/// Execute code asynchronously in the dispatcher thread
		/// </summary>
		/// <param name="priority"></param>
		/// <param name="method"></param>
		void BeginInvoke(DispatcherPrio priority, Action method);

		/// <summary>
		/// true if the current thread is the dispatcher thread
		/// </summary>
		/// <returns></returns>
		bool CheckAccess();

		/// <summary>
		/// Throws <see cref="InvalidOperationException"/> if this method is called on the wrong thread
		/// </summary>
		void VerifyAccess();
	}
}
