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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings;
using ICSharpCode.Decompiler;

namespace dnSpy.Decompiler.ILSpy.Settings {
	[Export]
	sealed class DecompilerSettingsImpl : DecompilerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("6745457F-254B-4B7B-90F1-F948F0721C3B");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		DecompilerSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			// Only read those settings that can be changed in the dialog box
			DecompilationObject0 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject0)) ?? DecompilationObject0;
			DecompilationObject1 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject1)) ?? DecompilationObject1;
			DecompilationObject2 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject2)) ?? DecompilationObject2;
			DecompilationObject3 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject3)) ?? DecompilationObject3;
			DecompilationObject4 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject4)) ?? DecompilationObject4;
			AnonymousMethods = sect.Attribute<bool?>(nameof(AnonymousMethods)) ?? AnonymousMethods;
			ExpressionTrees = sect.Attribute<bool?>(nameof(ExpressionTrees)) ?? ExpressionTrees;
			YieldReturn = sect.Attribute<bool?>(nameof(YieldReturn)) ?? YieldReturn;
			AsyncAwait = sect.Attribute<bool?>(nameof(AsyncAwait)) ?? AsyncAwait;
			//AutomaticProperties = sect.Attribute<bool?>(nameof(AutomaticProperties)) ?? AutomaticProperties;
			//AutomaticEvents = sect.Attribute<bool?>(nameof(AutomaticEvents)) ?? AutomaticEvents;
			//UsingStatement = sect.Attribute<bool?>(nameof(UsingStatement)) ?? UsingStatement;
			//ForEachStatement = sect.Attribute<bool?>(nameof(ForEachStatement)) ?? ForEachStatement;
			//LockStatement = sect.Attribute<bool?>(nameof(LockStatement)) ?? LockStatement;
			//SwitchStatementOnString = sect.Attribute<bool?>(nameof(SwitchStatementOnString)) ?? SwitchStatementOnString;
			//UsingDeclarations = sect.Attribute<bool?>(nameof(UsingDeclarations)) ?? UsingDeclarations;
			QueryExpressions = sect.Attribute<bool?>(nameof(QueryExpressions)) ?? QueryExpressions;
			FullyQualifyAmbiguousTypeNames = sect.Attribute<bool?>(nameof(FullyQualifyAmbiguousTypeNames)) ?? FullyQualifyAmbiguousTypeNames;
			FullyQualifyAllTypes = sect.Attribute<bool?>(nameof(FullyQualifyAllTypes)) ?? FullyQualifyAllTypes;
			UseDebugSymbols = sect.Attribute<bool?>(nameof(UseDebugSymbols)) ?? UseDebugSymbols;
			//ObjectOrCollectionInitializers = sect.Attribute<bool?>(nameof(ObjectOrCollectionInitializers)) ?? ObjectOrCollectionInitializers;
			ShowXmlDocumentation = sect.Attribute<bool?>(nameof(ShowXmlDocumentation)) ?? ShowXmlDocumentation;
			RemoveEmptyDefaultConstructors = sect.Attribute<bool?>(nameof(RemoveEmptyDefaultConstructors)) ?? RemoveEmptyDefaultConstructors;
			//IntroduceIncrementAndDecrement = sect.Attribute<bool?>(nameof(IntroduceIncrementAndDecrement)) ?? IntroduceIncrementAndDecrement;
			//MakeAssignmentExpressions = sect.Attribute<bool?>(nameof(MakeAssignmentExpressions)) ?? MakeAssignmentExpressions;
			//AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject = sect.Attribute<bool?>(nameof(AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject)) ?? AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject;
			ShowTokenAndRvaComments = sect.Attribute<bool?>(nameof(ShowTokenAndRvaComments)) ?? ShowTokenAndRvaComments;
			SortMembers = sect.Attribute<bool?>(nameof(SortMembers)) ?? SortMembers;
			ForceShowAllMembers = sect.Attribute<bool?>(nameof(ForceShowAllMembers)) ?? ForceShowAllMembers;
			SortSystemUsingStatementsFirst = sect.Attribute<bool?>(nameof(SortSystemUsingStatementsFirst)) ?? SortSystemUsingStatementsFirst;
			//MaxArrayElements = sect.Attribute<int?>(nameof(MaxArrayElements)) ?? MaxArrayElements;
			SortCustomAttributes = sect.Attribute<bool?>(nameof(SortCustomAttributes)) ?? SortCustomAttributes;
			UseSourceCodeOrder = sect.Attribute<bool?>(nameof(UseSourceCodeOrder)) ?? UseSourceCodeOrder;
			AllowFieldInitializers = sect.Attribute<bool?>(nameof(AllowFieldInitializers)) ?? AllowFieldInitializers;
			OneCustomAttributePerLine = sect.Attribute<bool?>(nameof(OneCustomAttributePerLine)) ?? OneCustomAttributePerLine;
			TypeAddInternalModifier = sect.Attribute<bool?>(nameof(TypeAddInternalModifier)) ?? TypeAddInternalModifier;
			MemberAddPrivateModifier = sect.Attribute<bool?>(nameof(MemberAddPrivateModifier)) ?? MemberAddPrivateModifier;
			//RemoveNewDelegateClass = sect.Attribute<bool?>(nameof(RemoveNewDelegateClass)) ?? RemoveNewDelegateClass;
			//TODO: CSharpFormattingOptions
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;

			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			// Only save those settings that can be changed in the dialog box
			sect.Attribute(nameof(DecompilationObject0), DecompilationObject0);
			sect.Attribute(nameof(DecompilationObject1), DecompilationObject1);
			sect.Attribute(nameof(DecompilationObject2), DecompilationObject2);
			sect.Attribute(nameof(DecompilationObject3), DecompilationObject3);
			sect.Attribute(nameof(DecompilationObject4), DecompilationObject4);
			sect.Attribute(nameof(AnonymousMethods), AnonymousMethods);
			sect.Attribute(nameof(ExpressionTrees), ExpressionTrees);
			sect.Attribute(nameof(YieldReturn), YieldReturn);
			sect.Attribute(nameof(AsyncAwait), AsyncAwait);
			//sect.Attribute(nameof(AutomaticProperties), AutomaticProperties);
			//sect.Attribute(nameof(AutomaticEvents), AutomaticEvents);
			//sect.Attribute(nameof(UsingStatement), UsingStatement);
			//sect.Attribute(nameof(ForEachStatement), ForEachStatement);
			//sect.Attribute(nameof(LockStatement), LockStatement);
			//sect.Attribute(nameof(SwitchStatementOnString), SwitchStatementOnString);
			//sect.Attribute(nameof(UsingDeclarations), UsingDeclarations);
			sect.Attribute(nameof(QueryExpressions), QueryExpressions);
			sect.Attribute(nameof(FullyQualifyAmbiguousTypeNames), FullyQualifyAmbiguousTypeNames);
			sect.Attribute(nameof(FullyQualifyAllTypes), FullyQualifyAllTypes);
			sect.Attribute(nameof(UseDebugSymbols), UseDebugSymbols);
			//sect.Attribute(nameof(ObjectOrCollectionInitializers), ObjectOrCollectionInitializers);
			sect.Attribute(nameof(ShowXmlDocumentation), ShowXmlDocumentation);
			sect.Attribute(nameof(RemoveEmptyDefaultConstructors), RemoveEmptyDefaultConstructors);
			//sect.Attribute(nameof(IntroduceIncrementAndDecrement), IntroduceIncrementAndDecrement);
			//sect.Attribute(nameof(MakeAssignmentExpressions), MakeAssignmentExpressions);
			//sect.Attribute(nameof(AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject), AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject);
			sect.Attribute(nameof(ShowTokenAndRvaComments), ShowTokenAndRvaComments);
			sect.Attribute(nameof(SortMembers), SortMembers);
			sect.Attribute(nameof(ForceShowAllMembers), ForceShowAllMembers);
			sect.Attribute(nameof(SortSystemUsingStatementsFirst), SortSystemUsingStatementsFirst);
			//sect.Attribute(nameof(MaxArrayElements), MaxArrayElements);
			sect.Attribute(nameof(SortCustomAttributes), SortCustomAttributes);
			sect.Attribute(nameof(UseSourceCodeOrder), UseSourceCodeOrder);
			sect.Attribute(nameof(AllowFieldInitializers), AllowFieldInitializers);
			sect.Attribute(nameof(OneCustomAttributePerLine), OneCustomAttributePerLine);
			sect.Attribute(nameof(TypeAddInternalModifier), TypeAddInternalModifier);
			sect.Attribute(nameof(MemberAddPrivateModifier), MemberAddPrivateModifier);
			//sect.Attribute(nameof(RemoveNewDelegateClass), RemoveNewDelegateClass);
			//TODO: CSharpFormattingOptions
		}
	}
}
