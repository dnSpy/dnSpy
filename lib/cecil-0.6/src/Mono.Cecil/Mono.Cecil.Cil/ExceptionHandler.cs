//
// ExceptionHandler.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
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

namespace Mono.Cecil.Cil {

	using Mono.Cecil;

	public sealed class ExceptionHandler : ICodeVisitable {

		Instruction m_tryStart;
		Instruction m_tryEnd;
		Instruction m_filterStart;
		Instruction m_filterEnd;
		Instruction m_handlerStart;
		Instruction m_handlerEnd;

		TypeReference m_catchType;
		ExceptionHandlerType m_type;

		public Instruction TryStart {
			get { return m_tryStart; }
			set { m_tryStart = value; }
		}

		public Instruction TryEnd {
			get { return m_tryEnd; }
			set { m_tryEnd = value; }
		}

		public Instruction FilterStart {
			get { return m_filterStart; }
			set { m_filterStart = value; }
		}

		public Instruction FilterEnd {
			get { return m_filterEnd; }
			set { m_filterEnd = value; }
		}

		public Instruction HandlerStart {
			get { return m_handlerStart; }
			set { m_handlerStart = value; }
		}

		public Instruction HandlerEnd {
			get { return m_handlerEnd; }
			set { m_handlerEnd = value; }
		}

		public TypeReference CatchType {
			get { return m_catchType; }
			set { m_catchType = value; }
		}

		public ExceptionHandlerType Type {
			get { return m_type; }
			set { m_type = value; }
		}

		public ExceptionHandler (ExceptionHandlerType type)
		{
			m_type = type;
		}

		public void Accept (ICodeVisitor visitor)
		{
			visitor.VisitExceptionHandler (this);
		}
	}
}
