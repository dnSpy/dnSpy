using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.DotNet.Code;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace dnSpy.Roslyn.Debugger.ExpressionCompiler {
	static class RoslynExpressionCompilerMethods {
		public static void GetAllScopes(DbgMethodDebugScope rootScope, List<DbgMethodDebugScope> stack, List<DbgMethodDebugScope> allScopes, List<DbgMethodDebugScope> containingScopes, uint ilOffset) {
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

		public static Microsoft.CodeAnalysis.ExpressionEvaluator.ILSpan GetReuseSpan(List<DbgMethodDebugScope> scopes, uint ilOffset) {
			return MethodContextReuseConstraints.CalculateReuseSpan(
				(int)ilOffset,
				Microsoft.CodeAnalysis.ExpressionEvaluator.ILSpan.MaxValue,
				scopes.Select(scope => new Microsoft.CodeAnalysis.ExpressionEvaluator.ILSpan(scope.Span.Start, scope.Span.End)));
		}

		public static ImmutableArray<string> GetLocalNames(int totalLocals, List<DbgMethodDebugScope> scopes, CompilerGeneratedVariableInfo[] compilerGeneratedVariables) {
			if (totalLocals == 0)
				return ImmutableArray<string>.Empty;
			var res = new string[totalLocals];
			foreach (var scope in scopes) {
				foreach (var local in scope.Locals) {
					if (local.IsDecompilerGenerated)
						continue;
					if (local.Index < 0)
						continue;

					res[local.Index] = local.Name;
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
