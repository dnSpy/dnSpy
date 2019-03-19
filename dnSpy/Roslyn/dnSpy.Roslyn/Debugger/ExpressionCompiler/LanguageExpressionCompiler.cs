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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Debugger.Text.DnSpy;
using dnSpy.Contracts.Decompiler;
using dnSpy.Roslyn.Text;
using dnSpy.Roslyn.Text.Classification;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.DnSpy;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace dnSpy.Roslyn.Debugger.ExpressionCompiler {
	readonly struct CompilerGeneratedVariableInfo {
		public int Index { get; }
		public string Name { get; }
		public CompilerGeneratedVariableInfo(int index, string name) {
			Index = index;
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}
	}

	abstract class LanguageExpressionCompiler : DbgDotNetExpressionCompiler {
		protected abstract class EvalContextState {
			public DbgModuleReference[] LastModuleReferences;
			public ImmutableArray<MetadataBlock> LastMetadataBlocks;

			public DSEEMethodDebugInfo MethodDebugInfo;
			public object MethodDebugInfoKey;

			public CompilerGeneratedVariableInfo[] CompilerGeneratedVariableInfos;
			public bool[] NotCompilerGenerated;
			public object CompilerGeneratedVariableInfosKey;
		}

		T GetEvalContextState<T>(DbgStackFrame frame) where T : EvalContextState, new() {
			// We attach the state to the module, and not to the app domain, so the Roslyn compilation
			// doesn't get recreated everytime we change the module.
			var module = frame.Module;
			if (module == null)
				throw new InvalidOperationException();
			return module.GetOrCreateData<T>();
		}

		protected void GetCompilationState<T>(DbgEvaluationInfo evalInfo, DbgModuleReference[] references, out DbgLanguageDebugInfo langDebugInfo, out MethodDef method, out int methodToken, out int localVarSigTok, out T state, out ImmutableArray<MetadataBlock> metadataBlocks, out int methodVersion) where T : EvalContextState, new() {
			langDebugInfo = evalInfo.Context.GetLanguageDebugInfo();
			method = langDebugInfo.MethodDebugInfo.Method;
			methodToken = langDebugInfo.MethodToken;
			localVarSigTok = langDebugInfo.LocalVarSigTok;

			state = GetEvalContextState<T>(evalInfo.Frame);

			if (state.LastModuleReferences == references && !state.LastMetadataBlocks.IsDefault)
				metadataBlocks = state.LastMetadataBlocks;
			else {
				metadataBlocks = CreateMetadataBlock(references);
				state.LastModuleReferences = references;
				state.LastMetadataBlocks = metadataBlocks;
			}

			methodVersion = langDebugInfo.MethodVersion;
		}

		protected void GetTypeCompilationState<T>(DbgEvaluationInfo evalInfo, DbgModuleReference[] references, out T state, out ImmutableArray<MetadataBlock> metadataBlocks) where T : EvalContextState, new() {
			state = GetEvalContextState<T>(evalInfo.Frame);

			if (state.LastModuleReferences == references && !state.LastMetadataBlocks.IsDefault)
				metadataBlocks = state.LastMetadataBlocks;
			else {
				metadataBlocks = CreateMetadataBlock(references);
				state.LastModuleReferences = references;
				state.LastMetadataBlocks = metadataBlocks;
			}
		}

		protected GetMethodDebugInfo CreateGetMethodDebugInfo(EvalContextState evalContextState, DbgLanguageDebugInfo langDebugInfo) {
			if (evalContextState.CompilerGeneratedVariableInfosKey != langDebugInfo.MethodDebugInfo.Method) {
				evalContextState.CompilerGeneratedVariableInfosKey = langDebugInfo.MethodDebugInfo.Method;
				evalContextState.CompilerGeneratedVariableInfos = null;
				evalContextState.NotCompilerGenerated = null;
			}

			// If there's only one scope, we can cache the whole thing
			if (langDebugInfo.MethodDebugInfo.Scope.Scopes.Length == 0) {
				if (evalContextState.MethodDebugInfoKey != langDebugInfo.MethodDebugInfo.Method) {
					evalContextState.MethodDebugInfo = CreateMethodDebugInfo(langDebugInfo, ref evalContextState.CompilerGeneratedVariableInfos, ref evalContextState.NotCompilerGenerated);
					evalContextState.MethodDebugInfoKey = langDebugInfo.MethodDebugInfo.Method;
				}
				return () => evalContextState.MethodDebugInfo;
			}
			evalContextState.MethodDebugInfo = default;
			evalContextState.MethodDebugInfoKey = null;
			return () => CreateMethodDebugInfo(langDebugInfo, ref evalContextState.CompilerGeneratedVariableInfos, ref evalContextState.NotCompilerGenerated);
		}

		(CompilerGeneratedVariableInfo[] infos, bool[] notCompilerGenerated) CreateCompilerGeneratedVariableInfos(List<DbgMethodDebugScope> allScopes, DbgMethodDebugInfo methodDebugInfo) {
			var locals = methodDebugInfo.Method.Body?.Variables;
			if (locals == null || locals.Count == 0)
				return (Array.Empty<CompilerGeneratedVariableInfo>(), Array.Empty<bool>());
			var notCompilerGenerated = new bool[locals.Count];
			foreach (var scope in allScopes) {
				foreach (var local in scope.Locals) {
					if (local.IsDecompilerGenerated)
						continue;
					if (local.Index < 0)
						continue;
					if (!local.IsDebuggerHidden)
						notCompilerGenerated[local.Index] = true;
				}
			}
			int count = 0;
			foreach (bool b in notCompilerGenerated) {
				if (!b)
					count++;
			}
			var res = count == 0 ? Array.Empty<CompilerGeneratedVariableInfo>() : new CompilerGeneratedVariableInfo[count];
			int w = 0;
			for (int i = 0; i < notCompilerGenerated.Length; i++) {
				if (notCompilerGenerated[i])
					continue;
				res[w++] = new CompilerGeneratedVariableInfo(i, "V_" + i.ToString());
			}
			if (w != res.Length)
				throw new InvalidOperationException();
			return (res, notCompilerGenerated);
		}

		DSEEMethodDebugInfo CreateMethodDebugInfo(DbgLanguageDebugInfo langDebugInfo, ref CompilerGeneratedVariableInfo[] compilerGeneratedVariableInfos, ref bool[] notCompilerGenerated) {
			var info = new DSEEMethodDebugInfo();
			var methodDebugInfo = langDebugInfo.MethodDebugInfo;

			var stack = new List<DbgMethodDebugScope>();
			var allScopes = new List<DbgMethodDebugScope>();
			var containingScopes = new List<DbgMethodDebugScope>();
			RoslynExpressionCompilerMethods.GetAllScopes(methodDebugInfo.Scope, stack, allScopes, containingScopes, langDebugInfo.ILOffset);

			if (compilerGeneratedVariableInfos == null)
				(compilerGeneratedVariableInfos, notCompilerGenerated) = CreateCompilerGeneratedVariableInfos(allScopes, methodDebugInfo);

			info.Compiler = GetCompiler(methodDebugInfo.Compiler);
			(info.HoistedLocalScopeRecords, info.HoistedVarFieldTokenToNamesMap) = GetHoistedVariablesInfo(allScopes, info.Compiler);
			info.ImportRecordGroups = GetImports(methodDebugInfo.Method.DeclaringType, methodDebugInfo.Scope, out var defaultNamespaceName);
			info.ExternAliasRecords = default;
			info.DynamicLocalMap = default;
			info.TupleLocalMap = default;
			info.DefaultNamespaceName = defaultNamespaceName ?? string.Empty;
			info.LocalVariableNames = RoslynExpressionCompilerMethods.GetLocalNames(methodDebugInfo.Method.Body?.Variables.Count ?? 0, containingScopes, compilerGeneratedVariableInfos);
			info.ParameterNames = GetParameterNames(langDebugInfo.MethodDebugInfo.Method, methodDebugInfo.Parameters);
			info.LocalConstants = default;
			info.ReuseSpan = RoslynExpressionCompilerMethods.GetReuseSpan(allScopes, langDebugInfo.ILOffset);

			return info;
		}

		static CompilerKind GetCompiler(DbgCompilerKind compiler) {
			switch (compiler) {
			case DbgCompilerKind.Unknown:				return CompilerKind.Unknown;
			case DbgCompilerKind.MicrosoftCSharp:		return CompilerKind.MicrosoftCSharp;
			case DbgCompilerKind.MicrosoftVisualBasic:	return CompilerKind.MicrosoftVisualBasic;
			case DbgCompilerKind.MonoCSharp:			return CompilerKind.MonoCSharp;
			default:
				Debug.Fail($"Unknown compiler: {compiler}");
				return CompilerKind.Unknown;
			}
		}

		(ImmutableArray<HoistedLocalScopeRecord> hoistedLocalScopeRecords, ImmutableDictionary<int, string> hoistedVarFieldTokenToNamesMap) GetHoistedVariablesInfo(List<DbgMethodDebugScope> scopes, CompilerKind compiler) {
			int maxSlotIndex = -1;
			foreach (var scope in scopes) {
				foreach (var local in scope.Locals) {
					if (local.HoistedField == null)
						continue;
					if (TryGetHoistedLocalSlotIndex(local.HoistedField, compiler, out var slotIndex))
						maxSlotIndex = Math.Max(maxSlotIndex, slotIndex);
				}
			}
			if (maxSlotIndex < 0)
				return default;
			int maxSlots = maxSlotIndex + 1;
			if (maxSlots >= 4 * 1024)
				return default;

			var scopeBuilder = ImmutableArray.CreateBuilder<HoistedLocalScopeRecord>(maxSlots);
			var namesBuilder = ImmutableDictionary.CreateBuilder<int, string>();
			scopeBuilder.Count = maxSlots;
			foreach (var scope in scopes) {
				foreach (var local in scope.Locals) {
					if (local.HoistedField == null)
						continue;
					if (TryGetHoistedLocalSlotIndex(local.HoistedField, compiler, out var slotIndex)) {
						Debug.Assert((uint)slotIndex < (uint)scopeBuilder.Count);
						scopeBuilder[slotIndex] = new HoistedLocalScopeRecord((int)scope.Span.Start, (int)scope.Span.Length);
						namesBuilder[local.HoistedField.MDToken.ToInt32()] = local.Name;
					}
				}
			}
			return (scopeBuilder.ToImmutable(), namesBuilder.ToImmutable());
		}

		static bool TryGetHoistedLocalSlotIndex(FieldDef field, CompilerKind compiler, out int slotIndex) {
			string name = field.Name;
			switch (compiler) {
			case CompilerKind.MicrosoftCSharp:
				return CSharp.GeneratedNamesHelpers.TryGetHoistedLocalSlotIndex(name, out slotIndex);
			case CompilerKind.MicrosoftVisualBasic:
				return VisualBasic.GeneratedNamesHelpers.TryGetHoistedLocalSlotIndex(name, out slotIndex);
			case CompilerKind.MonoCSharp:
				return CSharp.MonoGeneratedNamesHelpers.TryGetHoistedLocalSlotIndex(name, out slotIndex);
			case CompilerKind.Unknown:
				break;
			default:
				Debug.Fail($"Unknown compiler kind: {compiler}");
				break;
			}

			if (CSharp.GeneratedNamesHelpers.TryGetHoistedLocalSlotIndex(name, out slotIndex))
				return true;
			if (VisualBasic.GeneratedNamesHelpers.TryGetHoistedLocalSlotIndex(name, out slotIndex))
				return true;
			return CSharp.MonoGeneratedNamesHelpers.TryGetHoistedLocalSlotIndex(name, out slotIndex);
		}

		ImmutableArray<string> GetParameterNames(MethodDef method, DbgParameter[] parameters) {
			var ps = method.Parameters;
			if (method.MethodSig.Params.Count == 0)
				return default;

			bool valid = true;
			for (int i = 0; i < ps.Count; i++) {
				var p = ps[i];
				if (!p.IsNormalMethodParameter)
					continue;
				var name = TryGetSourceParameter(parameters, i).Name ?? p.Name;
				if (GetParameterName(i, name) != name) {
					valid = false;
					break;
				}
			}
			if (valid)
				return default;

			var builder = ImmutableArray.CreateBuilder<string>(method.Parameters.Count);
			for (int i = 0; i < ps.Count; i++) {
				var p = ps[i];
				if (p.IsNormalMethodParameter) {
					var name = TryGetSourceParameter(parameters, i).Name ?? p.Name;
					builder.Add(GetParameterName(i, name));
				}
				else
					builder.Add(null);
			}
			return builder.ToImmutable();
		}

		DbgParameter TryGetSourceParameter(DbgParameter[] parameters, int index) {
			foreach (var p in parameters) {
				if (p.Index == index)
					return p;
			}
			return default;
		}

		protected virtual string GetParameterName(int index, string name) => GetParameterNameCore(index, name);

		protected string GetParameterNameCore(int index, string name) {
			if (string.IsNullOrEmpty(name))
				return "A_" + index.ToString();
			return IdentifierEscaper.Escape(name);
		}

		protected abstract ImmutableArray<ImmutableArray<DSEEImportRecord>> GetImports(TypeDef declaringType, DbgMethodDebugScope scope, out string defaultNamespaceName);

		protected static void AddDSEEImportRecord(ImmutableArray<DSEEImportRecord>.Builder builder, DbgImportInfo info, ref string defaultNamespaceName) {
			switch (info.TargetKind) {
			case DbgImportInfoKind.Namespace:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.Namespace, info.Alias, info.Target, info.ExternAlias));
				break;

			case DbgImportInfoKind.Type:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.Type, info.Alias, info.Target, info.ExternAlias));
				break;

			case DbgImportInfoKind.NamespaceOrType:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.NamespaceOrType, info.Alias, info.Target, info.ExternAlias));
				break;

			case DbgImportInfoKind.Assembly:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.Assembly, info.Alias, info.Target, info.ExternAlias));
				break;

			case DbgImportInfoKind.XmlNamespace:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.XmlNamespace, info.Alias, info.Target, info.ExternAlias));
				break;

			case DbgImportInfoKind.MethodToken:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.MethodToken, info.Alias, info.Target, info.ExternAlias));
				break;

			case DbgImportInfoKind.CurrentNamespace:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.CurrentNamespace, info.Alias, info.Target, info.ExternAlias));
				break;

			case DbgImportInfoKind.DefaultNamespace:
				defaultNamespaceName = info.Target ?? string.Empty;
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.DefaultNamespace, info.Alias, info.Target, info.ExternAlias));
				break;

			default:
				Debug.Fail($"Unknown import kind: {info.TargetKind}");
				break;
			}
		}

		protected ImmutableArray<Alias> CreateAliases(DbgDotNetAlias[] aliases) {
			if (aliases.Length == 0)
				return ImmutableArray<Alias>.Empty;

			var builder = ImmutableArray.CreateBuilder<Alias>(aliases.Length);
			foreach (var alias in aliases) {
				DkmClrAliasKind aliasKind;
				switch (alias.Kind) {
				case DbgDotNetAliasKind.Exception:
					aliasKind = DkmClrAliasKind.Exception;
					break;

				case DbgDotNetAliasKind.StowedException:
					aliasKind = DkmClrAliasKind.StowedException;
					break;

				case DbgDotNetAliasKind.ReturnValue:
					aliasKind = DkmClrAliasKind.ReturnValue;
					break;

				case DbgDotNetAliasKind.Variable:
					aliasKind = DkmClrAliasKind.Variable;
					break;

				case DbgDotNetAliasKind.ObjectId:
					aliasKind = DkmClrAliasKind.ObjectId;
					break;

				default:
					throw new InvalidOperationException();
				}
				builder.Add(new Alias(aliasKind, alias.Name, alias.Name, alias.Type, alias.CustomTypeInfoId, alias.CustomTypeInfo));
			}
			return builder.ToImmutableArray();
		}

		protected DbgDotNetCompilationResult CreateCompilationResult(string expression, CompileResult compileResult, ResultProperties resultProperties, string errorMessage, DbgDotNetText name) {
			if (errorMessage != null)
				return new DbgDotNetCompilationResult(errorMessage);
			Debug.Assert(compileResult != null);
			if (compileResult == null)
				return new DbgDotNetCompilationResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			var customTypeInfoGuid = compileResult.GetCustomTypeInfo(out var payload);
			DbgDotNetCustomTypeInfo customTypeInfo;
			if (payload != null)
				customTypeInfo = new DbgDotNetCustomTypeInfo(customTypeInfoGuid, payload);
			else
				customTypeInfo = null;

			var compExprs = new[] {
				DbgDotNetCompiledExpressionResult.Create(compileResult.TypeName, compileResult.MethodName,
						expression, name, ToEvaluationResultFlags(resultProperties.Flags),
						resultProperties.GetImageName(), customTypeInfo, compileResult.FormatSpecifiers),
			};
			return new DbgDotNetCompilationResult(compileResult.Assembly, compExprs);
		}

		static DbgEvaluationResultFlags ToEvaluationResultFlags(DkmClrCompilationResultFlags flags) {
			var res = DbgEvaluationResultFlags.None;
			if ((flags & DkmClrCompilationResultFlags.PotentialSideEffect) != 0)
				res |= DbgEvaluationResultFlags.SideEffects;
			if ((flags & DkmClrCompilationResultFlags.ReadOnlyResult) != 0)
				res |= DbgEvaluationResultFlags.ReadOnly;
			if ((flags & DkmClrCompilationResultFlags.BoolResult) != 0)
				res |= DbgEvaluationResultFlags.BooleanExpression;
			return res;
		}

		protected DbgDotNetCompilationResult CreateCompilationResult(EvalContextState state, byte[] assembly, string typeName, DSEELocalAndMethod[] infos, string errorMessage) {
			Debug.Assert(errorMessage == null || (assembly == null || assembly.Length == 0));

			if (errorMessage != null)
				return new DbgDotNetCompilationResult(errorMessage);
			if (assembly == null || assembly.Length == 0)
				return new DbgDotNetCompilationResult(Array.Empty<byte>(), Array.Empty<DbgDotNetCompiledExpressionResult>());

			var compiledExpressions = new DbgDotNetCompiledExpressionResult[infos.Length];
			int w = 0;
			for (int i = 0; i < infos.Length; i++) {
				var info = infos[i];

				string imageName;
				DbgDotNetText displayName;
				DbgTextColor nameColor;
				switch (info.Kind) {
				case LocalAndMethodKind.Local:
					imageName = PredefinedDbgValueNodeImageNames.Local;
					nameColor = DbgTextColor.Local;
					break;

				case LocalAndMethodKind.Parameter:
					imageName = PredefinedDbgValueNodeImageNames.Parameter;
					nameColor = DbgTextColor.Parameter;
					break;

				case LocalAndMethodKind.This:
					imageName = PredefinedDbgValueNodeImageNames.This;
					nameColor = DbgTextColor.Keyword;
					break;

				case LocalAndMethodKind.LocalConstant:
					imageName = PredefinedDbgValueNodeImageNames.Constant;
					nameColor = DbgTextColor.Local;
					break;

				case LocalAndMethodKind.StaticLocal:
					imageName = PredefinedDbgValueNodeImageNames.Local;
					nameColor = DbgTextColor.Local;
					break;

				case LocalAndMethodKind.ObjectAddress:
					imageName = PredefinedDbgValueNodeImageNames.ObjectAddress;
					nameColor = DbgTextColor.Number;
					break;

				case LocalAndMethodKind.TypeVariables:
					Debug.Fail("Roslyn's EC shouldn't create type variables");
					continue;

				case LocalAndMethodKind.Exception:
				case LocalAndMethodKind.StowedException:
				case LocalAndMethodKind.ReturnValue:
				case LocalAndMethodKind.ObjectId:
				case LocalAndMethodKind.EEVariable:
					Debug.Fail("Should not happen since we didn't pass in any aliases");
					continue;

				default:
					Debug.Fail($"Unknown {nameof(LocalAndMethodKind)} value: {info.Kind}");
					imageName = PredefinedDbgValueNodeImageNames.Data;
					nameColor = DbgTextColor.Text;
					break;
				}
				displayName = new DbgDotNetText(new DbgDotNetTextPart(nameColor, info.LocalDisplayName));

				var flags = DbgEvaluationResultFlags.None;
				if ((info.Flags & DkmClrCompilationResultFlags.PotentialSideEffect) != 0)
					flags |= DbgEvaluationResultFlags.SideEffects;
				if ((info.Flags & DkmClrCompilationResultFlags.ReadOnlyResult) != 0)
					flags |= DbgEvaluationResultFlags.ReadOnly;
				if ((info.Flags & DkmClrCompilationResultFlags.BoolResult) != 0)
					flags |= DbgEvaluationResultFlags.BooleanExpression;

				DbgDotNetCustomTypeInfo customTypeInfo;
				if (info.CustomTypeInfo != null)
					customTypeInfo = new DbgDotNetCustomTypeInfo(info.CustomTypeInfoId, info.CustomTypeInfo);
				else
					customTypeInfo = null;

				var resultFlags = DbgDotNetCompiledExpressionResultFlags.None;
				if (info.Kind == LocalAndMethodKind.Local && (uint)info.Index < (uint)state.NotCompilerGenerated.Length && !state.NotCompilerGenerated[info.Index])
					resultFlags |= DbgDotNetCompiledExpressionResultFlags.CompilerGenerated;
				compiledExpressions[w++] = DbgDotNetCompiledExpressionResult.Create(typeName, info.MethodName, info.LocalName, displayName, flags, imageName, customTypeInfo, null, resultFlags, info.Index);
			}
			if (compiledExpressions.Length != w)
				Array.Resize(ref compiledExpressions, w);
			return new DbgDotNetCompilationResult(assembly, compiledExpressions);
		}

		static ImmutableArray<MetadataBlock> CreateMetadataBlock(DbgModuleReference[] references) {
			var builder = ImmutableArray.CreateBuilder<MetadataBlock>(references.Length);
			for (int i = 0; i < references.Length; i++) {
				var r = references[i];
				builder.Add(new MetadataBlock(r.ModuleVersionId, r.GenerationId, r.MetadataAddress, (int)r.MetadataSize));
			}
			return builder.ToImmutable();
		}

		protected static DbgDotNetText CreateErrorName(string expression) => new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Error, expression));

		protected DbgDotNetText GetExpressionText(string languageName, CompilationOptions compilationOptions, ParseOptions parseOptions, string expression, string documentText, int documentTextExpressionOffset, IEnumerable<MetadataReference> metadataReferences, CancellationToken cancellationToken) {
			compilationOptions = compilationOptions.WithMetadataImportOptions(MetadataImportOptions.All);
			using (var workspace = new AdhocWorkspace(RoslynMefHostServices.DefaultServices)) {
				var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "P", Guid.NewGuid().ToString(), languageName,
					compilationOptions: compilationOptions,
					parseOptions: parseOptions,
					metadataReferences: metadataReferences);
				workspace.AddProject(projectInfo);

				var doc = workspace.AddDocument(projectInfo.Id, "A", SourceText.From(documentText));
				var syntaxRoot = doc.GetSyntaxRootAsync().GetAwaiter().GetResult();
				var semanticModel = doc.GetSemanticModelAsync().GetAwaiter().GetResult();
				var classifier = new RoslynClassifier(syntaxRoot, semanticModel, workspace, RoslynClassificationTypes.Default, null, cancellationToken);
				var textSpan = new Microsoft.CodeAnalysis.Text.TextSpan(documentTextExpressionOffset, expression.Length);

				int pos = textSpan.Start;
				var output = ObjectCache.AllocDotNetTextOutput();
				foreach (var info in classifier.GetColors(textSpan)) {
					if (pos < info.Span.Start)
						output.Write(DbgTextColor.Text, expression.Substring(pos - textSpan.Start, info.Span.Start - pos));
					output.Write(ColorConverter.ToDebuggerColor(info.Color), expression.Substring(info.Span.Start - textSpan.Start, info.Span.Length));
					pos = info.Span.End;
				}
				if (pos < textSpan.End)
					output.Write(DbgTextColor.Text, expression.Substring(pos - textSpan.Start, textSpan.End - pos));
				return ObjectCache.FreeAndToText(ref output);
			}
		}

		protected DbgDotNetCompilationResult CompileGetLocals(EvalContextState state, MethodDef method) {
			var builder = new GetLocalsAssemblyBuilder(this, method, state.MethodDebugInfo.LocalVariableNames, state.MethodDebugInfo.ParameterNames);
			var asmBytes = builder.Compile(out var localsInfo, out var typeName, out var errorMessage);
			return CreateCompilationResult(state, asmBytes, typeName, localsInfo, errorMessage);
		}

		internal abstract string GetVariableName(string metadataName, bool isThis);

		protected abstract bool IsCaseSensitive { get; }
		public override bool TryGetAliasInfo(string aliasName, out DbgDotNetParsedAlias aliasInfo) =>
			AliasConstants.TryGetAliasInfo(aliasName, IsCaseSensitive, out aliasInfo);

		protected DbgDotNetText CreateText(DbgDotNetAliasKind kind, string expression) {
			DbgTextColor color;
			switch (kind) {
			case DbgDotNetAliasKind.Exception:		color = DbgTextColor.ExceptionName; break;
			case DbgDotNetAliasKind.StowedException:color = DbgTextColor.StowedExceptionName; break;
			case DbgDotNetAliasKind.ReturnValue:	color = DbgTextColor.ReturnValueName; break;
			case DbgDotNetAliasKind.Variable:		color = DbgTextColor.VariableName; break;
			case DbgDotNetAliasKind.ObjectId:		color = DbgTextColor.ObjectIdName; break;
			default:								throw new InvalidOperationException();
			}
			return new DbgDotNetText(new DbgDotNetTextPart(color, expression));
		}
	}
}
