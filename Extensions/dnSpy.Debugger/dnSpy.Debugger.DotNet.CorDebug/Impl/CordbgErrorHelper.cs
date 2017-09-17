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

using dndbg.Engine;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Debugger.DotNet.CorDebug.Properties;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	static class CordbgErrorHelper {
		public static string InternalError => dnSpy_Debugger_DotNet_CorDebug_Resources.InternalDebuggerError;

		public static string GetErrorMessage(int hr) {
			if (hr >= 0)
				return dnSpy_Debugger_DotNet_CorDebug_Resources.InternalDebuggerError;

			if (CordbgErrors.IsCantEvaluateError(hr))
				return PredefinedEvaluationErrorMessages.CantFuncEvaluateWhenThreadIsAtUnsafePoint;

			return dnSpy_Debugger_DotNet_CorDebug_Resources.InternalDebuggerError + " (0x" + hr.ToString("X8") + ")";
		}
	}
}
