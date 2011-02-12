// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents an accessor (property getter/setter; or event add/remove/invoke).
	/// </summary>
	public interface IAccessor : IFreezable
	{
		/// <summary>
		/// Gets the accessor region.
		/// </summary>
		DomRegion Region { get; }
		
		/// <summary>
		/// Gets the attributes defined on this accessor.
		/// </summary>
		IList<IAttribute> Attributes { get; }
		
		/// <summary>
		/// Gets the accessibility of this accessor.
		/// </summary>
		Accessibility Accessibility { get; }
	}
}
