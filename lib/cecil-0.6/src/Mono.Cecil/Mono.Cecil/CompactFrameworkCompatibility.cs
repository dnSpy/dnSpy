//
// FrameworkCompatibility.cs
//
// Author:
//   Rodrigo B. de Oliveira (rodrigobamboo@gmail.com)
//
// (C) 2005 Jb Evain
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
#if CF_1_0
namespace System {

	internal class NotImplementedException : System.Exception {

		public NotImplementedException (string message) : base (message)
		{
		}

		public NotImplementedException ()
		{
		}
	}
}
#endif

#if CF_1_0 || CF_2_0
namespace System.Security {

	public class SecurityElement 	{

		public SecurityElement (string tag)
		{
		}

		public string Text
		{
			get { return string.Empty; }
			set {}
		}

		public void AddChild (SecurityElement child)
		{
		}

		public void AddAttribute (string name, string value)
		{
		}
	}
}
#endif
