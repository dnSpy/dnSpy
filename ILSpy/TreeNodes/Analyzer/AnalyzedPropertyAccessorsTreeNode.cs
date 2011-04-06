using System;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	/// <summary>
	/// Description of AnalyzedPropertyAccessorsTreeNode.
	/// </summary>
	public class AnalyzedPropertyAccessorsTreeNode : AnalyzerTreeNode
	{
		PropertyDefinition analyzedProperty;
		
		public AnalyzedPropertyAccessorsTreeNode(PropertyDefinition analyzedProperty)
		{
			if (analyzedProperty == null)
				throw new ArgumentNullException("analyzedProperty");
			this.analyzedProperty = analyzedProperty;
			
			if (analyzedProperty.GetMethod != null)
				this.Children.Add(new AnalyzedPropertyAccessorTreeNode(analyzedProperty.GetMethod, "get"));
			if (analyzedProperty.SetMethod != null)
				this.Children.Add(new AnalyzedPropertyAccessorTreeNode(analyzedProperty.SetMethod, "set"));
			foreach (var accessor in analyzedProperty.OtherMethods)
				this.Children.Add(new AnalyzedPropertyAccessorTreeNode(accessor, null));
		}
		
		public override object Icon {
			get { return Images.Search; }
		}
		
		public override object Text {
			get { return "Accessors"; }
		}
		
		public static bool CanShow(PropertyDefinition property)
		{
			return !MainWindow.Instance.CurrentLanguage.ShowMember(property.GetMethod ?? property.SetMethod);
		}
		
		class AnalyzedPropertyAccessorTreeNode : AnalyzedMethodTreeNode
		{
			string name;
			
			public AnalyzedPropertyAccessorTreeNode(MethodDefinition analyzedMethod, string name)
				: base(analyzedMethod)
			{
				this.name = name;
			}
			
			public override object Text {
				get { return name ?? base.Text; }
			}
		}
	}
}
