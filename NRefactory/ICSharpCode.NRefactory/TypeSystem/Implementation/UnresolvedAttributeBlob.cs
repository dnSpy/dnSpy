//
// UnresolvedAttributeBlob.cs
//
// Author:
//       Daniel Grunwald <daniel@danielgrunwald.de>
//
// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// <c>IUnresolvedAttribute</c> implementation that loads the arguments from a binary blob.
	/// </summary>
	[Serializable]
	public sealed class UnresolvedAttributeBlob : IUnresolvedAttribute, ISupportsInterning
	{
		internal readonly ITypeReference attributeType;
		internal readonly IList<ITypeReference> ctorParameterTypes;
		internal readonly byte[] blob;
		
		public UnresolvedAttributeBlob(ITypeReference attributeType, IList<ITypeReference> ctorParameterTypes, byte[] blob)
		{
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			if (ctorParameterTypes == null)
				throw new ArgumentNullException("ctorParameterTypes");
			if (blob == null)
				throw new ArgumentNullException("blob");
			this.attributeType = attributeType;
			this.ctorParameterTypes = ctorParameterTypes;
			this.blob = blob;
		}
		
		DomRegion IUnresolvedAttribute.Region {
			get { return DomRegion.Empty; }
		}
		
		public IAttribute CreateResolvedAttribute(ITypeResolveContext context)
		{
			if (context.CurrentAssembly == null)
				throw new InvalidOperationException("Cannot resolve CecilUnresolvedAttribute without a parent assembly");
			return new CecilResolvedAttribute(context, this);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return attributeType.GetHashCode() ^ ctorParameterTypes.GetHashCode() ^ BlobReader.GetBlobHashCode(blob);
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			UnresolvedAttributeBlob o = other as UnresolvedAttributeBlob;
			return o != null && attributeType == o.attributeType && ctorParameterTypes == o.ctorParameterTypes
				&& BlobReader.BlobEquals(blob, o.blob);
		}
	}
}
