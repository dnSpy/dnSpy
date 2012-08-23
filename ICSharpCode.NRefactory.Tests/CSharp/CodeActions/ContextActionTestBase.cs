// 
// ContextActionTestBase.cs
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Threading;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	public abstract class ContextActionTestBase
	{
		internal static string HomogenizeEol (string str)
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < str.Length; i++) {
				var ch = str [i];
				if (ch == '\n') {
					sb.AppendLine ();
				} else if (ch == '\r') {
					sb.AppendLine ();
					if (i + 1 < str.Length && str [i + 1] == '\n')
						i++;
				} else {
					sb.Append (ch);
				}
			}
			return sb.ToString ();
		}

		public void Test<T> (string input, string output, int action = 0, bool expectErrors = false) 
			where T : ICodeActionProvider, new ()
		{
			string result = RunContextAction (new T (), HomogenizeEol (input), action, expectErrors);
			bool passed = result == output;
			if (!passed) {
				Console.WriteLine ("-----------Expected:");
				Console.WriteLine (output);
				Console.WriteLine ("-----------Got:");
				Console.WriteLine (result);
			}
			Assert.AreEqual (HomogenizeEol (output), result);
		}	
	

		protected static string RunContextAction (ICodeActionProvider action, string input,
		                                          int actionIndex = 0, bool expectErrors = false)
		{
			var context = TestRefactoringContext.Create (input, expectErrors);
			bool isValid = action.GetActions (context).Any ();

			if (!isValid)
				Console.WriteLine ("invalid node is:" + context.GetNode ());
			Assert.IsTrue (isValid, action.GetType () + " is invalid.");
			using (var script = context.StartScript ()) {
				action.GetActions (context).Skip (actionIndex).First ().Run (script);
			}

			return context.doc.Text;
		}
		
		protected static void TestWrongContext<T> (string input) where T : ICodeActionProvider, new ()
		{
			TestWrongContext (new T(), input);
		}
		
		protected static void TestWrongContext (ICodeActionProvider action, string input)
		{
			var context = TestRefactoringContext.Create (input);
			bool isValid = action.GetActions (context).Any ();
			if (!isValid)
				Console.WriteLine ("invalid node is:" + context.GetNode ());
			Assert.IsTrue (!isValid, action.GetType () + " shouldn't be valid there.");
		}
	}
}
