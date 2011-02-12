// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class SignatureComparer : IEqualityComparer<IMember>
	{
		ParameterListComparer parameterListComparer = new ParameterListComparer();
		
		public bool Equals(IMember x, IMember y)
		{	
			if (x.EntityType != y.EntityType)
				return false;
			
			if (x.Name != y.Name)
				return false;
			
			if (x is IMethod && y is IMethod)
				return parameterListComparer.Equals(x as IMethod, y as IMethod);
			
			return true;
		}
		
		public int GetHashCode(IMember obj)
		{
			int hashCode = obj.Name.GetHashCode();
			
			if (obj is IMethod)
				hashCode ^= parameterListComparer.GetHashCode(obj as IMethod);
			
			return hashCode;
		}
	}
}
