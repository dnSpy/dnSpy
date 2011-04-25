//
// ExceptionHandler.cs
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

namespace Mono.Cecil.Cil {

	public enum ExceptionHandlerType {
		Catch = 0,
		Filter = 1,
		Finally = 2,
		Fault = 4,
	}

	public sealed class ExceptionHandler {

		Instruction try_start;
		Instruction try_end;
		Instruction filter_start;
		Instruction handler_start;
		Instruction handler_end;

		TypeReference catch_type;
		ExceptionHandlerType handler_type;

		public Instruction TryStart {
			get { return try_start; }
			set { try_start = value; }
		}

		public Instruction TryEnd {
			get { return try_end; }
			set { try_end = value; }
		}

		public Instruction FilterStart {
			get { return filter_start; }
			set { filter_start = value; }
		}

		public Instruction HandlerStart {
			get { return handler_start; }
			set { handler_start = value; }
		}

		public Instruction HandlerEnd {
			get { return handler_end; }
			set { handler_end = value; }
		}

		public TypeReference CatchType {
			get { return catch_type; }
			set { catch_type = value; }
		}

		public ExceptionHandlerType HandlerType {
			get { return handler_type; }
			set { handler_type = value; }
		}

		public ExceptionHandler (ExceptionHandlerType handlerType)
		{
			this.handler_type = handlerType;
		}
	}
}
