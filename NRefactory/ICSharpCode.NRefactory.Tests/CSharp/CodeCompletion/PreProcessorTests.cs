// 
// PreProcessorTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	public class PreProcessorTests: TestBase
	{
		[Test()]
		public void TestPreProcessorContext ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$#$", provider => {
				Assert.IsNotNull (provider.Find ("if"), "directive 'if' not found.");
				Assert.IsNotNull (provider.Find ("region"), "directive 'region' not found.");
			});
		}
		
		[Test()]
		public void TestPreProcessorContext2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"// $#$", provider => {
				Assert.IsNull (provider.Find ("if"), "directive 'if' not found.");
				Assert.IsNull (provider.Find ("region"), "directive 'region' not found.");
			});
		}
		
		
		[Test()]
		public void TestIfContext ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$#if $", provider => {
				Assert.IsNotNull (provider.Find ("DEBUG"), "define 'DEBUG' not found.");
			});
		}	

		[Test()]
		public void TestIfInsideComment ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$// #if $", provider => {
				Assert.IsNull (provider.Find ("DEBUG"), "define 'DEBUG' found.");
			});
		}	
	}
}
