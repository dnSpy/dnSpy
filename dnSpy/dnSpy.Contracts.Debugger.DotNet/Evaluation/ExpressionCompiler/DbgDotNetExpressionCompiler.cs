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
using System.ComponentModel.Composition;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler {
	/// <summary>
	/// A .NET expression compiler. Use <see cref="ExportDbgDotNetExpressionCompilerAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgDotNetExpressionCompiler {
		/// <summary>
		/// Compiles an expression
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="references">.NET module references</param>
		/// <param name="aliases">Aliases</param>
		/// <param name="expression">Expression</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgDotNetCompilationResult CompileExpression(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, DbgDotNetAlias[] aliases, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an assembly that is used to get all the locals
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="references">.NET module references</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgDotNetCompilationResult CompileGetLocals(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, DbgEvaluationOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Compiles an assignment
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="references">.NET module references</param>
		/// <param name="aliases">Aliases</param>
		/// <param name="target">Target expression (lhs)</param>
		/// <param name="expression">Expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgDotNetCompilationResult CompileAssignment(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, DbgDotNetAlias[] aliases, string target, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken);
	}

	/// <summary>Metadata</summary>
	public interface IDbgDotNetExpressionCompilerMetadata {
		/// <summary>See <see cref="ExportDbgDotNetExpressionCompilerAttribute.LanguageGuid"/></summary>
		string LanguageGuid { get; }
		/// <summary>See <see cref="ExportDbgDotNetExpressionCompilerAttribute.LanguageName"/></summary>
		string LanguageName { get; }
		/// <summary>See <see cref="ExportDbgDotNetExpressionCompilerAttribute.LanguageDisplayName"/></summary>
		string LanguageDisplayName { get; }
		/// <summary>See <see cref="ExportDbgDotNetExpressionCompilerAttribute.DecompilerGuid"/></summary>
		string DecompilerGuid { get; }
		/// <summary>See <see cref="ExportDbgDotNetExpressionCompilerAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgDotNetExpressionCompiler"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgDotNetExpressionCompilerAttribute : ExportAttribute, IDbgDotNetExpressionCompilerMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="languageGuid">Language GUID, see <see cref="DbgDotNetLanguageGuids"/></param>
		/// <param name="languageName">Language name, see <see cref="PredefinedDbgLanguageNames"/></param>
		/// <param name="languageDisplayName">Language's display name (shown in the UI)</param>
		/// <param name="decompilerGuid">Decompiler GUID, see <see cref="PredefinedDecompilerGuids"/> or one of the decompiler GUIDs (<see cref="DecompilerConstants"/>)</param>
		/// <param name="order">Order</param>
		public ExportDbgDotNetExpressionCompilerAttribute(string languageGuid, string languageName, string languageDisplayName, string decompilerGuid, double order = double.MaxValue)
			: base(typeof(DbgDotNetExpressionCompiler)) {
			LanguageGuid = languageGuid ?? throw new ArgumentNullException(nameof(languageGuid));
			LanguageName = languageName ?? throw new ArgumentNullException(nameof(languageName));
			LanguageDisplayName = languageDisplayName ?? throw new ArgumentNullException(nameof(languageDisplayName));
			DecompilerGuid = decompilerGuid ?? throw new ArgumentNullException(nameof(decompilerGuid));
			Order = order;
		}

		/// <summary>
		/// Language GUID, see <see cref="DbgDotNetLanguageGuids"/>
		/// </summary>
		public string LanguageGuid { get; }

		/// <summary>
		/// Gets the language name, see <see cref="PredefinedDbgLanguageNames"/>
		/// </summary>
		public string LanguageName { get; }

		/// <summary>
		/// Gets the language's display name (shown in the UI)
		/// </summary>
		public string LanguageDisplayName { get; }

		/// <summary>
		/// Gets the decompiler GUID, see <see cref="PredefinedDecompilerGuids"/> or one of the decompiler GUIDs (<see cref="DecompilerConstants"/>)
		/// </summary>
		public string DecompilerGuid { get; }

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}

	/// <summary>
	/// Order of known expression compilers
	/// </summary>
	public static class PredefinedDbgDotNetExpressionCompilerOrders {
		/// <summary>
		/// Order of C# expression compiler
		/// </summary>
		public const double CSharp = 1000000;

		/// <summary>
		/// Order of Visual Basic expression compiler
		/// </summary>
		public const double VisualBasic = 2000000;
	}
}
