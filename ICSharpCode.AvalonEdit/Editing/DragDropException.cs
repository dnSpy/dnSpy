// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Runtime.Serialization;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Wraps exceptions that occur during drag'n'drop.
	/// Exceptions during drag'n'drop might
	/// get swallowed by WPF/COM, so AvalonEdit catches them and re-throws them later
	/// wrapped in a DragDropException.
	/// </summary>
	[Serializable()]
	public class DragDropException : Exception
	{
		/// <summary>
		/// Creates a new DragDropException.
		/// </summary>
		public DragDropException() : base()
		{
		}
		
		/// <summary>
		/// Creates a new DragDropException.
		/// </summary>
		public DragDropException(string message) : base(message)
		{
		}
		
		/// <summary>
		/// Creates a new DragDropException.
		/// </summary>
		public DragDropException(string message, Exception innerException) : base(message, innerException)
		{
		}
		
		/// <summary>
		/// Deserializes a DragDropException.
		/// </summary>
		protected DragDropException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
