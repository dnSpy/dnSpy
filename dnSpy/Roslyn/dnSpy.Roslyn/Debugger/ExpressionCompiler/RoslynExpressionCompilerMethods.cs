using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace dnSpy.Roslyn.Debugger.ExpressionCompiler {
	static class RoslynExpressionCompilerMethods {
		public static void GetAllScopes(MethodDebugScope rootScope, List<MethodDebugScope> stack, List<MethodDebugScope> allScopes, List<MethodDebugScope> containingScopes, uint ilOffset) {
			stack.Add(rootScope);
			while (stack.Count > 0) {
				var scope = stack[stack.Count - 1];
				stack.RemoveAt(stack.Count - 1);
				allScopes.Add(scope);
				if (scope.Span.Start <= ilOffset && ilOffset < scope.Span.End)
					containingScopes.Add(scope);

				foreach (var nested in scope.Scopes)
					stack.Add(nested);
			}
		}

		public static Microsoft.CodeAnalysis.ExpressionEvaluator.ILSpan GetReuseSpan(List<MethodDebugScope> scopes, uint ilOffset) {
			return MethodContextReuseConstraints.CalculateReuseSpan(
				(int)ilOffset,
				Microsoft.CodeAnalysis.ExpressionEvaluator.ILSpan.MaxValue,
				scopes.Select(scope => new Microsoft.CodeAnalysis.ExpressionEvaluator.ILSpan(scope.Span.Start, scope.Span.End)));
		}

		public static ImmutableArray<string> GetLocalNames(int totalLocals, List<MethodDebugScope> scopes, CompilerGeneratedVariableInfo[] compilerGeneratedVariables) {
			if (totalLocals == 0)
				return ImmutableArray<string>.Empty;
			var res = new string[totalLocals];
			foreach (var scope in scopes) {
				foreach (var local in scope.Locals) {
					if (local.IsDecompilerGenerated)
						continue;
					if (local.Local == null)
						continue;

					res[local.Local.Index] = local.Name;
				}
			}
			foreach (var info in compilerGeneratedVariables) {
				Debug.Assert(res[info.Index] == null);
				if (res[info.Index] == null)
					res[info.Index] = info.Name;
			}
			return ImmutableArray.Create(res);
		}
	}
}
