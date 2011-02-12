// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{

	public interface IParameter : IFreezable, IComparable
	{
		string Name {
			get;
		}

		IReturnType ReturnType {
			get;
			set;
		}

		IList<IAttribute> Attributes {
			get;
		}

		ParameterModifiers Modifiers {
			get;
		}
		
		DomRegion Region {
			get;
		}
		
		string Documentation {
			get;
		}

		bool IsOut {
			get;
		}

		bool IsRef {
			get;
		}

		bool IsParams {
			get;
		}
		
		bool IsOptional {
			get;
		}
	}
}
