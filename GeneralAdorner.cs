// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;

namespace ICSharpCode.TreeView
{
	public class GeneralAdorner : Adorner
	{
		public GeneralAdorner(UIElement target)
			: base(target)
		{
		}

		FrameworkElement child;

		public FrameworkElement Child
		{
			get
			{
				return child;
			}
			set
			{
				if (child != value) {
					RemoveVisualChild(child);
					RemoveLogicalChild(child);
					child = value;
					AddLogicalChild(value);
					AddVisualChild(value);
					InvalidateMeasure();
				}
			}
		}

		protected override int VisualChildrenCount
		{
			get { return child == null ? 0 : 1; }
		}

		protected override Visual GetVisualChild(int index)
		{
			return child;
		}

		protected override Size MeasureOverride(Size constraint)
		{
			if (child != null) {
				child.Measure(constraint);
				return child.DesiredSize;
			}
			return new Size();
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (child != null) {
				child.Arrange(new Rect(finalSize));
				return finalSize;
			}
			return new Size();
		}
	}
}
