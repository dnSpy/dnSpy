// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	public interface IProperty : IMethodOrProperty
	{
		DomRegion GetterRegion {
			get;
		}

		DomRegion SetterRegion {
			get;
		}

		bool CanGet {
			get;
		}

		bool CanSet {
			get;
		}
		
		bool IsIndexer {
			get;
		}
		
		ModifierEnum GetterModifiers {
			get;
		}
		
		ModifierEnum SetterModifiers {
			get;
		}
	}
}
