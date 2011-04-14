// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	/// <summary>
	/// Node that can be bound to <see cref="DebuggerTooltipControl" />.
	/// </summary>
	internal interface ITreeNode : IComparable<ITreeNode>
	{
		string Name { get; }
		
		string FullName { get; }
		
		string ImageName { get; }
		
		string Text { get; }
		
		bool CanSetText { get; }
		
		string Type { get; }
		
		ImageSource ImageSource { get; }
		
		IEnumerable<ITreeNode> ChildNodes { get; }
		
		bool HasChildNodes { get; }
		
		IEnumerable<IVisualizerCommand> VisualizerCommands { get; }
		
		bool HasVisualizerCommands { get; }
		
		bool IsPinned { get; set; }
		
		bool SetText(string newValue);
	}
}
