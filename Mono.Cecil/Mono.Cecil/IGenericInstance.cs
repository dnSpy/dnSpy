//
// IGenericInstance.cs
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

using System.Text;

using Mono.Collections.Generic;

namespace Mono.Cecil {

	public interface IGenericInstance : IMetadataTokenProvider {

		bool HasGenericArguments { get; }
		Collection<TypeReference> GenericArguments { get; }
	}

	static partial class Mixin {

		public static bool ContainsGenericParameter (this IGenericInstance self)
		{
			var arguments = self.GenericArguments;

			for (int i = 0; i < arguments.Count; i++)
				if (arguments [i].ContainsGenericParameter)
					return true;

			return false;
		}

		public static void GenericInstanceFullName (this IGenericInstance self, StringBuilder builder)
		{
			builder.Append ("<");
			var arguments = self.GenericArguments;
			for (int i = 0; i < arguments.Count; i++) {
				if (i > 0)
					builder.Append (",");
				builder.Append (arguments [i].FullName);
			}
			builder.Append (">");
		}
	}
}
