// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Windows;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Adds additional WPF-specific output features to <see cref="ITextOutput"/>.
	/// </summary>
	public interface ISmartTextOutput : ITextOutput
	{
		/// <summary>
		/// Inserts an interactive UI element at the current position in the text output.
		/// </summary>
		void AddUIElement(Func<UIElement> element);
	}
}
