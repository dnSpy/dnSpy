// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	internal interface ISetText
	{
		bool CanSetText { get; }
		
		bool SetText(string text);
	}
}
