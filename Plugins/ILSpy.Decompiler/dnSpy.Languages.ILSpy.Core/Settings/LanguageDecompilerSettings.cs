/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Languages;
using dnSpy.Languages.ILSpy.Core.Properties;
using dnSpy.Languages.Settings;
using ICSharpCode.Decompiler;

namespace dnSpy.Languages.ILSpy.Settings {
	sealed class LanguageDecompilerSettings : DecompilerSettingsBase {
		public DecompilerSettings Settings => decompilerSettings;
		readonly DecompilerSettings decompilerSettings;

		public LanguageDecompilerSettings(DecompilerSettings decompilerSettings = null) {
			this.decompilerSettings = decompilerSettings ?? new DecompilerSettings();
			this.options = CreateOptions().ToArray();
		}

		public override DecompilerSettingsBase Clone() => new LanguageDecompilerSettings(this.decompilerSettings.Clone());

		public override IEnumerable<IDecompilerOption> Options => options;
		readonly IDecompilerOption[] options;

		IEnumerable<IDecompilerOption> CreateOptions() {
			yield return new DecompilerOption<string>(DecompilerOptionConstants.MemberOrder_GUID,
						() => GetMemberOrder(), a => SetMemberOrder(a)) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompilationOrder,
				Name = DecompilerOptionConstants.MemberOrder_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.AnonymousMethods_GUID,
						() => decompilerSettings.AnonymousMethods, a => decompilerSettings.AnonymousMethods = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileAnonMethods,
				Name = DecompilerOptionConstants.AnonymousMethods_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.ExpressionTrees_GUID,
						() => decompilerSettings.ExpressionTrees, a => decompilerSettings.ExpressionTrees = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileExprTrees,
				Name = DecompilerOptionConstants.ExpressionTrees_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.YieldReturn_GUID,
						() => decompilerSettings.YieldReturn, a => decompilerSettings.YieldReturn = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileEnumerators,
				Name = DecompilerOptionConstants.YieldReturn_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.AsyncAwait_GUID,
						() => decompilerSettings.AsyncAwait, a => decompilerSettings.AsyncAwait = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileAsyncMethods,
				Name = DecompilerOptionConstants.AsyncAwait_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.AutomaticProperties_GUID,
						() => decompilerSettings.AutomaticProperties, a => decompilerSettings.AutomaticProperties = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileAutoProps,
				Name = DecompilerOptionConstants.AutomaticProperties_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.AutomaticEvents_GUID,
						() => decompilerSettings.AutomaticEvents, a => decompilerSettings.AutomaticEvents = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileAutoEvents,
				Name = DecompilerOptionConstants.AutomaticEvents_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.UsingStatement_GUID,
						() => decompilerSettings.UsingStatement, a => decompilerSettings.UsingStatement = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileUsingStatements,
				Name = DecompilerOptionConstants.UsingStatement_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.ForEachStatement_GUID,
						() => decompilerSettings.ForEachStatement, a => decompilerSettings.ForEachStatement = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileForeachStatements,
				Name = DecompilerOptionConstants.ForEachStatement_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.LockStatement_GUID,
						() => decompilerSettings.LockStatement, a => decompilerSettings.LockStatement = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileLockStatements,
				Name = DecompilerOptionConstants.LockStatement_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.SwitchStatementOnString_GUID,
						() => decompilerSettings.SwitchStatementOnString, a => decompilerSettings.SwitchStatementOnString = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileSwitchOnString,
				Name = DecompilerOptionConstants.SwitchStatementOnString_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.UsingDeclarations_GUID,
						() => decompilerSettings.UsingDeclarations, a => decompilerSettings.UsingDeclarations = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_AddUsingDeclarations,
				Name = DecompilerOptionConstants.UsingDeclarations_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.QueryExpressions_GUID,
						() => decompilerSettings.QueryExpressions, a => decompilerSettings.QueryExpressions = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_DecompileQueryExpr,
				Name = DecompilerOptionConstants.QueryExpressions_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.FullyQualifyAmbiguousTypeNames_GUID,
						() => decompilerSettings.FullyQualifyAmbiguousTypeNames, a => decompilerSettings.FullyQualifyAmbiguousTypeNames = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_FullyQualifyAmbiguousTypeNames,
				Name = DecompilerOptionConstants.FullyQualifyAmbiguousTypeNames_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.FullyQualifyAllTypes_GUID,
						() => decompilerSettings.FullyQualifyAllTypes, a => decompilerSettings.FullyQualifyAllTypes = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_FullyQualifyAllTypes,
				Name = DecompilerOptionConstants.FullyQualifyAllTypes_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.UseDebugSymbols_GUID,
						() => decompilerSettings.UseDebugSymbols, a => decompilerSettings.UseDebugSymbols = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_UseLocalNameFromSyms,
				Name = DecompilerOptionConstants.UseDebugSymbols_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.ObjectOrCollectionInitializers_GUID,
						() => decompilerSettings.ObjectOrCollectionInitializers, a => decompilerSettings.ObjectOrCollectionInitializers = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_ObjectOrCollectionInitializers,
				Name = DecompilerOptionConstants.ObjectOrCollectionInitializers_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.ShowXmlDocumentation_GUID,
						() => decompilerSettings.ShowXmlDocumentation, a => decompilerSettings.ShowXmlDocumentation = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_ShowXMLDocComments,
				Name = DecompilerOptionConstants.ShowXmlDocumentation_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.RemoveEmptyDefaultConstructors_GUID,
						() => decompilerSettings.RemoveEmptyDefaultConstructors, a => decompilerSettings.RemoveEmptyDefaultConstructors = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_RemoveEmptyDefaultCtors,
				Name = DecompilerOptionConstants.RemoveEmptyDefaultConstructors_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.IntroduceIncrementAndDecrement_GUID,
						() => decompilerSettings.IntroduceIncrementAndDecrement, a => decompilerSettings.IntroduceIncrementAndDecrement = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_IntroduceIncrementAndDecrement,
				Name = DecompilerOptionConstants.IntroduceIncrementAndDecrement_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.MakeAssignmentExpressions_GUID,
						() => decompilerSettings.MakeAssignmentExpressions, a => decompilerSettings.MakeAssignmentExpressions = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_MakeAssignmentExpressions,
				Name = DecompilerOptionConstants.MakeAssignmentExpressions_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject_GUID,
						() => decompilerSettings.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject, a => decompilerSettings.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject,
				Name = DecompilerOptionConstants.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID,
						() => decompilerSettings.ShowTokenAndRvaComments, a => decompilerSettings.ShowTokenAndRvaComments = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_ShowTokensRvasOffsets,
				Name = DecompilerOptionConstants.ShowTokenAndRvaComments_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.SortMembers_GUID,
						() => decompilerSettings.SortMembers, a => decompilerSettings.SortMembers = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_SortMethods,
				Name = DecompilerOptionConstants.SortMembers_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.ForceShowAllMembers_GUID,
						() => decompilerSettings.ForceShowAllMembers, a => decompilerSettings.ForceShowAllMembers = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_ShowCompilerGeneratedTypes,
				Name = DecompilerOptionConstants.ForceShowAllMembers_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.SortSystemUsingStatementsFirst_GUID,
						() => decompilerSettings.SortSystemUsingStatementsFirst, a => decompilerSettings.SortSystemUsingStatementsFirst = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_SortSystemFirst,
				Name = DecompilerOptionConstants.SortSystemUsingStatementsFirst_NAME,
			};
			yield return new DecompilerOption<int>(DecompilerOptionConstants.MaxArrayElements_GUID,
						() => decompilerSettings.MaxArrayElements, a => decompilerSettings.MaxArrayElements = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_MaxArrayElements,
				Name = DecompilerOptionConstants.MaxArrayElements_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.SortCustomAttributes_GUID,
						() => decompilerSettings.SortCustomAttributes, a => decompilerSettings.SortCustomAttributes = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_SortCustomAttributes,
				Name = DecompilerOptionConstants.SortCustomAttributes_NAME,
			};
			yield return new DecompilerOption<bool>(DecompilerOptionConstants.UseSourceCodeOrder_GUID,
						() => decompilerSettings.UseSourceCodeOrder, a => decompilerSettings.UseSourceCodeOrder = a) {
				Description = dnSpy_Languages_ILSpy_Core.DecompilerSettings_UseSourceCodeOrder,
				Name = DecompilerOptionConstants.UseSourceCodeOrder_NAME,
			};
		}

		string GetMemberOrder() =>
			GetMemberOrderString(decompilerSettings.DecompilationObject0) +
			GetMemberOrderString(decompilerSettings.DecompilationObject1) +
			GetMemberOrderString(decompilerSettings.DecompilationObject2) +
			GetMemberOrderString(decompilerSettings.DecompilationObject3) +
			GetMemberOrderString(decompilerSettings.DecompilationObject4);

		static string GetMemberOrderString(DecompilationObject d) {
			switch (d) {
			case DecompilationObject.NestedTypes:	return "t";
			case DecompilationObject.Fields:		return "f";
			case DecompilationObject.Events:		return "e";
			case DecompilationObject.Properties:	return "p";
			case DecompilationObject.Methods:		return "m";
			default:
				Debug.Fail("Shouldn't be here");
				return "?";
			}
		}

		void SetMemberOrder(string s) {
			if (s == null || s.Length != 5)
				return;
			decompilerSettings.DecompilationObject0 = GetDecompilationObject(s[0]) ?? decompilerSettings.DecompilationObject0;
			decompilerSettings.DecompilationObject1 = GetDecompilationObject(s[1]) ?? decompilerSettings.DecompilationObject1;
			decompilerSettings.DecompilationObject2 = GetDecompilationObject(s[2]) ?? decompilerSettings.DecompilationObject2;
			decompilerSettings.DecompilationObject3 = GetDecompilationObject(s[3]) ?? decompilerSettings.DecompilationObject3;
			decompilerSettings.DecompilationObject4 = GetDecompilationObject(s[4]) ?? decompilerSettings.DecompilationObject4;
		}

		static DecompilationObject? GetDecompilationObject(char c) {
			switch (c) {
			case 't': return DecompilationObject.NestedTypes;
			case 'f': return DecompilationObject.Fields;
			case 'e': return DecompilationObject.Events;
			case 'p': return DecompilationObject.Properties;
			case 'm': return DecompilationObject.Methods;
			}
			return null;
		}

		protected override bool EqualsCore(object obj) {
			var other = obj as LanguageDecompilerSettings;
			return other != null && decompilerSettings.Equals(other.decompilerSettings);
		}

		protected override int GetHashCodeCore() => decompilerSettings.GetHashCode();
	}
}
