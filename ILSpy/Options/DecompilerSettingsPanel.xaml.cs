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

using System;
using System.Threading;
using System.Windows.Controls;
using System.Xml.Linq;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.Options
{
	/// <summary>
	/// Interaction logic for DecompilerSettingsPanel.xaml
	/// </summary>
	[ExportOptionPage(Title = "Decompiler", Order = 0)]
	sealed class DecompilerSettingsPanelCreator : IOptionPageCreator
	{
		public IOptionPage Create()
		{
			return new DecompilerSettingsPanel();
		}
	}

	partial class DecompilerSettingsPanel : UserControl, IOptionPage
	{
		public DecompilerSettingsPanel()
		{
			InitializeComponent();
		}
		
		public void Load(ILSpySettings settings)
		{
			this.DataContext = LoadDecompilerSettings(settings);
		}
		
		static DecompilerSettings currentDecompilerSettings;
		
		public static DecompilerSettings CurrentDecompilerSettings {
			get {
				if (currentDecompilerSettings != null)
					return currentDecompilerSettings;
				Interlocked.CompareExchange(ref currentDecompilerSettings, LoadDecompilerSettings(ILSpySettings.Load()), null);
				return currentDecompilerSettings;
			}
		}
		
		public static DecompilerSettings LoadDecompilerSettings(ILSpySettings settings)
		{
			XElement e = settings["DecompilerSettings"];
			DecompilerSettings s = new DecompilerSettings();
			s.AnonymousMethods = (bool?)e.Attribute("anonymousMethods") ?? s.AnonymousMethods;
			s.YieldReturn = (bool?)e.Attribute("yieldReturn") ?? s.YieldReturn;
			s.AsyncAwait = (bool?)e.Attribute("asyncAwait") ?? s.AsyncAwait;
			s.QueryExpressions = (bool?)e.Attribute("queryExpressions") ?? s.QueryExpressions;
			s.ExpressionTrees = (bool?)e.Attribute("expressionTrees") ?? s.ExpressionTrees;
			s.UseDebugSymbols = (bool?)e.Attribute("useDebugSymbols") ?? s.UseDebugSymbols;
			s.ShowXmlDocumentation = (bool?)e.Attribute("xmlDoc") ?? s.ShowXmlDocumentation;
			s.ShowILComments = (bool?)e.Attribute("showILComments") ?? s.ShowILComments;
			s.RemoveEmptyDefaultConstructors = (bool?)e.Attribute("removeEmptyDefaultConstructors") ?? s.RemoveEmptyDefaultConstructors;
			s.ShowTokenAndRvaComments = (bool?)e.Attribute("showTokenAndRvaComments") ?? s.ShowTokenAndRvaComments;
			s.ShowILBytes = (bool?)e.Attribute("showILBytes") ?? s.ShowILBytes;
			s.DecompilationObject0 = (DecompilationObject)((int?)e.Attribute("decompilationObject0") ?? (int)s.DecompilationObject0);
			s.DecompilationObject1 = (DecompilationObject)((int?)e.Attribute("decompilationObject1") ?? (int)s.DecompilationObject1);
			s.DecompilationObject2 = (DecompilationObject)((int?)e.Attribute("decompilationObject2") ?? (int)s.DecompilationObject2);
			s.DecompilationObject3 = (DecompilationObject)((int?)e.Attribute("decompilationObject3") ?? (int)s.DecompilationObject3);
			s.DecompilationObject4 = (DecompilationObject)((int?)e.Attribute("decompilationObject4") ?? (int)s.DecompilationObject4);
			s.SortMembers = (bool?)e.Attribute("sortMembers") ?? s.SortMembers;
			return s;
		}
		
		public RefreshFlags Save(XElement root)
		{
			DecompilerSettings s = (DecompilerSettings)this.DataContext;
			var flags = RefreshFlags.None;

			if (CurrentDecompilerSettings.AnonymousMethods != s.AnonymousMethods) flags |= RefreshFlags.ILAst;
			if (CurrentDecompilerSettings.ExpressionTrees != s.ExpressionTrees) flags |= RefreshFlags.ILAst;
			if (CurrentDecompilerSettings.YieldReturn != s.YieldReturn) flags |= RefreshFlags.ILAst;
			if (CurrentDecompilerSettings.AsyncAwait != s.AsyncAwait) flags |= RefreshFlags.ILAst;
			if (CurrentDecompilerSettings.AutomaticProperties != s.AutomaticProperties) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.AutomaticEvents != s.AutomaticEvents) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.UsingStatement != s.UsingStatement) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.ForEachStatement != s.ForEachStatement) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.LockStatement != s.LockStatement) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.SwitchStatementOnString != s.SwitchStatementOnString) flags |= RefreshFlags.CSharp;
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
			if (CurrentDecompilerSettings.ShowTokenAndRvaComments != s.ShowTokenAndRvaComments) flags |= RefreshFlags.CSharp | RefreshFlags.IL;
			if (CurrentDecompilerSettings.ShowILBytes != s.ShowILBytes) flags |= RefreshFlags.IL;
			if (CurrentDecompilerSettings.DecompilationObject0 != s.DecompilationObject0) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.DecompilationObject1 != s.DecompilationObject1) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.DecompilationObject2 != s.DecompilationObject2) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.DecompilationObject3 != s.DecompilationObject3) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.DecompilationObject4 != s.DecompilationObject4) flags |= RefreshFlags.CSharp;
			if (CurrentDecompilerSettings.SortMembers != s.SortMembers) flags |= RefreshFlags.IL | RefreshFlags.CSharp;

			XElement section = new XElement("DecompilerSettings");
			section.SetAttributeValue("anonymousMethods", s.AnonymousMethods);
			section.SetAttributeValue("yieldReturn", s.YieldReturn);
			section.SetAttributeValue("asyncAwait", s.AsyncAwait);
			section.SetAttributeValue("queryExpressions", s.QueryExpressions);
			section.SetAttributeValue("expressionTrees", s.ExpressionTrees);
			section.SetAttributeValue("useDebugSymbols", s.UseDebugSymbols);
			section.SetAttributeValue("xmlDoc", s.ShowXmlDocumentation);
			section.SetAttributeValue("showILComments", s.ShowILComments);
			section.SetAttributeValue("removeEmptyDefaultConstructors", s.RemoveEmptyDefaultConstructors);
			section.SetAttributeValue("showTokenAndRvaComments", s.ShowTokenAndRvaComments);
			section.SetAttributeValue("showILBytes", s.ShowILBytes);
			section.SetAttributeValue("decompilationObject0", (int)s.DecompilationObject0);
			section.SetAttributeValue("decompilationObject1", (int)s.DecompilationObject1);
			section.SetAttributeValue("decompilationObject2", (int)s.DecompilationObject2);
			section.SetAttributeValue("decompilationObject3", (int)s.DecompilationObject3);
			section.SetAttributeValue("decompilationObject4", (int)s.DecompilationObject4);
			section.SetAttributeValue("sortMembers", s.SortMembers);
			
			XElement existingElement = root.Element("DecompilerSettings");
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);
			
			currentDecompilerSettings = null; // invalidate cached settings
			return flags;
		}
	}
}