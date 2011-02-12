// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom.VBNet
{
	/// <summary>
	/// Description of IVBNetOptionProvider.
	/// </summary>
	public interface IVBNetOptionProvider
	{
		bool? OptionInfer { get; }
		bool? OptionStrict { get; }
		bool? OptionExplicit { get; }
		CompareKind? OptionCompare { get; }
	}
	
	public enum CompareKind
	{
		Binary,
		Text
	}
}
