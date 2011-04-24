using System;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	/// <summary>
	/// Description of AnalyzedEventAccessorsTreeNode.
	/// </summary>
	public class AnalyzedEventAccessorsTreeNode : AnalyzerTreeNode
	{
		EventDefinition analyzedEvent;

		public AnalyzedEventAccessorsTreeNode(EventDefinition analyzedEvent)
		{
			if (analyzedEvent == null)
				throw new ArgumentNullException("analyzedEvent");
			this.analyzedEvent = analyzedEvent;

			if (analyzedEvent.AddMethod != null)
				this.Children.Add(new AnalyzedEventAccessorTreeNode(analyzedEvent.AddMethod, "add"));
			if (analyzedEvent.RemoveMethod != null)
				this.Children.Add(new AnalyzedEventAccessorTreeNode(analyzedEvent.RemoveMethod, "remove"));
			foreach (var accessor in analyzedEvent.OtherMethods)
				this.Children.Add(new AnalyzedEventAccessorTreeNode(accessor, null));
		}

		public override object Icon
		{
			get { return Images.Search; }
		}

		public override object Text
		{
			get { return "Accessors"; }
		}

		public static bool CanShow(EventDefinition property)
		{
			return !MainWindow.Instance.CurrentLanguage.ShowMember(property.AddMethod ?? property.RemoveMethod);
		}

		class AnalyzedEventAccessorTreeNode : AnalyzedMethodTreeNode
		{
			string name;

			public AnalyzedEventAccessorTreeNode(MethodDefinition analyzedMethod, string name)
				: base(analyzedMethod)
			{
				this.name = name;
			}

			public override object Text
			{
				get { return name ?? base.Text; }
			}
		}
	}
}
