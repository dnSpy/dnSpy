// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	class AnalyzedFieldNode : AnalyzerTreeNode
	{
		FieldDefinition analyzedField;
		
		public AnalyzedFieldNode(FieldDefinition analyzedField)
		{
			if (analyzedField == null)
				throw new ArgumentNullException("analyzedField");
			this.analyzedField = analyzedField;
			this.LazyLoading = true;
		}
		
		public override object Icon {
			get { return FieldTreeNode.GetIcon(analyzedField); }
		}
		
		public override object Text {
			get {
				return Language.TypeToString(analyzedField.DeclaringType, true) +
					"." + analyzedField.Name + " : " + this.Language.TypeToString(analyzedField.FieldType, false, analyzedField);
			}
		}
		
		public override void ActivateItem(System.Windows.RoutedEventArgs e)
		{
			e.Handled = true;
			MainWindow.Instance.JumpToReference(analyzedField);
		}
		
		protected override void LoadChildren()
		{
			this.Children.Add(new AnalyzedFieldAccessNode(analyzedField, false));
			if (!analyzedField.IsLiteral)
				this.Children.Add(new AnalyzedFieldAccessNode(analyzedField, true));
		}
	}
}
