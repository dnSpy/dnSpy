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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation.Internal;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Evaluation {
	static class PredefinedEvaluationErrorMessagesHelper {
		static readonly Dictionary<string, string> toErrorMessage;
		static PredefinedEvaluationErrorMessagesHelper() {
			const int TOTAL_COUNT = 12;
			toErrorMessage = new Dictionary<string, string>(TOTAL_COUNT, StringComparer.Ordinal) {
				{ PredefinedEvaluationErrorMessages.InternalDebuggerError, dnSpy_Debugger_Resources.InternalDebuggerError },
				{ PredefinedEvaluationErrorMessages.ExpressionCausesSideEffects, dnSpy_Debugger_Resources.ExpressionCausesSideEffectsNoEval },
				{ PredefinedEvaluationErrorMessages.FuncEvalDisabled, dnSpy_Debugger_Resources.FunctionEvaluationDisabled },
				{ PredefinedEvaluationErrorMessages.FuncEvalTimedOut, dnSpy_Debugger_Resources.Locals_Error_EvaluationTimedOut },
				{ PredefinedEvaluationErrorMessages.FuncEvalTimedOutNowDisabled, dnSpy_Debugger_Resources.Locals_Error_EvalTimedOutIsDisabled },
				{ PredefinedEvaluationErrorMessages.CanFuncEvalOnlyWhenPaused, dnSpy_Debugger_Resources.Error_CantEvalUnlessDebuggerStopped },
				{ PredefinedEvaluationErrorMessages.CantFuncEvalWhenUnhandledExceptionHasOccurred, dnSpy_Debugger_Resources.Error_CantEvalWhenUnhandledExceptionHasOccurred },
				{ PredefinedEvaluationErrorMessages.CantFuncEval, dnSpy_Debugger_Resources.Locals_Error_EvalDisabledCantCallPropsAndMethods },
				{ PredefinedEvaluationErrorMessages.CantFuncEvaluateWhenThreadIsAtUnsafePoint, dnSpy_Debugger_Resources.Locals_Error_CantEvaluateWhenThreadIsAtUnsafePoint },
				{ PredefinedEvaluationErrorMessages.FuncEvalRequiresAllThreadsToRun, dnSpy_Debugger_Resources.FuncEvalRequiresAllThreadsToRun },
				{ PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway, dnSpy_Debugger_Resources.CannotReadLocalOrArgumentMaybeOptimizedAway },
				{ PredefinedEvaluationErrorMessages.RuntimeIsUnableToEvaluateExpression, dnSpy_Debugger_Resources.RuntimeIsUnableToEvaluateExpression },
			};
			Debug.Assert(toErrorMessage.Count == TOTAL_COUNT);
		}

		public static string GetErrorMessage(string error) {
			if (error == null)
				return null;
			if (toErrorMessage.TryGetValue(error, out var msg))
				return msg;
			return error;
		}
	}

	[Export(typeof(IPredefinedEvaluationErrorMessagesHelper))]
	sealed class PredefinedEvaluationErrorMessagesHelperImpl : IPredefinedEvaluationErrorMessagesHelper {
		public string GetErrorMessage(string error) {
			if (error == null)
				throw new ArgumentNullException(nameof(error));
			return PredefinedEvaluationErrorMessagesHelper.GetErrorMessage(error);
		}
	}
}
