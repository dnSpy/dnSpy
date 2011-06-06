// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Windows.Media;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	internal class SavedTreeNode : TreeNode
	{			
		public override bool CanSetText { 
			get { return true; }
		}
		
		public SavedTreeNode(ImageSource image, string fullname, string text)
		{
			base.ImageSource = image;
			FullName = fullname;
			Text = text;
		}
		
		public override bool SetText(string newValue) { 
			Text = newValue;
			return false;
		}
	}
}
