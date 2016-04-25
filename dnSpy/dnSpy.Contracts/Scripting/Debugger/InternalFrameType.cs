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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Internal frame type
	/// </summary>
	public enum InternalFrameType {
		// IMPORTANT: Must be identical to dndbg.COM.CorDebug.CorDebugInternalFrameType (enum field names may be different)

		/// <summary>
		/// A null value. The ICorDebugInternalFrame::GetFrameType method never returns this value.
		/// </summary>
		None,
		/// <summary>
		/// A managed-to-unmanaged stub frame.
		/// </summary>
		M2U,
		/// <summary>
		/// An unmanaged-to-managed stub frame.
		/// </summary>
		U2M,
		/// <summary>
		/// A transition between application domains.
		/// </summary>
		AppDomainTransition,
		/// <summary>
		/// A lightweight method call.
		/// </summary>
		LightweightFunction,
		/// <summary>
		/// The start of function evaluation.
		/// </summary>
		FuncEval,
		/// <summary>
		/// An internal call into the common language runtime.
		/// </summary>
		InternalCall,
		/// <summary>
		/// The start of a class initialization.
		/// </summary>
		ClassInit,
		/// <summary>
		/// An exception that is thrown.
		/// </summary>
		Exception,
		/// <summary>
		/// A frame used for code access security.
		/// </summary>
		Security,
		/// <summary>
		/// The runtime is JIT-compiling a method.
		/// </summary>
		JitCompilation
	}
}
