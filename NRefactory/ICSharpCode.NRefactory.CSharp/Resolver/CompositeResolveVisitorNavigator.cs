// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	public sealed class CompositeResolveVisitorNavigator : IResolveVisitorNavigator
	{
		IResolveVisitorNavigator[] navigators;
		
		public CompositeResolveVisitorNavigator(params IResolveVisitorNavigator[] navigators)
		{
			if (navigators == null)
				throw new ArgumentNullException("navigators");
			this.navigators = navigators;
			foreach (var n in navigators) {
				if (n == null)
					throw new ArgumentException("Array must not contain nulls.");
			}
		}
		
		public ResolveVisitorNavigationMode Scan(AstNode node)
		{
			bool needsScan = false;
			foreach (var navigator in navigators) {
				ResolveVisitorNavigationMode mode = navigator.Scan(node);
				if (mode == ResolveVisitorNavigationMode.Resolve)
					return mode; // resolve has highest priority
				else if (mode == ResolveVisitorNavigationMode.Scan)
					needsScan = true;
			}
			return needsScan ? ResolveVisitorNavigationMode.Scan : ResolveVisitorNavigationMode.Skip;
		}
		
		public void Resolved(AstNode node, ResolveResult result)
		{
			foreach (var navigator in navigators) {
				navigator.Resolved(node, result);
			}
		}
		
		public void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
		{
			foreach (var navigator in navigators) {
				navigator.ProcessConversion(expression, result, conversion, targetType);
			}
		}
	}
}
