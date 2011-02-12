// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public sealed class ParameterListComparer : IEqualityComparer<IParameterizedMember>
	{
		public static readonly ParameterListComparer Instance = new ParameterListComparer();
		
		public bool Equals(IParameterizedMember x, IParameterizedMember y)
		{
			var px = x.Parameters;
			var py = y.Parameters;
			if (px.Count != py.Count)
				return false;
			for (int i = 0; i < px.Count; i++) {
				if (!px[i].Type.Equals(py[i].Type))
					return false;
			}
			return true;
		}
		
		public int GetHashCode(IParameterizedMember obj)
		{
			int hashCode = obj.Parameters.Count;
			unchecked {
				foreach (IParameter p in obj.Parameters) {
					hashCode *= 27;
					hashCode += p.Type.GetHashCode();
				}
			}
			return hashCode;
		}
	}
}
