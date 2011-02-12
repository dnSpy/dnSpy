// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	sealed class UnknownReturnType : ProxyReturnType
	{
		public static readonly UnknownReturnType Instance = new UnknownReturnType();
		
		public override IReturnType BaseType {
			get {
				return null;
			}
		}
	}
}
