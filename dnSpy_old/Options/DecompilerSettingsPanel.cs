// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Threading;
using dnSpy.Contracts;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.Options {
	[ExportOptionPage(Title = "Decompiler", Order = 0)]
	sealed class DecompilerSettingsPanelCreator : IOptionPageCreator {
		public OptionPage Create() {
			return new DecompilerSettingsPanel();
		}
	}

	public class DecompilerSettingsPanel : OptionPage {
		static DecompilerSettings currentDecompilerSettings;

		public DecompilerSettings Settings {
			get { return settings; }
		}
		DecompilerSettings settings;

		public static DecompilerSettings CurrentDecompilerSettings {
			get {
				if (currentDecompilerSettings != null)
					return currentDecompilerSettings;
				Interlocked.CompareExchange(ref currentDecompilerSettings, LoadDecompilerSettings(), null);
				return currentDecompilerSettings;
			}
		}

		public override void Load() {
			this.settings = LoadDecompilerSettings();
		}

		const string SETTINGS_NAME = "AFE83D9E-39AC-4F4E-B7E4-CAA3D132C8AD";

		public static DecompilerSettings LoadDecompilerSettings() {
			var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_NAME);
			DecompilerSettings s = new DecompilerSettings();
			s.AnonymousMethods = section.Attribute<bool?>("AnonymousMethods") ?? s.AnonymousMethods;
			s.YieldReturn = section.Attribute<bool?>("YieldReturn") ?? s.YieldReturn;
			s.AsyncAwait = section.Attribute<bool?>("AsyncAwait") ?? s.AsyncAwait;
			s.QueryExpressions = section.Attribute<bool?>("QueryExpressions") ?? s.QueryExpressions;
			s.ExpressionTrees = section.Attribute<bool?>("ExpressionTrees") ?? s.ExpressionTrees;
			s.UseDebugSymbols = section.Attribute<bool?>("UseDebugSymbols") ?? s.UseDebugSymbols;
			s.ShowXmlDocumentation = section.Attribute<bool?>("ShowXmlDocumentation") ?? s.ShowXmlDocumentation;
			s.ShowILComments = section.Attribute<bool?>("ShowILComments") ?? s.ShowILComments;
			s.RemoveEmptyDefaultConstructors = section.Attribute<bool?>("RemoveEmptyDefaultConstructors") ?? s.RemoveEmptyDefaultConstructors;
			s.ShowTokenAndRvaComments = section.Attribute<bool?>("ShowTokenAndRvaComments") ?? s.ShowTokenAndRvaComments;
			s.ShowILBytes = section.Attribute<bool?>("ShowILBytes") ?? s.ShowILBytes;
			s.DecompilationObject0 = section.Attribute<DecompilationObject?>("DecompilationObject0") ?? s.DecompilationObject0;
			s.DecompilationObject1 = section.Attribute<DecompilationObject?>("DecompilationObject1") ?? s.DecompilationObject1;
			s.DecompilationObject2 = section.Attribute<DecompilationObject?>("DecompilationObject2") ?? s.DecompilationObject2;
			s.DecompilationObject3 = section.Attribute<DecompilationObject?>("DecompilationObject3") ?? s.DecompilationObject3;
			s.DecompilationObject4 = section.Attribute<DecompilationObject?>("DecompilationObject4") ?? s.DecompilationObject4;
			s.SortMembers = section.Attribute<bool?>("SortMembers") ?? s.SortMembers;
			s.ForceShowAllMembers = section.Attribute<bool?>("ForceShowAllMembers") ?? s.ForceShowAllMembers;
			s.SortSystemUsingStatementsFirst = section.Attribute<bool?>("SortSystemUsingStatementsFirst") ?? s.SortSystemUsingStatementsFirst;
			return s;
		}

		public override RefreshFlags Save() {
			DecompilerSettings s = this.settings;
			var flags = RefreshFlags.None;

			if (CurrentDecompilerSettings.AnonymousMethods != s.AnonymousMethods) flags |= RefreshFlags.ILAst | RefreshFlags.TreeViewNodes;
			if (CurrentDecompilerSettings.ExpressionTrees != s.ExpressionTrees) flags |= RefreshFlags.ILAst;
			if (CurrentDecompilerSettings.YieldReturn != s.YieldReturn) flags |= RefreshFlags.ILAst | RefreshFlags.TreeViewNodes;
			if (CurrentDecompilerSettings.AsyncAwait != s.AsyncAwait) flags |= RefreshFlags.ILAst | RefreshFlags.TreeViewNodes;
			if (CurrentDecompilerSettings.AutomaticProperties != s.AutomaticProperties) flags |= RefreshFlags.CSharp | RefreshFlags.TreeViewNodes;
			if (CurrentDecompilerSettings.AutomaticEvents != s.AutomaticEvents) flags |= RefreshFlags.CSharp | RefreshFlags.TreeViewNodes;
			if (CurrentDecompilerSettings.UsingStatement != s.UsingStatement) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.ForEachStatement != s.ForEachStatement) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.LockStatement != s.LockStatement) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.SwitchStatementOnString != s.SwitchStatementOnString) flags |= RefreshFlags.CSharp | RefreshFlags.TreeViewNodes;
			if (CurrentDecompilerSettings.UsingDeclarations != s.UsingDeclarations) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.QueryExpressions != s.QueryExpressions) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.FullyQualifyAmbiguousTypeNames != s.FullyQualifyAmbiguousTypeNames) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.UseDebugSymbols != s.UseDebugSymbols) flags |= RefreshFlags.DecompileAll;
			if (CurrentDecompilerSettings.ObjectOrCollectionInitializers != s.ObjectOrCollectionInitializers) flags |= RefreshFlags.ILAst;
			if (CurrentDecompilerSettings.ShowXmlDocumentation != s.ShowXmlDocumentation) flags |= RefreshFlags.DecompileAll;
			if (CurrentDecompilerSettings.ShowILComments != s.ShowILComments) flags |= RefreshFlags.IL;
			if (CurrentDecompilerSettings.RemoveEmptyDefaultConstructors != s.RemoveEmptyDefaultConstructors) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.IntroduceIncrementAndDecrement != s.IntroduceIncrementAndDecrement) flags |= RefreshFlags.ILAst;
			if (CurrentDecompilerSettings.MakeAssignmentExpressions != s.MakeAssignmentExpressions) flags |= RefreshFlags.ILAst;
			if (CurrentDecompilerSettings.AlwaysGenerateExceptionVariableForCatchBlocks != s.AlwaysGenerateExceptionVariableForCatchBlocks) flags |= RefreshFlags.ILAst;
			if (CurrentDecompilerSettings.ShowTokenAndRvaComments != s.ShowTokenAndRvaComments) flags |= RefreshFlags.DecompileAll;
			if (CurrentDecompilerSettings.ShowILBytes != s.ShowILBytes) flags |= RefreshFlags.IL;
			if (CurrentDecompilerSettings.DecompilationObject0 != s.DecompilationObject0) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.DecompilationObject1 != s.DecompilationObject1) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.DecompilationObject2 != s.DecompilationObject2) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.DecompilationObject3 != s.DecompilationObject3) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.DecompilationObject4 != s.DecompilationObject4) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.SortMembers != s.SortMembers) flags |= RefreshFlags.IL | RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.ForceShowAllMembers != s.ForceShowAllMembers) flags |= RefreshFlags.CSharp | RefreshFlags.TreeViewNodes;
			if (CurrentDecompilerSettings.SortSystemUsingStatementsFirst != s.SortSystemUsingStatementsFirst) flags |= RefreshFlags.CSharp;

			var section = DnSpy.App.SettingsManager.CreateSection(SETTINGS_NAME);
			section.Attribute("AnonymousMethods", s.AnonymousMethods);
			section.Attribute("YieldReturn", s.YieldReturn);
			section.Attribute("AsyncAwait", s.AsyncAwait);
			section.Attribute("QueryExpressions", s.QueryExpressions);
			section.Attribute("ExpressionTrees", s.ExpressionTrees);
			section.Attribute("UseDebugSymbols", s.UseDebugSymbols);
			section.Attribute("ShowXmlDocumentation", s.ShowXmlDocumentation);
			section.Attribute("ShowILComments", s.ShowILComments);
			section.Attribute("RemoveEmptyDefaultConstructors", s.RemoveEmptyDefaultConstructors);
			section.Attribute("ShowTokenAndRvaComments", s.ShowTokenAndRvaComments);
			section.Attribute("ShowILBytes", s.ShowILBytes);
			section.Attribute("DecompilationObject0", s.DecompilationObject0);
			section.Attribute("DecompilationObject1", s.DecompilationObject1);
			section.Attribute("DecompilationObject2", s.DecompilationObject2);
			section.Attribute("DecompilationObject3", s.DecompilationObject3);
			section.Attribute("DecompilationObject4", s.DecompilationObject4);
			section.Attribute("SortMembers", s.SortMembers);
			section.Attribute("ForceShowAllMembers", s.ForceShowAllMembers);
			section.Attribute("SortSystemUsingStatementsFirst", s.SortSystemUsingStatementsFirst);

			currentDecompilerSettings = null; // invalidate cached settings
			return flags;
		}
	}
}