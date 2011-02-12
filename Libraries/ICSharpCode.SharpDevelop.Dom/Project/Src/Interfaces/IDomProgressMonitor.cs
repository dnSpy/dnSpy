// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
		/// <summary>
	/// This is a basic interface to a "progress bar" type of
	/// control.
	/// </summary>
	public interface IDomProgressMonitor
	{
		/// <summary>
		/// Gets/sets if the task current shows a modal dialog. Set this property to true to make progress
		/// dialogs windows temporarily invisible while your modal dialog is showing.
		/// </summary>
		bool ShowingDialog {
			get;
			set;
		}
	}
}
