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
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedEventTreeNode : AnalyzerEntityTreeNode
	{
		private readonly EventDefinition analyzedEvent;
		private readonly string prefix;

		public AnalyzedEventTreeNode(EventDefinition analyzedEvent, string prefix = "")
		{
			if (analyzedEvent == null)
				throw new ArgumentNullException("analyzedEvent");
			this.analyzedEvent = analyzedEvent;
			this.prefix = prefix;
			this.LazyLoading = true;
		}

		public override MemberReference Member {
			get { return analyzedEvent; }
		}

		public override object Icon
		{
			get { return EventTreeNode.GetIcon(analyzedEvent); }
		}

		public override object Text
		{
			get
			{
				// TODO: This way of formatting is not suitable for events which explicitly implement interfaces.
				return prefix + Language.TypeToString(analyzedEvent.DeclaringType, true) + "." + EventTreeNode.GetText(analyzedEvent, Language);
			}
		}

		protected override void LoadChildren()
		{
			if (analyzedEvent.AddMethod != null)
				this.Children.Add(new AnalyzedEventAccessorTreeNode(analyzedEvent.AddMethod, "add"));
			
			if (analyzedEvent.RemoveMethod != null)
				this.Children.Add(new AnalyzedEventAccessorTreeNode(analyzedEvent.RemoveMethod, "remove"));
			
			foreach (var accessor in analyzedEvent.OtherMethods)
				this.Children.Add(new AnalyzedEventAccessorTreeNode(accessor, null));

			if (AnalyzedEventFiredByTreeNode.CanShow(analyzedEvent))
				this.Children.Add(new AnalyzedEventFiredByTreeNode(analyzedEvent));

			if (AnalyzedEventOverridesTreeNode.CanShow(analyzedEvent))
				this.Children.Add(new AnalyzedEventOverridesTreeNode(analyzedEvent));
			
			if (AnalyzedInterfaceEventImplementedByTreeNode.CanShow(analyzedEvent))
				this.Children.Add(new AnalyzedInterfaceEventImplementedByTreeNode(analyzedEvent));
		}

		public static AnalyzerTreeNode TryCreateAnalyzer(MemberReference member)
		{
			if (CanShow(member))
				return new AnalyzedEventTreeNode(member as EventDefinition);
			else
				return null;
		}

		public static bool CanShow(MemberReference member)
		{
			var eventDef = member as EventDefinition;
			if (eventDef == null)
				return false;

			return !MainWindow.Instance.CurrentLanguage.ShowMember(eventDef.AddMethod ?? eventDef.RemoveMethod)
				|| AnalyzedEventOverridesTreeNode.CanShow(eventDef);
		}
	}
}
