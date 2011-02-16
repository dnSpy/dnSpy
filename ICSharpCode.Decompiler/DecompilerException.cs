// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Runtime.Serialization;
using Mono.Cecil;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Desctiption of DecompilerException.
	/// </summary>
	public class DecompilerException : Exception, ISerializable
	{
		public MethodDefinition DecompiledMethod { get; set; }
		
		public DecompilerException(MethodDefinition decompiledMethod, Exception innerException) 
			: base("Error decompiling " + decompiledMethod.FullName, innerException)
		{
		}

		// This constructor is needed for serialization.
		protected DecompilerException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}