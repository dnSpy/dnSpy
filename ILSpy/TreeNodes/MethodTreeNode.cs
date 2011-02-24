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
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TreeNodes.Analyzer;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Tree Node representing a field, method, property, or event.
	/// </summary>
	sealed class MethodTreeNode : ILSpyTreeNode
	{
		MethodDefinition method;
		
		public MethodDefinition MethodDefinition {
			get { return method; }
		}
		
		public MethodTreeNode(MethodDefinition method)
		{
			if (method == null)
				throw new ArgumentNullException("method");
			this.method = method;
		}
		
		public override object Text {
			get {
				return GetText(method, Language);
			}
		}

		public static object GetText(MethodDefinition method, Language language)
		{
			StringBuilder b = new StringBuilder();
			b.Append('(');
			for (int i = 0; i < method.Parameters.Count; i++) {
				if (i > 0)
					b.Append(", ");
				b.Append(language.TypeToString(method.Parameters[i].ParameterType, false, method.Parameters[i]));
			}
			b.Append(") : ");
			b.Append(language.TypeToString(method.ReturnType, false, method.MethodReturnType));
			return HighlightSearchMatch(method.Name, b.ToString());
		}
		
		public override object Icon {
			get {
				return GetIcon(method);
			}
		}

		public static BitmapImage GetIcon(MethodDefinition method)
		{
			if (method.IsSpecialName && method.Name.StartsWith("op_", StringComparison.Ordinal))
				return Images.Operator;
			if (method.IsStatic && method.HasCustomAttributes) {
				foreach (var ca in method.CustomAttributes) {
					if (ca.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute")
						return Images.ExtensionMethod;
				}
			}
			return Images.Method;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileMethod(method, output, options);
		}
		
		public override FilterResult Filter(FilterSettings settings)
		{
			if (settings.SearchTermMatches(method.Name) && settings.Language.ShowMember(method))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}
		
		public override System.Windows.Controls.ContextMenu GetContextMenu()
		{
			ContextMenu menu = new ContextMenu();
			MenuItem item = new MenuItem() { Header = "Analyze", Icon = new Image() { Source = Images.Search } };
			item.Click += delegate { MainWindow.Instance.AddToAnalyzer(new AnalyzedMethodTreeNode(method)); };
			
			menu.Items.Add(item);
			
			return menu;
		}
	}
}
