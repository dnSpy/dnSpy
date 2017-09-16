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

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation {
	/// <summary>
	/// Return value of methods creating <see cref="DbgDotNetValue"/>s
	/// </summary>
	public struct DbgDotNetValueResult {
		/// <summary>
		/// Gets the value or null if there was an error (<see cref="ErrorMessage"/>)
		/// </summary>
		public DbgDotNetValue Value { get; }

		/// <summary>
		/// Gets the error message or null if there was no error
		/// </summary>
		public string ErrorMessage { get; }

		/// <summary>
		/// true if there was an error, see <see cref="ErrorMessage"/>
		/// </summary>
		public bool HasError => ErrorMessage != null;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public DbgDotNetValueResult(DbgDotNetValue value) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			ErrorMessage = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		public DbgDotNetValueResult(string errorMessage) {
			Value = null;
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
		}
	}
}
