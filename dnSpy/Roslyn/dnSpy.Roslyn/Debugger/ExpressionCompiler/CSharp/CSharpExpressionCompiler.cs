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
using System.Collections.Immutable;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.DnSpy;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace dnSpy.Roslyn.Debugger.ExpressionCompiler.CSharp {
	[ExportDbgDotNetExpressionCompiler(DbgDotNetLanguageGuids.CSharp, PredefinedDbgLanguageNames.CSharp, "C#", PredefinedDecompilerGuids.CSharp, PredefinedDbgDotNetExpressionCompilerOrders.CSharp)]
	sealed class CSharpExpressionCompiler : LanguageExpressionCompiler {
		protected override bool IsCaseSensitive => true;

		sealed class CSharpEvalContextState : EvalContextState {
			public CSharpMetadataContext MetadataContext;
		}

		protected override ImmutableArray<ImmutableArray<DSEEImportRecord>> GetImports(TypeDef declaringType, DbgMethodDebugScope scope, out string defaultNamespaceName) {
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

		public override DbgDotNetCompilationResult CompileAssignment(DbgEvaluationInfo evalInfo, DbgModuleReference[] references, DbgDotNetAlias[] aliases, string target, string expression, DbgEvaluationOptions options) {
			GetCompilationState<CSharpEvalContextState>(evalInfo, references, out var langDebugInfo, out var method, out var methodToken, out var localVarSigTok, out var state, out var metadataBlocks, out var methodVersion);

			var getMethodDebugInfo = CreateGetMethodDebugInfo(state, langDebugInfo);
			var evalCtx = EvaluationContext.CreateMethodContext(state.MetadataContext, metadataBlocks, getMethodDebugInfo, method.Module.Mvid ?? Guid.Empty, methodToken, methodVersion, langDebugInfo.ILOffset, localVarSigTok);
			state.MetadataContext = new CSharpMetadataContext(evalCtx.Compilation, evalCtx);

			var compileResult = evalCtx.CompileAssignment(target, expression, CreateAliases(aliases), out var resultProperties, out var errorMessage);
			return CreateCompilationResult(target, compileResult, resultProperties, errorMessage, DbgDotNetText.Empty);
		}

		public override DbgDotNetCompilationResult CompileGetLocals(DbgEvaluationInfo evalInfo, DbgModuleReference[] references, DbgEvaluationOptions options) {
			GetCompilationState<CSharpEvalContextState>(evalInfo, references, out var langDebugInfo, out var method, out var methodToken, out var localVarSigTok, out var state, out var metadataBlocks, out var methodVersion);

			var getMethodDebugInfo = CreateGetMethodDebugInfo(state, langDebugInfo);
			var evalCtx = EvaluationContext.CreateMethodContext(state.MetadataContext, metadataBlocks, getMethodDebugInfo, method.Module.Mvid ?? Guid.Empty, methodToken, methodVersion, langDebugInfo.ILOffset, localVarSigTok);
			state.MetadataContext = new CSharpMetadataContext(evalCtx.Compilation, evalCtx);

			if ((options & DbgEvaluationOptions.RawLocals) == 0) {
				var asmBytes = evalCtx.CompileGetLocals(false, ImmutableArray<Alias>.Empty, out var localsInfo, out var typeName, out var errorMessage);
				var res = CreateCompilationResult(state, asmBytes, typeName, localsInfo, errorMessage);
				if (!res.IsError)
					return res;
			}
			return CompileGetLocals(state, method);
		}

		public override DbgDotNetCompilationResult CompileExpression(DbgEvaluationInfo evalInfo, DbgModuleReference[] references, DbgDotNetAlias[] aliases, string expression, DbgEvaluationOptions options) {
			GetCompilationState<CSharpEvalContextState>(evalInfo, references, out var langDebugInfo, out var method, out var methodToken, out var localVarSigTok, out var state, out var metadataBlocks, out var methodVersion);

			var getMethodDebugInfo = CreateGetMethodDebugInfo(state, langDebugInfo);
			var evalCtx = EvaluationContext.CreateMethodContext(state.MetadataContext, metadataBlocks, getMethodDebugInfo, method.Module.Mvid ?? Guid.Empty, methodToken, methodVersion, langDebugInfo.ILOffset, localVarSigTok);

			return CompileExpressionCore(aliases, expression, options, state, evalCtx, evalInfo.CancellationToken);
		}

		DbgDotNetCompilationResult CompileExpressionCore(DbgDotNetAlias[] aliases, string expression, DbgEvaluationOptions options, CSharpEvalContextState state, EvaluationContext evalCtx, CancellationToken cancellationToken) {
			state.MetadataContext = new CSharpMetadataContext(evalCtx.Compilation, evalCtx);
			var compilationFlags = DkmEvaluationFlags.None;
			if ((options & DbgEvaluationOptions.Expression) != 0)
				compilationFlags |= DkmEvaluationFlags.TreatAsExpression;
			var compileResult = evalCtx.CompileExpression(expression, compilationFlags, CreateAliases(aliases), out var resultProperties, out var errorMessage);
			DbgDotNetText name;
			if ((options & DbgEvaluationOptions.NoName) != 0)
				name = DbgDotNetText.Empty;
			else if (compileResult == null || errorMessage != null)
				name = CreateErrorName(expression);
			else
				name = GetExpressionText(state.MetadataContext.EvaluationContext, state.MetadataContext.Compilation, expression, cancellationToken);
			return CreateCompilationResult(expression, compileResult, resultProperties, errorMessage, name);
		}

		DbgDotNetText GetExpressionText(EvaluationContext evaluationContext, CSharpCompilation compilation, string expression, CancellationToken cancellationToken) {
			if (TryGetAliasInfo(expression, out var aliasInfo))
				return CreateText(aliasInfo.Kind, expression);
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

		public override DbgDotNetCompilationResult CompileTypeExpression(DbgEvaluationInfo evalInfo, DmdType type, DbgModuleReference[] references, DbgDotNetAlias[] aliases, string expression, DbgEvaluationOptions options) {
			GetTypeCompilationState<CSharpEvalContextState>(evalInfo, references, out var state, out var metadataBlocks);
			var evalCtx = EvaluationContext.CreateTypeContext(state.MetadataContext.Compilation, type.Module.ModuleVersionId, type.MetadataToken);
			return CompileExpressionCore(aliases, expression, options, state, evalCtx, evalInfo.CancellationToken);
		}

		internal override string GetVariableName(string metadataName, bool isThis) {
			if (isThis)
				return "this";
			return Formatters.CSharp.CSharpTypeFormatter.GetFormattedIdentifier(metadataName);
		}
	}
}
