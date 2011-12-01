// 
// TestBase.cs
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
using System.Collections.Generic;
using NUnit.Framework;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.Completion;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Editor;
using System.Diagnostics;

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	[TestFixture]
	public class TestBase 
	{
		class TestListener : TraceListener
		{
			public override void Fail (string message)
			{
				throw new Exception ("Assertion failed:"+  message);
			}
			public override void Write (string o)
			{
				Console.Write (o);
			}
			public override void WriteLine (string o)
			{
				Console.WriteLine (o);
			}
		}
		TestListener listener = new TestListener ();
		
		[TestFixtureSetUp]
		public void FixtureSetUp ()
		{
			System.Diagnostics.Debug.Listeners.Add (listener);
		}
		
		[TestFixtureTearDown]
		public void FixtureTearDown ()
		{
			System.Diagnostics.Debug.Listeners.Remove (listener);
		}
	}
	
}
