/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings.Dialog;
using ICSharpCode.Decompiler;

namespace dnSpy.Decompiler {
	[Export(typeof(IAppSettingsTabCreator))]
	sealed class DecompilerAppSettingsTabCreator : IAppSettingsTabCreator {
		readonly DecompilerSettings decompilerSettings;

		[ImportingConstructor]
		DecompilerAppSettingsTabCreator(DecompilerSettings decompilerSettings) {
			this.decompilerSettings = decompilerSettings;
		}

		public IEnumerable<IAppSettingsTab> Create() {
			yield return new DecompilerAppSettingsTab(decompilerSettings);
		}
	}

	sealed class DecompilerAppSettingsTab : IAppSettingsTab {
		readonly DecompilerSettings _global_decompilerSettings;
		readonly DecompilerSettings decompilerSettings;

		public double Order {
			get { return AppSettingsConstants.ORDER_SETTINGS_TAB_DECOMPILER; }
		}

		public string Title {
			get { return "Decompiler"; }
		}

		public object UIObject {
			get { return decompilerSettings; }
		}

		public DecompilerAppSettingsTab(DecompilerSettings decompilerSettings) {
			this._global_decompilerSettings = decompilerSettings;
			this.decompilerSettings = decompilerSettings.Clone();
		}

		[Flags]
		public enum RefreshFlags {
			ShowMember			= 0x00000001,
			IL					= 0x00000002,
			ILAst				= 0x00000004,
			CSharp				= 0x00000008,
			VB					= 0x00000010,
			DecompilationOrder	= 0x00000020,
			DecompileAll = IL | ILAst | CSharp | VB,
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;

			RefreshFlags flags = 0;
			var g = _global_decompilerSettings;
			var d = decompilerSettings;
			if (g.AnonymousMethods != d.AnonymousMethods) flags |= RefreshFlags.ILAst | RefreshFlags.ShowMember;
			if (g.ExpressionTrees != d.ExpressionTrees) flags |= RefreshFlags.ILAst;
			if (g.YieldReturn != d.YieldReturn) flags |= RefreshFlags.ILAst | RefreshFlags.ShowMember;
			if (g.AsyncAwait != d.AsyncAwait) flags |= RefreshFlags.ILAst | RefreshFlags.ShowMember;
			if (g.AutomaticProperties != d.AutomaticProperties) flags |= RefreshFlags.CSharp | RefreshFlags.ShowMember;
			if (g.AutomaticEvents != d.AutomaticEvents) flags |= RefreshFlags.CSharp | RefreshFlags.ShowMember;
			if (g.UsingStatement != d.UsingStatement) flags |= RefreshFlags.CSharp;
			if (g.ForEachStatement != d.ForEachStatement) flags |= RefreshFlags.CSharp;
			if (g.LockStatement != d.LockStatement) flags |= RefreshFlags.CSharp;
			if (g.SwitchStatementOnString != d.SwitchStatementOnString) flags |= RefreshFlags.CSharp | RefreshFlags.ShowMember;
			if (g.UsingDeclarations != d.UsingDeclarations) flags |= RefreshFlags.CSharp;
			if (g.QueryExpressions != d.QueryExpressions) flags |= RefreshFlags.CSharp;
			if (g.FullyQualifyAmbiguousTypeNames != d.FullyQualifyAmbiguousTypeNames) flags |= RefreshFlags.CSharp;
			if (g.UseDebugSymbols != d.UseDebugSymbols) flags |= RefreshFlags.DecompileAll;
			if (g.ObjectOrCollectionInitializers != d.ObjectOrCollectionInitializers) flags |= RefreshFlags.ILAst;
			if (g.ShowXmlDocumentation != d.ShowXmlDocumentation) flags |= RefreshFlags.DecompileAll;
			if (g.ShowILComments != d.ShowILComments) flags |= RefreshFlags.IL;
			if (g.RemoveEmptyDefaultConstructors != d.RemoveEmptyDefaultConstructors) flags |= RefreshFlags.CSharp;
			if (g.IntroduceIncrementAndDecrement != d.IntroduceIncrementAndDecrement) flags |= RefreshFlags.ILAst;
			if (g.MakeAssignmentExpressions != d.MakeAssignmentExpressions) flags |= RefreshFlags.ILAst;
			if (g.AlwaysGenerateExceptionVariableForCatchBlocks != d.AlwaysGenerateExceptionVariableForCatchBlocks) flags |= RefreshFlags.ILAst;
			if (g.ShowTokenAndRvaComments != d.ShowTokenAndRvaComments) flags |= RefreshFlags.DecompileAll;
			if (g.ShowILBytes != d.ShowILBytes) flags |= RefreshFlags.IL;
			if (g.DecompilationObject0 != d.DecompilationObject0) flags |= RefreshFlags.CSharp | RefreshFlags.DecompilationOrder;
			if (g.DecompilationObject1 != d.DecompilationObject1) flags |= RefreshFlags.CSharp | RefreshFlags.DecompilationOrder;
			if (g.DecompilationObject2 != d.DecompilationObject2) flags |= RefreshFlags.CSharp | RefreshFlags.DecompilationOrder;
			if (g.DecompilationObject3 != d.DecompilationObject3) flags |= RefreshFlags.CSharp | RefreshFlags.DecompilationOrder;
			if (g.DecompilationObject4 != d.DecompilationObject4) flags |= RefreshFlags.CSharp | RefreshFlags.DecompilationOrder;
			if (g.SortMembers != d.SortMembers) flags |= RefreshFlags.IL | RefreshFlags.CSharp;
			if (g.ForceShowAllMembers != d.ForceShowAllMembers) flags |= RefreshFlags.CSharp | RefreshFlags.ShowMember;
			if (g.SortSystemUsingStatementsFirst != d.SortSystemUsingStatementsFirst) flags |= RefreshFlags.CSharp;

			if ((flags & RefreshFlags.ShowMember) != 0)
				appRefreshSettings.Add(AppSettingsConstants.REFRESH_LANGUAGE_SHOWMEMBER);
			if ((flags & RefreshFlags.DecompilationOrder) != 0)
				appRefreshSettings.Add(AppSettingsConstants.REFRESH_LANGUAGE_DECOMPILATION_ORDER);
			if ((flags & RefreshFlags.IL) != 0)
				appRefreshSettings.Add(AppSettingsConstants.REDISASSEMBLE_IL_CODE);
			if ((flags & RefreshFlags.ILAst) != 0)
				appRefreshSettings.Add(AppSettingsConstants.REDECOMPILE_ILAST_CODE);
			if ((flags & RefreshFlags.CSharp) != 0)
				appRefreshSettings.Add(AppSettingsConstants.REDECOMPILE_CSHARP_CODE);
			if ((flags & RefreshFlags.VB) != 0)
				appRefreshSettings.Add(AppSettingsConstants.REDECOMPILE_VB_CODE);

			decompilerSettings.CopyTo(_global_decompilerSettings);
		}
	}
}
