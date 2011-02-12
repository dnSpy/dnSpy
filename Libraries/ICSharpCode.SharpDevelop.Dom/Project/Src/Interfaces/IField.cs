// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	public interface IField : IMember
	{
		/// <summary>Gets if this field is a local variable that has been converted into a field.</summary>
		bool IsLocalVariable { get; }
		
		/// <summary>Gets if this field is a parameter that has been converted into a field.</summary>
		bool IsParameter { get; }
	}
}
