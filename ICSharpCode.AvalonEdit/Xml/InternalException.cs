// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Runtime.Serialization;

namespace ICSharpCode.AvalonEdit.Xml
{
	/// <summary>
	/// Exception used for internal errors in XML parser.
	/// This exception indicates a bug in AvalonEdit.
	/// </summary>
	[Serializable()]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Justification = "This exception is not public because it is not supposed to be caught by user code - it indicates a bug in AvalonEdit.")]
	class InternalException : Exception
	{
		/// <summary>
		/// Creates a new InternalException instance.
		/// </summary>
		public InternalException() : base()
		{
		}
		
		/// <summary>
		/// Creates a new InternalException instance.
		/// </summary>
		public InternalException(string message) : base(message)
		{
		}
		
		/// <summary>
		/// Creates a new InternalException instance.
		/// </summary>
		public InternalException(string message, Exception innerException) : base(message, innerException)
		{
		}
		
		/// <summary>
		/// Creates a new InternalException instance.
		/// </summary>
		protected InternalException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
