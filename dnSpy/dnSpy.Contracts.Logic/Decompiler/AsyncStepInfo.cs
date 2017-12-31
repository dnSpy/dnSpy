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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Async method step info
	/// </summary>
	public readonly struct AsyncStepInfo {
		/// <summary>
		/// Offset in <see cref="System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext"/> where it starts waiting for the result
		/// </summary>
		public uint YieldOffset { get; }

		/// <summary>
		/// Offset in <see cref="System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext"/> where it resumes after the result is available
		/// </summary>
		public uint ResumeOffset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="yieldOffset">Offset in <see cref="System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext"/> where it starts waiting for the result</param>
		/// <param name="resumeOffset">Offset in <see cref="System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext"/> where it resumes after the result is available</param>
		public AsyncStepInfo(uint yieldOffset, uint resumeOffset) {
			YieldOffset = yieldOffset;
			ResumeOffset = resumeOffset;
		}
	}
}
