//
// IMemberDefinition.cs
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

namespace Mono.Cecil {

	public interface IMemberDefinition : ICustomAttributeProvider {

		string Name { get; set; }
		string FullName { get; }

		bool IsSpecialName { get; set; }
		bool IsRuntimeSpecialName { get; set; }

		TypeDefinition DeclaringType { get; set; }
	}

	static partial class Mixin {

		public static bool GetAttributes (this uint self, uint attributes)
		{
			return (self & attributes) != 0;
		}

		public static uint SetAttributes (this uint self, uint attributes, bool value)
		{
			if (value)
				return self | attributes;

			return self & ~attributes;
		}

		public static bool GetMaskedAttributes (this uint self, uint mask, uint attributes)
		{
			return (self & mask) == attributes;
		}

		public static uint SetMaskedAttributes (this uint self, uint mask, uint attributes, bool value)
		{
			if (value) {
				self &= ~mask;
				return self | attributes;
			}

			return self & ~(mask & attributes);
		}

		public static bool GetAttributes (this ushort self, ushort attributes)
		{
			return (self & attributes) != 0;
		}

		public static ushort SetAttributes (this ushort self, ushort attributes, bool value)
		{
			if (value)
				return (ushort) (self | attributes);

			return (ushort) (self & ~attributes);
		}

		public static bool GetMaskedAttributes (this ushort self, ushort mask, uint attributes)
		{
			return (self & mask) == attributes;
		}

		public static ushort SetMaskedAttributes (this ushort self, ushort mask, uint attributes, bool value)
		{
			if (value) {
				self = (ushort) (self & ~mask);
				return (ushort) (self | attributes);
			}

			return (ushort) (self & ~(mask & attributes));
		}
	}
}
