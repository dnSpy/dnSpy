// 
// IParameterCopmletionFactory.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	public interface IParameterCompletionDataFactory
	{
		IParameterDataProvider CreateConstructorProvider (int startOffset, IType type);

		/// <summary>
		/// Creates a constructor provider skipping the parent of thisInitializer.
		/// </summary>
		IParameterDataProvider CreateConstructorProvider (int startOffset, IType type, AstNode thisInitializer);

		IParameterDataProvider CreateMethodDataProvider (int startOffset, IEnumerable<IMethod> methods);

		IParameterDataProvider CreateDelegateDataProvider (int startOffset, IType type);
		
		IParameterDataProvider CreateIndexerParameterDataProvider (int startOffset, IType type, IEnumerable<IProperty> accessibleIndexers, AstNode resolvedNode);
		
		IParameterDataProvider CreateTypeParameterDataProvider (int startOffset, IEnumerable<IType> types);

		IParameterDataProvider CreateTypeParameterDataProvider (int startOffset, IEnumerable<IMethod> methods);
	}
	
}
