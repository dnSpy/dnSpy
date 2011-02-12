// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpDevelop.Dom.ReflectionLayer
{
	/// <summary>
	/// Thrown if there is a syntax error in a type name.
	/// </summary>
	[Serializable]
	public class ReflectionTypeNameSyntaxError : Exception
	{
		public ReflectionTypeNameSyntaxError() : base()
		{
		}
		
		public ReflectionTypeNameSyntaxError(string message) : base(message)
		{
		}
		
		public ReflectionTypeNameSyntaxError(string message, Exception innerException) : base(message, innerException)
		{
		}
		
		protected ReflectionTypeNameSyntaxError(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
