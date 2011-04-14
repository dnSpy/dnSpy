// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	/// <summary>
	/// A node in the variable tree.
	/// The node is imutable.
	/// </summary>
	internal class TreeNode : ITreeNode
	{
		string text  = string.Empty;
		
		IEnumerable<TreeNode> childNodes = null;
		
		public ImageSource ImageSource { get; protected set; }
		
		public string Name { get; set; }

		public string ImageName { get; set; }
		
		public virtual string FullName { 
			get { return Name; }
			set { Name = value; }
		}
		
		public virtual string Text
		{
			get { return text; }
			set { text = value; }
		}
		
		public virtual string Type { get; protected set; }
		
		public virtual IEnumerable<TreeNode> ChildNodes {
			get { return childNodes; }
			protected set { childNodes = value; }
		}
		
		IEnumerable<ITreeNode> ITreeNode.ChildNodes {
			get { return childNodes; }
		}
		
		public virtual bool HasChildNodes {
			get { return childNodes != null; }
		}
		
		public virtual bool CanSetText { 
			get { return false; }
		}
		
		public virtual IEnumerable<IVisualizerCommand> VisualizerCommands {
			get {
				return null;
			}
		}
		
		public virtual bool HasVisualizerCommands {
			get {
				return (VisualizerCommands != null) && (VisualizerCommands.Count() > 0);
			}
		}
		
		public bool IsPinned { get; set; }
		
		public TreeNode()
		{
		}
		
		public TreeNode(ImageSource iconImage, string name, string text, string type, IEnumerable<TreeNode> childNodes)
		{
			this.ImageSource = iconImage;
			this.Name = name;
			this.text = text;
			this.Type = type;
			this.childNodes = childNodes;
		}
		
		public int CompareTo(ITreeNode other)
		{
			return this.FullName.CompareTo(other.FullName);
		}
		
		public virtual bool SetText(string newValue) { 
			return false;
		}
	}
}
