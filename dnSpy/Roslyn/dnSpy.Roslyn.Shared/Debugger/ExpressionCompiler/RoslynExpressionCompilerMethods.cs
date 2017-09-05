using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace dnSpy.Roslyn.Shared.Debugger.ExpressionCompiler {
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

		public static ILSpan GetReuseSpan(List<MethodDebugScope> scopes, uint ilOffset) {
			return MethodContextReuseConstraints.CalculateReuseSpan(
				(int)ilOffset,
				ILSpan.MaxValue,
				scopes.Select(scope => new ILSpan(scope.Span.Start, scope.Span.End)));
		}

		public static ImmutableArray<string> GetLocalNames(List<MethodDebugScope> scopes, bool showAllLocals) {
			int count = -1;
			for (int i = 0; i < scopes.Count; i++) {
				foreach (var local in scopes[i].Locals) {
					if (local.Local != null && local.Local.Index > count)
						count = local.Local.Index;
				}
			}
			if (count < 0)
				return ImmutableArray<string>.Empty;
			var res = new string[count + 1];
			foreach (var scope in scopes) {
				foreach (var local in scope.Locals) {
					if (local.IsDecompilerGenerated)
						continue;
					if (!showAllLocals && (local.Local.PdbAttributes & 1) != 0)
						continue;

					res[local.Local.Index] = local.Name;
				}
			}
			return ImmutableArray.Create(res);
		}
	}
}
