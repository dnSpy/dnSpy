//
// ParameterDefinitionCollection.cs
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

using Mono.Collections.Generic;

namespace Mono.Cecil {

	sealed class ParameterDefinitionCollection : Collection<ParameterDefinition> {

		readonly IMethodSignature method;

		internal ParameterDefinitionCollection (IMethodSignature method)
		{
			this.method = method;
		}

		internal ParameterDefinitionCollection (IMethodSignature method, int capacity)
			: base (capacity)
		{
			this.method = method;
		}

		protected override void OnAdd (ParameterDefinition item, int index)
		{
			item.method = method;
			item.index = index;
		}

		protected override void OnInsert (ParameterDefinition item, int index)
		{
			item.method = method;
			item.index = index;

			for (int i = index; i < size; i++)
				items [i].index = i + 1;
		}

		protected override void OnSet (ParameterDefinition item, int index)
		{
			item.method = method;
			item.index = index;
		}

		protected override void OnRemove (ParameterDefinition item, int index)
		{
			item.method = null;
			item.index = -1;

			for (int i = index + 1; i < size; i++)
				items [i].index = i - 1;
		}
	}
}
