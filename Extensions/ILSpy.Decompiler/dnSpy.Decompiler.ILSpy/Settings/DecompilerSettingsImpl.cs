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

using System;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings;
using ICSharpCode.Decompiler;

namespace dnSpy.Decompiler.ILSpy.Settings {
	[Export]
	sealed class DecompilerSettingsImpl : DecompilerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("6745457F-254B-4B7B-90F1-F948F0721C3B");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		DecompilerSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			// Only read those settings that can be changed in the dialog box
			this.DecompilationObject0 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject0)) ?? this.DecompilationObject0;
			this.DecompilationObject1 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject1)) ?? this.DecompilationObject1;
			this.DecompilationObject2 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject2)) ?? this.DecompilationObject2;
			this.DecompilationObject3 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject3)) ?? this.DecompilationObject3;
			this.DecompilationObject4 = sect.Attribute<DecompilationObject?>(nameof(DecompilationObject4)) ?? this.DecompilationObject4;
			this.AnonymousMethods = sect.Attribute<bool?>(nameof(AnonymousMethods)) ?? this.AnonymousMethods;
			this.ExpressionTrees = sect.Attribute<bool?>(nameof(ExpressionTrees)) ?? this.ExpressionTrees;
			this.YieldReturn = sect.Attribute<bool?>(nameof(YieldReturn)) ?? this.YieldReturn;
			this.AsyncAwait = sect.Attribute<bool?>(nameof(AsyncAwait)) ?? this.AsyncAwait;
			//this.AutomaticProperties = sect.Attribute<bool?>(nameof(AutomaticProperties)) ?? this.AutomaticProperties;
			//this.AutomaticEvents = sect.Attribute<bool?>(nameof(AutomaticEvents)) ?? this.AutomaticEvents;
			//this.UsingStatement = sect.Attribute<bool?>(nameof(UsingStatement)) ?? this.UsingStatement;
			//this.ForEachStatement = sect.Attribute<bool?>(nameof(ForEachStatement)) ?? this.ForEachStatement;
			//this.LockStatement = sect.Attribute<bool?>(nameof(LockStatement)) ?? this.LockStatement;
			//this.SwitchStatementOnString = sect.Attribute<bool?>(nameof(SwitchStatementOnString)) ?? this.SwitchStatementOnString;
			//this.UsingDeclarations = sect.Attribute<bool?>(nameof(UsingDeclarations)) ?? this.UsingDeclarations;
			this.QueryExpressions = sect.Attribute<bool?>(nameof(QueryExpressions)) ?? this.QueryExpressions;
			//this.FullyQualifyAmbiguousTypeNames = sect.Attribute<bool?>(nameof(FullyQualifyAmbiguousTypeNames)) ?? this.FullyQualifyAmbiguousTypeNames;
			//this.FullyQualifyAllTypes = sect.Attribute<bool?>(nameof(FullyQualifyAllTypes)) ?? this.FullyQualifyAllTypes;
			this.UseDebugSymbols = sect.Attribute<bool?>(nameof(UseDebugSymbols)) ?? this.UseDebugSymbols;
			//this.ObjectOrCollectionInitializers = sect.Attribute<bool?>(nameof(ObjectOrCollectionInitializers)) ?? this.ObjectOrCollectionInitializers;
			this.ShowXmlDocumentation = sect.Attribute<bool?>(nameof(ShowXmlDocumentation)) ?? this.ShowXmlDocumentation;
			this.RemoveEmptyDefaultConstructors = sect.Attribute<bool?>(nameof(RemoveEmptyDefaultConstructors)) ?? this.RemoveEmptyDefaultConstructors;
			//this.IntroduceIncrementAndDecrement = sect.Attribute<bool?>(nameof(IntroduceIncrementAndDecrement)) ?? this.IntroduceIncrementAndDecrement;
			//this.MakeAssignmentExpressions = sect.Attribute<bool?>(nameof(MakeAssignmentExpressions)) ?? this.MakeAssignmentExpressions;
			//this.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject = sect.Attribute<bool?>(nameof(AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject)) ?? this.AlwaysGenerateExceptionVariableForCatchBlocksUnlessTypeIsObject;
			this.ShowTokenAndRvaComments = sect.Attribute<bool?>(nameof(ShowTokenAndRvaComments)) ?? this.ShowTokenAndRvaComments;
			this.SortMembers = sect.Attribute<bool?>(nameof(SortMembers)) ?? this.SortMembers;
			this.ForceShowAllMembers = sect.Attribute<bool?>(nameof(ForceShowAllMembers)) ?? this.ForceShowAllMembers;
			this.SortSystemUsingStatementsFirst = sect.Attribute<bool?>(nameof(SortSystemUsingStatementsFirst)) ?? this.SortSystemUsingStatementsFirst;
			//this.MaxArrayElements = sect.Attribute<int?>(nameof(MaxArrayElements)) ?? this.MaxArrayElements;
			this.SortCustomAttributes = sect.Attribute<bool?>(nameof(SortCustomAttributes)) ?? this.SortCustomAttributes;
			this.UseSourceCodeOrder = sect.Attribute<bool?>(nameof(UseSourceCodeOrder)) ?? this.UseSourceCodeOrder;
			//this.AllowFieldInitializers = sect.Attribute<bool?>(nameof(AllowFieldInitializers)) ?? this.AllowFieldInitializers;
			//TODO: CSharpFormattingOptions
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;

			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
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
			//sect.Attribute(nameof(FullyQualifyAmbiguousTypeNames), FullyQualifyAmbiguousTypeNames);
			//sect.Attribute(nameof(FullyQualifyAllTypes), FullyQualifyAllTypes);
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
			//sect.Attribute(nameof(AllowFieldInitializers), AllowFieldInitializers);
			//TODO: CSharpFormattingOptions
		}
	}
}
