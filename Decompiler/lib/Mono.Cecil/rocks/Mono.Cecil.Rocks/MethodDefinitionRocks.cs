//
// MethodDefinitionRocks.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
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

namespace Mono.Cecil.Rocks {

#if INSIDE_ROCKS
	public
#endif
	static class MethodDefinitionRocks {

		public static MethodDefinition GetBaseMethod (this MethodDefinition self)
		{
			if (self == null)
				throw new ArgumentNullException ("self");
			if (!self.IsVirtual)
				return self;

			var base_type = ResolveBaseType (self.DeclaringType);
			while (base_type != null) {
				var @base = GetMatchingMethod (base_type, self);
				if (@base != null)
					return @base;

				base_type = ResolveBaseType (base_type);
			}

			return self;
		}

		public static MethodDefinition GetOriginalBaseMethod (this MethodDefinition self)
		{
			if (self == null)
				throw new ArgumentNullException ("self");

			while (true) {
				var @base = self.GetBaseMethod ();
				if (@base == self)
					return self;

				self = @base;
			}
		}

		static TypeDefinition ResolveBaseType (TypeDefinition type)
		{
			if (type == null)
				return null;

			var base_type = type.BaseType;
			if (base_type == null)
				return null;

			return base_type.Resolve ();
		}

		static MethodDefinition GetMatchingMethod (TypeDefinition type, MethodDefinition method)
		{
			return MetadataResolver.GetMethod (type.Methods, method);
		}
	}
}
