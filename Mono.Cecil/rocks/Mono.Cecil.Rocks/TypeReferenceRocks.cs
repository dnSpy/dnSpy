//
// TypeReferenceRocks.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Cecil.Rocks {

#if INSIDE_ROCKS
	public
#endif
	static class TypeReferenceRocks {

		public static ArrayType MakeArrayType (this TypeReference self)
		{
			return new ArrayType (self);
		}

		public static ArrayType MakeArrayType (this TypeReference self, int rank)
		{
			if (rank == 0)
				throw new ArgumentOutOfRangeException ("rank");

			var array = new ArrayType (self);

			for (int i = 1; i < rank; i++)
				array.Dimensions.Add (new ArrayDimension ());

			return array;
		}

		public static PointerType MakePointerType (this TypeReference self)
		{
			return new PointerType (self);
		}

		public static ByReferenceType MakeByReferenceType (this TypeReference self)
		{
			return new ByReferenceType (self);
		}

		public static OptionalModifierType MakeOptionalModifierType (this TypeReference self, TypeReference modifierType)
		{
			return new OptionalModifierType (modifierType, self);
		}

		public static RequiredModifierType MakeRequiredModifierType (this TypeReference self, TypeReference modifierType)
		{
			return new RequiredModifierType (modifierType, self);
		}

		public static GenericInstanceType MakeGenericInstanceType (this TypeReference self, params TypeReference [] arguments)
		{
			if (self == null)
				throw new ArgumentNullException ("self");
			if (arguments == null)
				throw new ArgumentNullException ("arguments");
			if (arguments.Length == 0)
				throw new ArgumentException ();
			if (self.GenericParameters.Count != arguments.Length)
				throw new ArgumentException ();

			var instance = new GenericInstanceType (self);

			foreach (var argument in arguments)
				instance.GenericArguments.Add (argument);

			return instance;
		}

		public static PinnedType MakePinnedType (this TypeReference self)
		{
			return new PinnedType (self);
		}

		public static SentinelType MakeSentinelType (this TypeReference self)
		{
			return new SentinelType (self);
		}
	}
}
