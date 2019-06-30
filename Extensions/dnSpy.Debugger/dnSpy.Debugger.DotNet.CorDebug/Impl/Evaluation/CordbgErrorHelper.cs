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

using dndbg.Engine;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Debugger.DotNet.CorDebug.Properties;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	static class CordbgErrorHelper {
		public static string InternalError => PredefinedEvaluationErrorMessages.InternalDebuggerError;
		public static string FuncEvalRequiresAllThreadsToRun => PredefinedEvaluationErrorMessages.FuncEvalRequiresAllThreadsToRun;

		public static string GetErrorMessage(int hr) {
			if (hr >= -1)
				return PredefinedEvaluationErrorMessages.InternalDebuggerError;

			if (CordbgErrors.IsCantEvaluateError(hr))
				return PredefinedEvaluationErrorMessages.CantFuncEvaluateWhenThreadIsAtUnsafePoint;
			if (hr == CordbgErrors.CORDBG_E_IL_VAR_NOT_AVAILABLE)
				return PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway;
			if (hr == CordbgErrors.CORDBG_E_CLASS_NOT_LOADED || hr == CordbgErrors.CORDBG_E_STATIC_VAR_NOT_AVAILABLE)
				return PredefinedEvaluationErrorMessages.RuntimeIsUnableToEvaluateExpression;

			return dnSpy_Debugger_DotNet_CorDebug_Resources.InternalDebuggerError + " (0x" + hr.ToString("X8") + ")";
		}
	}
}
