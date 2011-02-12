// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Drawing;
using System.Windows.Forms;

namespace ICSharpCode.NRefactory.Demo
{
	public partial class VBEditDialog
	{
		public VBEditDialog(object element)
		{
			InitializeComponent();
			propertyGrid.SelectedObject = element;
		}
	}
}
