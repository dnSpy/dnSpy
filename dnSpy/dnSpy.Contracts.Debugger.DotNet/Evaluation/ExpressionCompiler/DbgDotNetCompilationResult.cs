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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler {
	/// <summary>
	/// Contains the compiled assembly and info on which method to evaluate to get the result of an expression
	/// </summary>
	public struct DbgDotNetCompilationResult {
		/// <summary>
		/// Gets the .NET assembly bytes
		/// </summary>
		public byte[] Assembly { get; }

		/// <summary>
		/// Gets the result of all compiled expressions
		/// </summary>
		public DbgDotNetCompiledExpressionResult[] CompiledExpressions { get; }
	}

	/// <summary>
	/// Compiled expression result
	/// </summary>
	public struct DbgDotNetCompiledExpressionResult {
		/// <summary>
		/// Error message or null if no error. See also <see cref="PredefinedEvaluationErrorMessages"/>
		/// </summary>
		public string ErrorMessage;

		/// <summary>
		/// Name of the type that contains the method (<see cref="MethodName"/>) that should be evaluated
		/// </summary>
		public string TypeName;

		/// <summary>
		/// Name of the method that should be evaluated. The declaring type is <see cref="TypeName"/>
		/// </summary>
		public string MethodName;

		/// <summary>
		/// Gets the expression that was evaluated. This is eg. a C# or Visual Basic expression.
		/// </summary>
		public string Expression;

		/// <summary>
		/// Gets the evaluation result flags
		/// </summary>
		public DbgEvaluationResultFlags Flags;

		/// <summary>
		/// Gets the image, see <see cref="PredefinedDbgValueNodeImageNames"/>
		/// </summary>
		public string ImageName;

		/// <summary>
		/// Creates a successful compiled expression with no error
		/// </summary>
		/// <param name="typeName">Name of type that contains the method to evaluate</param>
		/// <param name="methodName">Name of the method to evaluate</param>
		/// <param name="expression">Original expression</param>
		/// <param name="flags">Evaluation result flags</param>
		/// <param name="imageName">Image, see <see cref="PredefinedDbgValueNodeImageNames"/></param>
		/// <returns></returns>
		public static DbgDotNetCompiledExpressionResult Create(string typeName, string methodName, string expression, DbgEvaluationResultFlags flags, string imageName) {
			return new DbgDotNetCompiledExpressionResult {
				TypeName = typeName,
				MethodName = methodName,
				Expression = expression,
				Flags = flags,
				ImageName = imageName,
			};
		}

		/// <summary>
		/// Creates an error
		/// </summary>
		/// <param name="expression">Expression</param>
		/// <param name="errorMessage">Error message, see also <see cref="PredefinedEvaluationErrorMessages"/></param>
		/// <returns></returns>
		public static DbgDotNetCompiledExpressionResult CreateError(string expression, string errorMessage) {
			return new DbgDotNetCompiledExpressionResult {
				ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
				Expression = expression ?? throw new ArgumentNullException(nameof(expression)),
				Flags = DbgEvaluationResultFlags.ReadOnly,
				ImageName = PredefinedDbgValueNodeImageNames.Error,
			};
		}
	}
}
