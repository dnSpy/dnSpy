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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler {
	/// <summary>
	/// Contains the compiled assembly and info on which method to evaluate to get the result of an expression
	/// </summary>
	public readonly struct DbgDotNetCompilationResult {
		/// <summary>
		/// true if it has an error message (<see cref="ErrorMessage"/>)
		/// </summary>
		public bool IsError => ErrorMessage is not null;

		/// <summary>
		/// Gets the error message or null if there was no error
		/// </summary>
		public string? ErrorMessage { get; }

		/// <summary>
		/// Gets the .NET assembly bytes or null if there was an error (<see cref="ErrorMessage"/>). It's
		/// empty if <see cref="CompiledExpressions"/> is empty.
		/// </summary>
		public byte[]? Assembly { get; }

		/// <summary>
		/// Gets the result of all compiled expressions or null if there was an error (<see cref="ErrorMessage"/>)
		/// </summary>
		public DbgDotNetCompiledExpressionResult[]? CompiledExpressions { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		public DbgDotNetCompilationResult(string errorMessage) {
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
			Assembly = null;
			CompiledExpressions = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assembly">.NET assembly bytes</param>
		/// <param name="compiledExpressions">Compiled expressions info</param>
		public DbgDotNetCompilationResult(byte[] assembly, DbgDotNetCompiledExpressionResult[] compiledExpressions) {
			ErrorMessage = null;
			Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
			CompiledExpressions = compiledExpressions ?? throw new ArgumentNullException(nameof(compiledExpressions));
		}
	}

	/// <summary>
	/// Compiled expression result flags
	/// </summary>
	[Flags]
	public enum DbgDotNetCompiledExpressionResultFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// Compiler generated variable
		/// </summary>
		CompilerGenerated			= 0x00000001,
	}

	/// <summary>
	/// Compiled expression result
	/// </summary>
	public struct DbgDotNetCompiledExpressionResult {
		/// <summary>
		/// Error message or null if no error. See also <see cref="PredefinedEvaluationErrorMessages"/>
		/// </summary>
		public string? ErrorMessage;

		/// <summary>
		/// Name of the type that contains the method (<see cref="MethodName"/>) that should be evaluated
		/// </summary>
		public string TypeName;

		/// <summary>
		/// Name of the method that should be evaluated. The declaring type is <see cref="TypeName"/>
		/// </summary>
		public string MethodName;

		/// <summary>
		/// Gets the expression that was compiled. This is eg. a C# or Visual Basic expression.
		/// </summary>
		public string Expression;

		/// <summary>
		/// Display name shown in the UI
		/// </summary>
		public DbgDotNetText Name;

		/// <summary>
		/// Gets the evaluation result flags
		/// </summary>
		public DbgEvaluationResultFlags Flags;

		/// <summary>
		/// Gets the image, see <see cref="PredefinedDbgValueNodeImageNames"/>
		/// </summary>
		public string ImageName;

		/// <summary>
		/// Gets extra custom type info or null if none
		/// </summary>
		public DbgDotNetCustomTypeInfo? CustomTypeInfo;

		/// <summary>
		/// Gets the format specifiers or null
		/// </summary>
		public ReadOnlyCollection<string>? FormatSpecifiers;

		/// <summary>
		/// Parameter/local index or -1 if unknown
		/// </summary>
		public int Index;

		/// <summary>
		/// Gets the compiled expression flags
		/// </summary>
		public DbgDotNetCompiledExpressionResultFlags ResultFlags;

		/// <summary>
		/// Creates a successful compiled expression with no error
		/// </summary>
		/// <param name="typeName">Name of type that contains the method to evaluate</param>
		/// <param name="methodName">Name of the method to evaluate</param>
		/// <param name="expression">Original expression</param>
		/// <param name="name">Display name shown in the UI</param>
		/// <param name="flags">Evaluation result flags</param>
		/// <param name="imageName">Image, see <see cref="PredefinedDbgValueNodeImageNames"/></param>
		/// <param name="customTypeInfo">Optional custom type info known by the language expression compiler and the language value formatter</param>
		/// <param name="formatSpecifiers">Format specifiers</param>
		/// <param name="resultFlags">Result flags</param>
		/// <param name="index">Parameter/local index or -1 if unknown</param>
		/// <returns></returns>
		public static DbgDotNetCompiledExpressionResult Create(string typeName, string methodName, string expression, DbgDotNetText name, DbgEvaluationResultFlags flags, string imageName, DbgDotNetCustomTypeInfo? customTypeInfo = null, ReadOnlyCollection<string>? formatSpecifiers = null, DbgDotNetCompiledExpressionResultFlags resultFlags = DbgDotNetCompiledExpressionResultFlags.None, int index = -1) {
			if (name.Parts is null)
				throw new ArgumentException();
			return new DbgDotNetCompiledExpressionResult {
				TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName)),
				MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName)),
				Expression = expression ?? throw new ArgumentNullException(nameof(expression)),
				Name = name,
				Flags = flags,
				ImageName = imageName ?? throw new ArgumentNullException(nameof(imageName)),
				CustomTypeInfo = customTypeInfo,
				FormatSpecifiers = formatSpecifiers,
				Index = index,
				ResultFlags = resultFlags,
			};
		}

		/// <summary>
		/// Creates an error
		/// </summary>
		/// <param name="expression">Expression</param>
		/// <param name="name">Display name shown in the UI</param>
		/// <param name="errorMessage">Error message, see also <see cref="PredefinedEvaluationErrorMessages"/></param>
		/// <returns></returns>
		public static DbgDotNetCompiledExpressionResult CreateError(string expression, DbgDotNetText name, string errorMessage) {
			if (name.Parts is null)
				throw new ArgumentException();
			return new DbgDotNetCompiledExpressionResult {
				ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
				Expression = expression ?? throw new ArgumentNullException(nameof(expression)),
				Name = name,
				Flags = DbgEvaluationResultFlags.ReadOnly,
				ImageName = PredefinedDbgValueNodeImageNames.Error,
			};
		}
	}
}
