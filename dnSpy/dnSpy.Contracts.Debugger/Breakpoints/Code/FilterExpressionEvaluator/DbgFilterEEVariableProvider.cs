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

namespace dnSpy.Contracts.Debugger.Breakpoints.Code.FilterExpressionEvaluator {
	/// <summary>
	/// Provides all the values of the variables that can be used by the filter expression
	/// </summary>
	public abstract class DbgFilterEEVariableProvider {
		/// <summary>
		/// Machine name
		/// </summary>
		public abstract string MachineName { get; }

		/// <summary>
		/// Process id
		/// </summary>
		public abstract int ProcessId { get; }

		/// <summary>
		/// Process name
		/// </summary>
		public abstract string ProcessName { get; }

		/// <summary>
		/// Thread id
		/// </summary>
		public abstract ulong ThreadId { get; }

		/// <summary>
		/// Thread name
		/// </summary>
		public abstract string ThreadName { get; }
	}
}
