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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Creates <see cref="DbgEngine"/> instances
	/// </summary>
	public abstract class DbgEngineProvider {
		/// <summary>
		/// Creates a <see cref="DbgEngine"/> and starts debugging or returns null if
		/// <paramref name="options"/> is unsupported
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEngine Start(StartDebuggingOptions options);
	}
}
