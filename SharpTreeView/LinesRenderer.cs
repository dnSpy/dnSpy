// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace ICSharpCode.TreeView
{
	class LinesRenderer : FrameworkElement
	{
		static LinesRenderer()
		{
			pen = new Pen(Brushes.LightGray, 1);
			pen.Freeze();
		}

		static Pen pen;

		SharpTreeNodeView NodeView
		{
			get { return TemplatedParent as SharpTreeNodeView; }
		}

		protected override void OnRender(DrawingContext dc)
		{
			var indent = NodeView.CalculateIndent();
			var p = new Point(indent + 4.5, 0);

			if (!NodeView.Node.IsRoot || NodeView.ParentTreeView.ShowRootExpander) {
				dc.DrawLine(pen, new Point(p.X, ActualHeight / 2), new Point(p.X + 10, ActualHeight / 2));
			}

			if (NodeView.Node.IsRoot) return;

			if (NodeView.Node.IsLast) {
				dc.DrawLine(pen, p, new Point(p.X, ActualHeight / 2));
			}
			else {
				dc.DrawLine(pen, p, new Point(p.X, ActualHeight));
			}

			var current = NodeView.Node;
			while (true) {
				p.X -= 19;
				current = current.Parent;
				if (p.X < 0) break;
				if (!current.IsLast) {
					dc.DrawLine(pen, p, new Point(p.X, ActualHeight));
				}
			}
		}
	}
}
