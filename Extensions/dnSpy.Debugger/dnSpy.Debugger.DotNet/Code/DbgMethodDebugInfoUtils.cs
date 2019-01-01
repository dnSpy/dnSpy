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
using System.Diagnostics;
using dnlib.DotNet.Pdb;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Debugger.DotNet.Code {
	static class DbgMethodDebugInfoUtils {
		public static DbgMethodDebugInfo ToDbgMethodDebugInfo(MethodDebugInfo debugInfo) =>
			new DbgMethodDebugInfo(ToCompiler(debugInfo.CompilerName), debugInfo.DecompilerSettingsVersion, debugInfo.Method,
				ToParameters(debugInfo.Parameters), ToSourceStatements(debugInfo.Statements),
				ToScope(debugInfo.Scope), ToAsyncMethodDebugInfo(debugInfo.AsyncInfo));

		static DbgCompilerKind ToCompiler(string compilerName) {
			if (compilerName == null)
				return DbgCompilerKind.Unknown;
			switch (compilerName) {
			case PredefinedCompilerNames.MicrosoftCSharp:		return DbgCompilerKind.MicrosoftCSharp;
			case PredefinedCompilerNames.MicrosoftVisualBasic:	return DbgCompilerKind.MicrosoftVisualBasic;
			case PredefinedCompilerNames.MonoCSharp:			return DbgCompilerKind.MonoCSharp;
			default:
				Debug.Fail($"Unknown compiler name: {compilerName}");
				return DbgCompilerKind.Unknown;
			}
		}

		static DbgParameter[] ToParameters(SourceParameter[] parameters) {
			if (parameters.Length == 0)
				return Array.Empty<DbgParameter>();
			var res = new DbgParameter[parameters.Length];
			for (int i = 0; i < res.Length; i++) {
				var p = parameters[i];
				res[i] = new DbgParameter(p.Parameter.Index, p.Name ?? string.Empty);
			}
			return res;
		}

		static DbgSourceStatement[] ToSourceStatements(SourceStatement[] statements) {
			if (statements.Length == 0)
				return Array.Empty<DbgSourceStatement>();
			var res = new DbgSourceStatement[statements.Length];
			for (int i = 0; i < res.Length; i++) {
				var s = statements[i];
				res[i] = new DbgSourceStatement(new DbgILSpan(s.ILSpan.Start, s.ILSpan.Length), new DbgTextSpan(s.TextSpan.Start, s.TextSpan.Length));
			}
			return res;
		}

		static DbgMethodDebugScope ToScope(MethodDebugScope scope) {
			var scopes = scope.Scopes;
			var newScopes = scopes.Length == 0 ? Array.Empty<DbgMethodDebugScope>() : new DbgMethodDebugScope[scopes.Length];
			for (int i = 0; i < scopes.Length; i++)
				newScopes[i] = ToScope(scopes[i]);

			return new DbgMethodDebugScope(new DbgILSpan(scope.Span.Start, scope.Span.Length), newScopes, ToLocals(scope.Locals), ToImportInfo(scope.Imports));
		}

		static DbgLocal[] ToLocals(SourceLocal[] locals) {
			if (locals.Length == 0)
				return Array.Empty<DbgLocal>();
			var res = new DbgLocal[locals.Length];
			for (int i = 0; i < res.Length; i++) {
				var l = locals[i];
				var flags = DbgLocalFlags.None;
				if (l.IsDecompilerGenerated)
					flags |= DbgLocalFlags.DecompilerGenerated;
				int index;
				var local = l.Local;
				if (local == null)
					index = -1;
				else {
					index = local.Index;
					if ((local.Attributes & PdbLocalAttributes.DebuggerHidden) != 0)
						flags |= DbgLocalFlags.DebuggerHidden;
				}
				res[i] = new DbgLocal(index, l.Name ?? string.Empty, l.HoistedField, flags);
			}
			return res;
		}

		static DbgImportInfo[] ToImportInfo(ImportInfo[] imports) {
			if (imports.Length == 0)
				return Array.Empty<DbgImportInfo>();
			var res = new DbgImportInfo[imports.Length];
			for (int i = 0; i < res.Length; i++) {
				var imp = imports[i];
				res[i] = new DbgImportInfo(ToDbgImportInfoKind(imp.TargetKind), imp.Target, imp.Alias, imp.ExternAlias, ToDbgVBImportScopeKind(imp.VBImportScopeKind));
			}
			return res;
		}

		static DbgImportInfoKind ToDbgImportInfoKind(ImportInfoKind kind) {
			switch (kind) {
			case ImportInfoKind.Namespace:			return DbgImportInfoKind.Namespace;
			case ImportInfoKind.Type:				return DbgImportInfoKind.Type;
			case ImportInfoKind.NamespaceOrType:	return DbgImportInfoKind.NamespaceOrType;
			case ImportInfoKind.Assembly:			return DbgImportInfoKind.Assembly;
			case ImportInfoKind.XmlNamespace:		return DbgImportInfoKind.XmlNamespace;
			case ImportInfoKind.MethodToken:		return DbgImportInfoKind.MethodToken;
			case ImportInfoKind.CurrentNamespace:	return DbgImportInfoKind.CurrentNamespace;
			case ImportInfoKind.DefaultNamespace:	return DbgImportInfoKind.DefaultNamespace;
			default:
				Debug.Fail($"Unknown import: {kind}");
				return (DbgImportInfoKind)(-1);
			}
		}

		static DbgVBImportScopeKind ToDbgVBImportScopeKind(VBImportScopeKind kind) {
			switch (kind) {
			case VBImportScopeKind.None:		return DbgVBImportScopeKind.None;
			case VBImportScopeKind.File:		return DbgVBImportScopeKind.File;
			case VBImportScopeKind.Project:		return DbgVBImportScopeKind.Project;
			default:
				Debug.Fail($"Unknown VB import: {kind}");
				return DbgVBImportScopeKind.None;
			}
		}

		static DbgAsyncMethodDebugInfo ToAsyncMethodDebugInfo(AsyncMethodDebugInfo asyncInfo) {
			if (asyncInfo == null)
				return null;

			var stepInfos = asyncInfo.StepInfos;
			var newStepInfos = stepInfos.Length == 0 ? Array.Empty<DbgAsyncStepInfo>() : new DbgAsyncStepInfo[stepInfos.Length];
			for (int i = 0; i < stepInfos.Length; i++)
				newStepInfos[i] = new DbgAsyncStepInfo(stepInfos[i].YieldOffset, stepInfos[i].ResumeMethod, stepInfos[i].ResumeOffset);

			return new DbgAsyncMethodDebugInfo(newStepInfos, asyncInfo.BuilderFieldOrNull, asyncInfo.CatchHandlerOffset, asyncInfo.SetResultOffset);
		}
	}
}
