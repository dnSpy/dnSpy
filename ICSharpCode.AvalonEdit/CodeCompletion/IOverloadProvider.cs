// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// Provides the items for the OverloadViewer.
	/// </summary>
	public interface IOverloadProvider : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets/Sets the selected index.
		/// </summary>
		int SelectedIndex { get; set; }
		
		/// <summary>
		/// Gets the number of overloads.
		/// </summary>
		int Count { get; }
		
		/// <summary>
		/// Gets the text 'SelectedIndex of Count'.
		/// </summary>
		string CurrentIndexText { get; }
		
		/// <summary>
		/// Gets the current header.
		/// </summary>
		object CurrentHeader { get; }
		
		/// <summary>
		/// Gets the current content.
		/// </summary>
		object CurrentContent { get; }
	}
}
