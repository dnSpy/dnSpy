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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;
using dnSpy.Shared.UI.MVVM;
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

	sealed class DecompilerAppSettingsTab : IAppSettingsTab, INotifyPropertyChanged {
		readonly DecompilerSettings _global_decompilerSettings;
		readonly DecompilerSettings decompilerSettings;

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public double Order {
			get { return AppSettingsConstants.ORDER_SETTINGS_TAB_DECOMPILER; }
		}

		public string Title {
			get { return dnSpy_Resources.DecompilerDlgTabTitle; }
		}

		public DecompilerSettings Settings {
			get { return decompilerSettings; }
		}

		public object UIObject {
			get { return this; }
		}

		public DecompilationObjectVM[] DecompilationObjectsArray {
			get { return decompilationObjectVMs; }
		}
		readonly DecompilationObjectVM[] decompilationObjectVMs;

		public DecompilationObjectVM DecompilationObject0 {
			get { return decompilationObjectVMs[0]; }
			set { SetDecompilationObject(0, value); }
		}

		public DecompilationObjectVM DecompilationObject1 {
			get { return decompilationObjectVMs[1]; }
			set { SetDecompilationObject(1, value); }
		}

		public DecompilationObjectVM DecompilationObject2 {
			get { return decompilationObjectVMs[2]; }
			set { SetDecompilationObject(2, value); }
		}

		public DecompilationObjectVM DecompilationObject3 {
			get { return decompilationObjectVMs[3]; }
			set { SetDecompilationObject(3, value); }
		}

		public DecompilationObjectVM DecompilationObject4 {
			get { return decompilationObjectVMs[4]; }
			set { SetDecompilationObject(4, value); }
		}

		void SetDecompilationObject(int index, DecompilationObjectVM newValue) {
			Debug.Assert(newValue != null);
			if (newValue == null)
				throw new ArgumentNullException();
			if (decompilationObjectVMs[index] == newValue)
				return;

			int otherIndex = Array.IndexOf(decompilationObjectVMs, newValue);
			Debug.Assert(otherIndex >= 0);
			if (otherIndex >= 0) {
				decompilationObjectVMs[otherIndex] = decompilationObjectVMs[index];
				decompilationObjectVMs[index] = newValue;

				OnPropertyChanged(string.Format("DecompilationObject{0}", otherIndex));
			}
			OnPropertyChanged(string.Format("DecompilationObject{0}", index));
		}

		public DecompilerAppSettingsTab(DecompilerSettings decompilerSettings) {
			this._global_decompilerSettings = decompilerSettings;
			this.decompilerSettings = decompilerSettings.Clone();

			var defObjs = new DecompilerSettings().DecompilationObjectsArray;
			this.decompilationObjectVMs = new DecompilationObjectVM[defObjs.Length];
			for (int i = 0; i < defObjs.Length; i++)
				this.decompilationObjectVMs[i] = new DecompilationObjectVM(defObjs[i], ToString(defObjs[i]));

			this.DecompilationObject0 = this.decompilationObjectVMs.First(a => a.Object == decompilerSettings.DecompilationObject0);
			this.DecompilationObject1 = this.decompilationObjectVMs.First(a => a.Object == decompilerSettings.DecompilationObject1);
			this.DecompilationObject2 = this.decompilationObjectVMs.First(a => a.Object == decompilerSettings.DecompilationObject2);
			this.DecompilationObject3 = this.decompilationObjectVMs.First(a => a.Object == decompilerSettings.DecompilationObject3);
			this.DecompilationObject4 = this.decompilationObjectVMs.First(a => a.Object == decompilerSettings.DecompilationObject4);
		}

		static string ToString(DecompilationObject o) {
			switch (o) {
			case DecompilationObject.NestedTypes:	return dnSpy_Resources.DecompilationOrder_NestedTypes;
			case DecompilationObject.Fields:		return dnSpy_Resources.DecompilationOrder_Fields;
			case DecompilationObject.Events:		return dnSpy_Resources.DecompilationOrder_Events;
			case DecompilationObject.Properties:	return dnSpy_Resources.DecompilationOrder_Properties;
			case DecompilationObject.Methods:		return dnSpy_Resources.DecompilationOrder_Methods;
			default:
				Debug.Fail("Shouldn't be here");
				return "???";
			}
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

			d.DecompilationObject0 = DecompilationObject0.Object;
			d.DecompilationObject1 = DecompilationObject1.Object;
			d.DecompilationObject2 = DecompilationObject2.Object;
			d.DecompilationObject3 = DecompilationObject3.Object;
			d.DecompilationObject4 = DecompilationObject4.Object;

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

	sealed class DecompilationObjectVM : ViewModelBase {
		public DecompilationObject Object {
			get { return decompilationObject; }
		}
		readonly DecompilationObject decompilationObject;

		public string Text {
			get { return text; }
		}
		readonly string text;

		public DecompilationObjectVM(DecompilationObject decompilationObject, string text) {
			this.decompilationObject = decompilationObject;
			this.text = text;
		}
	}
}
