using System;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast
{
	public class TypeInformation
	{
		public readonly TypeReference InferredType;
		
		public TypeInformation(TypeReference inferredType)
		{
			this.InferredType = inferredType;
		}
	}
}
