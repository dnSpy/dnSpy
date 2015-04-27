// Copyright (c) 2009-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// Determines whether two objects are identical (one is a reused version of the other).
	/// </summary>
	public class ReuseEqualityComparer : IEqualityComparer<AXmlObject>
	{
		/// <summary>
		/// Determines whether two objects are identical (one is a reused version of the other).
		/// </summary>
		public bool Equals(AXmlObject x, AXmlObject y)
		{
			if (x == y)
				return true;
			if (x == null || y == null)
				return false;
			return x.internalObject == y.internalObject;
		}
		
		/// <summary>
		/// Gets the object's hash code so that reused versions of an object have the same hash code.
		/// </summary>
		public int GetHashCode(AXmlObject obj)
		{
			if (obj == null)
				return 0;
			else
				return obj.internalObject.GetHashCode();
		}
	}
}
