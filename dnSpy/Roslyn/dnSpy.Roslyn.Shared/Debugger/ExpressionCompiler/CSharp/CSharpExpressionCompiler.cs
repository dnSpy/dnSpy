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
using System.Collections.Immutable;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.DnSpy;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace dnSpy.Roslyn.Shared.Debugger.ExpressionCompiler.CSharp {
	[ExportDbgDotNetExpressionCompiler(DbgDotNetLanguageGuids.CSharp, PredefinedDbgLanguageNames.CSharp, "C#", PredefinedDecompilerGuids.CSharp, PredefinedDbgDotNetExpressionCompilerOrders.CSharp)]
	sealed class CSharpExpressionCompiler : LanguageExpressionCompiler {
		sealed class CSharpEvalContextState : EvalContextState {
			public CSharpMetadataContext MetadataContext;
		}

		protected override ImmutableArray<ImmutableArray<DSEEImportRecord>> GetImports(TypeDef declaringType, MethodDebugScope scope, out string defaultNamespaceName) {
			var importRecordGroupBuilder = ImmutableArray.CreateBuilder<ImmutableArray<DSEEImportRecord>>();

			var type = declaringType;
			while (type.DeclaringType != null)
				type = type.DeclaringType;
			var ns = UTF8String.ToSystemStringOrEmpty(type.Namespace);
			int index = 0;
			for (;;) {
				index = ns.IndexOf('.', index);
				importRecordGroupBuilder.Add(ImmutableArray<DSEEImportRecord>.Empty);
				if (index < 0)
					break;
				index++;
			}

			var globalLevelBuilder = ImmutableArray.CreateBuilder<DSEEImportRecord>(scope.Imports.Length);
			defaultNamespaceName = null;
			foreach (var info in scope.Imports)
				AddDSEEImportRecord(globalLevelBuilder, info, ref defaultNamespaceName);
			// C# doesn't use a default namespace, only VB does, so always initialize it to the empty string
			defaultNamespaceName = string.Empty;
			importRecordGroupBuilder.Add(globalLevelBuilder.ToImmutable());
			return importRecordGroupBuilder.ToImmutable();
		}

		public override DbgDotNetCompilationResult CompileAssignment(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, DbgDotNetAlias[] aliases, string target, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			GetCompilationState<CSharpEvalContextState>(context, frame, references, out var langDebugInfo, out var method, out var localVarSigTok, out var state, out var metadataBlocks, out var methodVersion);

			var getMethodDebugInfo = CreateGetMethodDebugInfo(state, langDebugInfo);
			var evalCtx = EvaluationContext.CreateMethodContext(state.MetadataContext, metadataBlocks, getMethodDebugInfo, method.Module.Mvid ?? Guid.Empty, method.MDToken.ToInt32(), methodVersion, langDebugInfo.ILOffset, localVarSigTok);
			state.MetadataContext = new CSharpMetadataContext(metadataBlocks, evalCtx);

			var compileResult = evalCtx.CompileAssignment(target, expression, CreateAliases(aliases), out var resultProperties, out var errorMessage);
			return CreateCompilationResult(target, compileResult, resultProperties, errorMessage, DbgDotNetText.Empty);
		}

		public override DbgDotNetCompilationResult CompileGetLocals(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			GetCompilationState<CSharpEvalContextState>(context, frame, references, out var langDebugInfo, out var method, out var localVarSigTok, out var state, out var metadataBlocks, out var methodVersion);

			var getMethodDebugInfo = CreateGetMethodDebugInfo(state, langDebugInfo);
			var evalCtx = EvaluationContext.CreateMethodContext(state.MetadataContext, metadataBlocks, getMethodDebugInfo, method.Module.Mvid ?? Guid.Empty, method.MDToken.ToInt32(), methodVersion, langDebugInfo.ILOffset, localVarSigTok);
			state.MetadataContext = new CSharpMetadataContext(metadataBlocks, evalCtx);

			var asmBytes = evalCtx.CompileGetLocals(false, ImmutableArray<Alias>.Empty, out var localsInfo, out var typeName, out var errorMessage);
			return CreateCompilationResult(state, asmBytes, typeName, localsInfo, errorMessage);
		}

		public override DbgDotNetCompilationResult CompileExpression(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, DbgDotNetAlias[] aliases, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			GetCompilationState<CSharpEvalContextState>(context, frame, references, out var langDebugInfo, out var method, out var localVarSigTok, out var state, out var metadataBlocks, out var methodVersion);

			var getMethodDebugInfo = CreateGetMethodDebugInfo(state, langDebugInfo);
			var evalCtx = EvaluationContext.CreateMethodContext(state.MetadataContext, metadataBlocks, getMethodDebugInfo, method.Module.Mvid ?? Guid.Empty, method.MDToken.ToInt32(), methodVersion, langDebugInfo.ILOffset, localVarSigTok);
			state.MetadataContext = new CSharpMetadataContext(metadataBlocks, evalCtx);

			var compilationFlags = DkmEvaluationFlags.None;
			if ((options & DbgEvaluationOptions.Expression) != 0)
				compilationFlags |= DkmEvaluationFlags.TreatAsExpression;
			var compileResult = evalCtx.CompileExpression(expression, compilationFlags, CreateAliases(aliases), out var resultProperties, out var errorMessage);
			var name = compileResult == null || errorMessage != null ? CreateErrorName(expression) :
				GetExpressionText(state.MetadataContext.EvaluationContext, state.MetadataContext.Compilation, expression, cancellationToken);
			return CreateCompilationResult(expression, compileResult, resultProperties, errorMessage, name);
		}

		DbgDotNetText GetExpressionText(EvaluationContext evaluationContext, CSharpCompilation compilation, string expression, CancellationToken cancellationToken) {
			var (exprSource, exprOffset) = CreateExpressionSource(evaluationContext, expression);
			return GetExpressionText(LanguageNames.CSharp, csharpCompilationOptions, csharpParseOptions, expression, exprSource, exprOffset, compilation.References, cancellationToken);
		}
		static readonly CSharpCompilationOptions csharpCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
		static readonly CSharpParseOptions csharpParseOptions = new CSharpParseOptions(LanguageVersion.Latest);

		(string exprSource, int exprOffset) CreateExpressionSource(EvaluationContext evaluationContext, string expression) {
			var sb = ObjectCache.AllocStringBuilder();

			evaluationContext.WriteImports(sb);
			sb.Append(@"static class __C__L__A__S__S__ {");
			sb.Append(@"static void __M__E__T__H__O__D__(");
			evaluationContext.WriteParameters(sb);
			sb.Append(") {");
			evaluationContext.WriteLocals(sb);
			int exprOffset = sb.Length;
			sb.Append(expression);
			sb.Append(';');
			sb.Append('}');
			sb.Append('}');

			var exprSource = ObjectCache.FreeAndToString(ref sb);
			return (exprSource, exprOffset);
		}
	}
}
