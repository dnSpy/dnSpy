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

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// Provides code ranges for .NET steppers
	/// </summary>
	public abstract class DbgDotNetCodeRangeService {
		/// <summary>
		/// The offset is in an epilog
		/// </summary>
		public const uint EPILOG = 0xFFFFFFFF;

		/// <summary>
		/// The offset is in the prolog
		/// </summary>
		public const uint PROLOG = 0xFFFFFFFE;

		/// <summary>
		/// Gets code ranges
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Method token</param>
		/// <param name="offset">Offset of IL instruction, or one of <see cref="PROLOG"/>, <see cref="EPILOG"/></param>
		/// <param name="callback">Gets called when the lookup is complete</param>
		public abstract void GetCodeRanges(DbgModule module, uint token, uint offset, Action<GetCodeRangeResult> callback);
	}

	/// <summary>
	/// Contains the code ranges of the requested statement
	/// </summary>
	public struct GetCodeRangeResult {
		/// <summary>
		/// Code ranges of the statement if <see cref="Success"/> is true
		/// </summary>
		public DbgCodeRange[] StatementRanges { get; }

		/// <summary>
		/// true if the code ranges were found
		/// </summary>
		public bool Success { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="success">true if successful</param>
		/// <param name="statementRanges">Code ranges of the statement</param>
		public GetCodeRangeResult(bool success, DbgCodeRange[] statementRanges) {
			Success = success;
			StatementRanges = statementRanges ?? throw new ArgumentNullException(nameof(statementRanges));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="statementRanges">Code ranges of the statement</param>
		public GetCodeRangeResult(DbgCodeRange[] statementRanges) {
			Success = true;
			StatementRanges = statementRanges ?? throw new ArgumentNullException(nameof(statementRanges));
		}
	}
}
