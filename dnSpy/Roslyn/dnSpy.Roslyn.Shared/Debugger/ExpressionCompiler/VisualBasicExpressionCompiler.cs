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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.Evaluation;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator;

namespace dnSpy.Roslyn.Shared.Debugger.ExpressionCompiler {
	[ExportDbgDotNetExpressionCompiler(DbgDotNetLanguageGuids.VisualBasic, PredefinedDbgLanguageNames.VisualBasic, "Visual Basic", PredefinedDecompilerGuids.VisualBasic)]
	sealed class VisualBasicExpressionCompiler : LanguageExpressionCompiler {
		sealed class VisualBasicEvalContextState : EvalContextState {
			public VisualBasicMetadataContext MetadataContext;
		}

		public override DbgDotNetCompilationResult CompileAssignment(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, DbgDotNetAlias[] aliases, string target, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			throw new NotImplementedException();//TODO:
		}

		public override DbgDotNetCompilationResult CompileGetLocals(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			GetCompileGetLocalsState<VisualBasicEvalContextState>(context, frame, references, out var langDebugInfo, out var method, out var localVarSigTok, out var state, out var metadataBlocks, out var methodVersion);

			var getMethodDebugInfo = CreateGetMethodDebugInfo(state, langDebugInfo, calcDefaultNamespaceName: true);
			var evalCtx = EvaluationContext.CreateMethodContext(state.MetadataContext, metadataBlocks, null, getMethodDebugInfo, method.Module.Mvid ?? Guid.Empty, method.MDToken.ToInt32(), methodVersion, langDebugInfo.ILOffset, localVarSigTok);
			state.MetadataContext = new VisualBasicMetadataContext(metadataBlocks, evalCtx);

			var asmBytes = evalCtx.CompileGetLocals(false, ImmutableArray<Alias>.Empty, out var localsInfo, out var typeName);
			return CreateCompilationResult(asmBytes, typeName, localsInfo);
		}

		public override DbgDotNetCompilationResult CompileExpressions(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, DbgDotNetAlias[] aliases, string[] expressions, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			throw new NotImplementedException();//TODO:
		}
	}
}
