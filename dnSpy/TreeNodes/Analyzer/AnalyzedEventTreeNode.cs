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
using dnlib.DotNet;
using dnSpy.Images;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	internal sealed class AnalyzedEventTreeNode : AnalyzerEntityTreeNode
	{
		private readonly EventDef analyzedEvent;
		private readonly bool hidesParent;

		public AnalyzedEventTreeNode(EventDef analyzedEvent, bool hidesParent = false)
		{
			if (analyzedEvent == null)
				throw new ArgumentNullException("analyzedEvent");
			this.analyzedEvent = analyzedEvent;
			this.hidesParent = hidesParent;
			this.LazyLoading = true;
		}

		public override IMemberRef Member {
			get { return analyzedEvent; }
		}

		public override IMDTokenProvider MDTokenProvider {
			get { return analyzedEvent; }
		}

		public override object Icon
		{
			get { return EventTreeNode.GetIcon(analyzedEvent, BackgroundType.TreeNode); }
		}

		protected override void Write(ITextOutput output, Language language)
		{
			if (hidesParent) {
				output.Write('(', TextTokenType.Operator);
				output.Write("hides", TextTokenType.Text);
				output.Write(')', TextTokenType.Operator);
				output.WriteSpace();
			}
			Language.TypeToString(output, analyzedEvent.DeclaringType, true);
			output.Write('.', TextTokenType.Operator);
			EventTreeNode.Write(output, analyzedEvent, Language);
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

		public static AnalyzerTreeNode TryCreateAnalyzer(IMemberRef member)
		{
			if (CanShow(member))
				return new AnalyzedEventTreeNode(member as EventDef);
			else
				return null;
		}

		public static bool CanShow(IMemberRef member)
		{
			var eventDef = member as EventDef;
			if (eventDef == null)
				return false;

			return !MainWindow.Instance.CurrentLanguage.ShowMember(eventDef.AddMethod ?? eventDef.RemoveMethod)
				|| AnalyzedEventOverridesTreeNode.CanShow(eventDef);
		}
	}
}
