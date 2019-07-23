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
using System.IO;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code.FilterExpressionEvaluator;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Debugger.Text.DnSpy;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Roslyn.Properties;
using dnSpy.Roslyn.Text;
using dnSpy.Roslyn.Text.Classification;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Debugger.FilterExpressionEvaluator {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class ClearCompiledExpressions : IDbgManagerStartListener {
		readonly DbgFilterExpressionEvaluatorImpl dbgFilterExpressionEvaluatorImpl;
		[ImportingConstructor]
		ClearCompiledExpressions(DbgFilterExpressionEvaluatorImpl dbgFilterExpressionEvaluatorImpl) => this.dbgFilterExpressionEvaluatorImpl = dbgFilterExpressionEvaluatorImpl;
		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
		void DbgManager_IsDebuggingChanged(object? sender, EventArgs e) {
			var dbgManager = (DbgManager)sender!;
			dbgFilterExpressionEvaluatorImpl.OnIsDebuggingChanged(dbgManager.IsDebugging);
		}
	}

	[ExportDbgFilterExpressionEvaluator(double.PositiveInfinity)]
	[Export(typeof(DbgFilterExpressionEvaluatorImpl))]
	sealed class DbgFilterExpressionEvaluatorImpl : DbgFilterExpressionEvaluator {
		readonly object lockObj;
		Dictionary<string, CompiledExpr> toCompiledExpr;
		WeakReference? toCompiledExprWeakRef;

		sealed class CompiledExpr {
			public EvalDelegate? Eval { get; }
			public string? CompilationError { get; }
			public string? RuntimeError { get; set; }
			public CompiledExpr(EvalDelegate eval) => Eval = eval ?? throw new ArgumentNullException(nameof(eval));
			public CompiledExpr(string compilationError) => CompilationError = compilationError ?? throw new ArgumentNullException(nameof(compilationError));
		}

		[ImportingConstructor]
		DbgFilterExpressionEvaluatorImpl(Lazy<IThemeClassificationTypeService> themeClassificationTypeService) {
			lockObj = new object();
			toCompiledExpr = CreateCompiledExprDict();
		}

		static Dictionary<string, CompiledExpr> CreateCompiledExprDict() => new Dictionary<string, CompiledExpr>(StringComparer.Ordinal);

		internal void OnIsDebuggingChanged(bool isDebugging) {
			lock (lockObj) {
				// Keep the compiled expressions if possible (eg. user presses Restart button)
				if (isDebugging) {
					toCompiledExpr = toCompiledExprWeakRef?.Target as Dictionary<string, CompiledExpr> ?? toCompiledExpr ?? CreateCompiledExprDict();
					toCompiledExprWeakRef = null;
				}
				else {
					toCompiledExprWeakRef = new WeakReference(toCompiledExpr);
					toCompiledExpr = CreateCompiledExprDict();
				}
			}
		}

		public override string? IsValidExpression(string expr) {
			if (expr is null)
				throw new ArgumentNullException(nameof(expr));
			lock (lockObj) {
				if (toCompiledExpr.TryGetValue(expr, out var compiledExpr))
					return compiledExpr.CompilationError;
			}
			return Compile(expr, verifyExpr: true).error;
		}

		public override DbgFilterExpressionEvaluatorResult Evaluate(string expr, DbgFilterEEVariableProvider variableProvider) {
			if (expr is null)
				throw new ArgumentNullException(nameof(expr));
			if (variableProvider is null)
				throw new ArgumentNullException(nameof(variableProvider));
			var compiledExpr = GetOrCompile(expr);
			if (!(compiledExpr.CompilationError is null))
				return new DbgFilterExpressionEvaluatorResult(compiledExpr.CompilationError);
			if (!(compiledExpr.RuntimeError is null))
				return new DbgFilterExpressionEvaluatorResult(compiledExpr.RuntimeError);

			bool evalResult;
			try {
				evalResult = compiledExpr.Eval!(variableProvider.MachineName, variableProvider.ProcessId, variableProvider.ProcessName, variableProvider.ThreadId, variableProvider.ThreadName);
			}
			catch (Exception ex) {
				compiledExpr.RuntimeError = string.Format(dnSpy_Roslyn_Resources.FilterExpressionEvaluator_CompiledExpressionThrewAnException, ex.GetType().FullName);
				return new DbgFilterExpressionEvaluatorResult(compiledExpr.RuntimeError);
			}
			return new DbgFilterExpressionEvaluatorResult(evalResult);
		}

		public override void Write(IDbgTextWriter output, string expr) {
			using (var workspace = new AdhocWorkspace(RoslynMefHostServices.DefaultServices)) {
				var projectId = ProjectId.CreateNewId();
				var (filterText, exprOffset) = CreateFilterClassSource(expr);
				var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "FEE", Guid.NewGuid().ToString(), LanguageNames.CSharp,
					compilationOptions: compilationOptions
							.WithOptimizationLevel(OptimizationLevel.Release)
							.WithPlatform(Platform.AnyCpu),
					parseOptions: parseOptions,
					isSubmission: false, hostObjectType: null);
				workspace.AddProject(projectInfo);
				var doc = workspace.AddDocument(projectId, "FEE.cs", SourceText.From(filterText));

				var syntaxRoot = doc.GetSyntaxRootAsync().GetAwaiter().GetResult();
				var semanticModel = doc.GetSemanticModelAsync().GetAwaiter().GetResult();
				var classifier = new RoslynClassifier(syntaxRoot, semanticModel, workspace, RoslynClassificationTypes.Default, null, CancellationToken.None);
				var textSpan = new TextSpan(exprOffset, expr.Length);

				int pos = textSpan.Start;
				var paramColor = RoslynClassificationTypes.Default.Parameter;
				var propColor = RoslynClassificationTypes.Default.InstanceProperty;
				foreach (var info in classifier.GetColors(textSpan)) {
					if (pos < info.Span.Start)
						output.Write(DbgTextColor.Text, expr.Substring(pos - textSpan.Start, info.Span.Start - pos));
					output.Write(ColorConverter.ToDebuggerColor(info.Color == paramColor ? propColor : info.Color), expr.Substring(info.Span.Start - textSpan.Start, info.Span.Length));
					pos = info.Span.End;
				}
				if (pos < textSpan.End)
					output.Write(DbgTextColor.Text, expr.Substring(pos - textSpan.Start, textSpan.End - pos));
			}
		}

		CompiledExpr GetOrCompile(string expr) {
			lock (lockObj) {
				if (toCompiledExpr.TryGetValue(expr, out var compiledExpr))
					return compiledExpr;
				compiledExpr = CreateCompiledExpr(expr);
				toCompiledExpr.Add(expr, compiledExpr);
				return compiledExpr;
			}
		}

		CompiledExpr CreateCompiledExpr(string expr) {
			var compRes = Compile(expr);
			if (!(compRes.error is null))
				return new CompiledExpr(compRes.error);

			try {
				using (var delCreator = new EvalDelegateCreator(compRes.assembly!, FilterExpressionClassName, EvalMethodName)) {
					var del = delCreator.CreateDelegate();
					if (!(del is null))
						return new CompiledExpr(del);
				}
			}
			catch (EvalDelegateCreatorException) {
			}
			return new CompiledExpr(dnSpy_Roslyn_Resources.FilterExpressionEvaluator_InvalidExpression);
		}

		const string FilterExpressionClassName = "FilterExpressionClass";
		const string EvalMethodName = "__EVAL__";
		static readonly CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
		static readonly CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
		static readonly SyntaxTree mscorlibSyntaxTree = CSharpSyntaxTree.ParseText(@"
namespace System {
	public class Object { }
	public abstract class ValueType { }
	public struct Void { }
	public struct Boolean { }
	public struct Int32 { }
	public struct UInt32 { }
	public struct UInt64 { }
	public sealed class String {
		public static bool operator ==(String left, String right) => false;
		public static bool operator !=(String left, String right) => false;
	}
}
", parseOptions);

		(string exprSource, int exprOffset) CreateFilterClassSource(string expr) {
			var filterText = @"
static class " + FilterExpressionClassName + @" {
	public static bool " + EvalMethodName + @"(string MachineName, int ProcessId, string ProcessName, ulong ThreadId, string ThreadName) =>
#line 1
" + expr + @";
}
";
			const string pattern = "#line 1";
			int exprOffset = filterText.IndexOf(pattern);
			Debug.Assert(exprOffset >= 0);
			exprOffset += pattern.Length;
			exprOffset += filterText[exprOffset] == '\r' ? 2 : 1;
			Debug.Assert(filterText[exprOffset - 1] == '\n');
			Debug.Assert(filterText.Substring(exprOffset, expr.Length) == expr);
			return (filterText, exprOffset);
		}

		(byte[]? assembly, string? error) Compile(string expr, bool verifyExpr = false) {
			var info = CreateFilterClassSource(expr);
			var filterExprClass = CSharpSyntaxTree.ParseText(info.exprSource, parseOptions);
			var comp = CSharpCompilation.Create("filter-expr-eval", new[] { mscorlibSyntaxTree, filterExprClass }, options: compilationOptions);
			var peStream = new MemoryStream();
			EmitResult emitResult;
			try {
				emitResult = comp.Emit(peStream);
			}
			catch (Exception ex) {
				return (null, $"Internal compiler error: {ex.GetType().FullName}: {ex.Message}");
			}
			if (!emitResult.Success) {
				var error = emitResult.Diagnostics.FirstOrDefault(a => a.Severity == DiagnosticSeverity.Error)?.ToString() ?? "Unknown error";
				return (null, error);
			}
			if (verifyExpr)
				return (null, null);
			return (peStream.ToArray(), null);
		}
	}
}
