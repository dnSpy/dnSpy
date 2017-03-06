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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Predefined thread kinds, see also <see cref="ThreadCategoryProvider"/>
	/// </summary>
	public static class PredefinedThreadKinds {
		/// <summary>
		/// Unknown thread kind
		/// </summary>
		public const string Unknown = nameof(Unknown);

		/// <summary>
		/// Main thread
		/// </summary>
		public const string Main = nameof(Main);

		/// <summary>
		/// Thread pool thread
		/// </summary>
		public const string ThreadPool = nameof(ThreadPool);

		/// <summary>
		/// Worker thread
		/// </summary>
		public const string WorkerThread = nameof(WorkerThread);

		/// <summary>
		/// Terminated thread
		/// </summary>
		public const string Terminated = nameof(Terminated);

		/// <summary>
		/// Garbage Collector thread
		/// </summary>
		public const string GC = nameof(GC);

		/// <summary>
		/// Finalizer thread
		/// </summary>
		public const string Finalizer = nameof(Finalizer);
	}
}
