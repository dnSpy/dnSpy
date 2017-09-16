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

using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Common error messages
	/// </summary>
	public static class PredefinedEvaluationErrorMessages {
		const string SUFFIX = " <({[dnSpy]})> ";

		/// <summary>
		/// <see cref="DbgEvaluationOptions.NoSideEffects"/> is set but expression causes side effects
		/// </summary>
		public const string ExpressionCausesSideEffects = nameof(ExpressionCausesSideEffects) + SUFFIX;

		/// <summary>
		/// <see cref="DbgValueNodeEvaluationOptions.NoFuncEval"/> or <see cref="DbgEvaluationOptions.NoFuncEval"/>
		/// is set but code must call a method
		/// </summary>
		public const string FunctionEvaluationDisabled = nameof(FunctionEvaluationDisabled) + SUFFIX;

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
