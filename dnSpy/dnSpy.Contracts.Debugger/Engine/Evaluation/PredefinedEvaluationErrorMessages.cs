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

using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Common error messages
	/// </summary>
	public static class PredefinedEvaluationErrorMessages {
		const string SUFFIX = " <({[dnSpy]})> ";

		/// <summary>
		/// Some error
		/// </summary>
		public const string InternalDebuggerError = nameof(InternalDebuggerError) + SUFFIX;

		/// <summary>
		/// <see cref="DbgEvaluationOptions.NoSideEffects"/> is set but expression causes side effects
		/// </summary>
		public const string ExpressionCausesSideEffects = nameof(ExpressionCausesSideEffects) + SUFFIX;

		/// <summary>
		/// <see cref="DbgValueNodeEvaluationOptions.NoFuncEval"/> or <see cref="DbgEvaluationOptions.NoFuncEval"/>
		/// is set but code must call a method
		/// </summary>
		public const string FuncEvalDisabled = nameof(FuncEvalDisabled) + SUFFIX;

		/// <summary>
		/// Function evaluation timed out
		/// </summary>
		public const string FuncEvalTimedOut = nameof(FuncEvalTimedOut) + SUFFIX;

		/// <summary>
		/// Function evaluation timed out previously and is disabled until the user continues the debugged program
		/// </summary>
		public const string FuncEvalTimedOutNowDisabled = nameof(FuncEvalTimedOutNowDisabled) + SUFFIX;

		/// <summary>
		/// The debugged program isn't paused so it's not possible to func-eval
		/// </summary>
		public const string CanFuncEvalOnlyWhenPaused = nameof(CanFuncEvalOnlyWhenPaused) + SUFFIX;

		/// <summary>
		/// It's not possible to func-eval when an unhandled exception has occurred
		/// </summary>
		public const string CantFuncEvalWhenUnhandledExceptionHasOccurred = nameof(CantFuncEvalWhenUnhandledExceptionHasOccurred) + SUFFIX;

		/// <summary>
		/// It's not possible to func-eval, eg. because we're already func-eval'ing
		/// </summary>
		public const string CantFuncEval = nameof(CantFuncEval) + SUFFIX;

		/// <summary>
		/// It's not possible to func-eval because the thread isn't at a safe point where a GC can occur
		/// </summary>
		public const string CantFuncEvaluateWhenThreadIsAtUnsafePoint = nameof(CantFuncEvaluateWhenThreadIsAtUnsafePoint) + SUFFIX;

		/// <summary>
		/// Can't func eval since all threads must execute (the code called <see cref="System.Diagnostics.Debugger.NotifyOfCrossThreadDependency"/>)
		/// </summary>
		public const string FuncEvalRequiresAllThreadsToRun = nameof(FuncEvalRequiresAllThreadsToRun) + SUFFIX;

		/// <summary>
		/// Can't read local or argument because it's not available at the current IP or it's been optimized away
		/// </summary>
		public const string CannotReadLocalOrArgumentMaybeOptimizedAway = nameof(CannotReadLocalOrArgumentMaybeOptimizedAway) + SUFFIX;

		/// <summary>
		/// Runtime can't read a field, local
		/// </summary>
		public const string RuntimeIsUnableToEvaluateExpression = nameof(RuntimeIsUnableToEvaluateExpression) + SUFFIX;

		// If more errors are added, also update the code in PredefinedEvaluationErrorMessagesHelper
	}

	namespace Internal {
		/// <summary>
		/// Converts <see cref="PredefinedEvaluationErrorMessages"/> values to localized strings
		/// </summary>
		public interface IPredefinedEvaluationErrorMessagesHelper {
			/// <summary>
			/// Gets a message
			/// </summary>
			/// <param name="error">An error message (eg. one in <see cref="PredefinedEvaluationErrorMessages"/>)</param>
			/// <returns></returns>
			string GetErrorMessage(string error);
		}
	}
}
