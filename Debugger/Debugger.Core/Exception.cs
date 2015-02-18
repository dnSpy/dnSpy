// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Text;

namespace Debugger
{	
	/// <summary> This convenience class provides access to an exception within the debugee. </summary>
	/// <seealso cref="System.Exception" />
	public class Exception: DebuggerObject
	{
		Value exception;
		
		public Value Value {
			get { return exception; }
		}
		
		public Exception(Value exception)
		{
			this.exception = exception;
		}
		
		/// <summary> The <c>GetType().FullName</c> of the exception. </summary>
		/// <seealso cref="System.Exception" />
		public string Type {
			get {
				return exception.Type.FullName;
			}
		}
		
		/// <summary> The <c>Message</c> property of the exception. </summary>
		/// <seealso cref="System.Exception" />
		public string Message {
			get {
				Value message = exception.GetMemberValue("_message");
				return message.IsNull ? string.Empty : message.AsString();
			}
		}
		
		/// <summary> The <c>InnerException</c> property of the exception. </summary>
		/// <seealso cref="System.Exception" />
		public Exception InnerException {
			get {
				Value innerException = exception.GetMemberValue("_innerException");
				return innerException.IsNull ? null : new Exception(innerException);
			}
		}
		
		public void MakeValuePermanent()
		{
			exception = exception.GetPermanentReference();
		}
		
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(this.Type);
			if (!string.IsNullOrEmpty(this.Message)) {
				sb.Append(": ");
				sb.Append(this.Message);
			}
			if (this.InnerException != null) {
				sb.Append(" ---> ");
				sb.Append(this.InnerException.ToString());
			}
			return sb.ToString();
		}
		
		public string GetStackTrace()
		{
			return GetStackTrace("--- End of inner exception stack trace ---");
		}
		
		/// <summary> Returs formated stacktrace for the exception </summary>
		/// <exception cref="GetValueException"> Getting the stacktrace involves property
		/// evaluation so GetValueException can be thrown in some cicumstances. </exception>
		public string GetStackTrace(string endOfInnerExceptionFormat)
		{
			StringBuilder sb = new StringBuilder();
			if (this.InnerException != null) {
				sb.Append(this.InnerException.GetStackTrace(endOfInnerExceptionFormat));
				sb.Append("   ");
				sb.Append(endOfInnerExceptionFormat);
				sb.AppendLine();
			}
			// Note that evaluation is not possible after a stackoverflow exception
			Value stackTrace = exception.GetMemberValue("StackTrace");
			if (!stackTrace.IsNull) {
				sb.Append(stackTrace.AsString());
				sb.AppendLine();
			}
			return sb.ToString();
		}
	}
	
	public class ExceptionEventArgs: ProcessEventArgs
	{
	    readonly Exception exception;
	    readonly ExceptionType exceptionType;
	    readonly bool isUnhandled;
		
		public Exception Exception {
			get { return exception; }
		}
		
		public ExceptionType ExceptionType {
			get { return exceptionType; }
		}
		
		public bool IsUnhandled {
			get { return isUnhandled; }
		}
		
		public ExceptionEventArgs(Process process, Exception exception, ExceptionType exceptionType, bool isUnhandled):base(process)
		{
			this.exception = exception;
			this.exceptionType = exceptionType;
			this.isUnhandled = isUnhandled;
		}
	}
}
