using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	static class Helpers
	{
		public static bool IsReferencedBy(TypeDefinition type, TypeReference typeRef)
		{
			// TODO: move it to a better place after adding support for more cases.
			if (type == null)
				throw new ArgumentNullException("type");
			if (typeRef == null)
				throw new ArgumentNullException("typeRef");

			if (type == typeRef)
				return true;
			if (type.Name != typeRef.Name)
				return false;
			if (type.Namespace != typeRef.Namespace)
				return false;

			if (type.DeclaringType != null || typeRef.DeclaringType != null) {
				if (type.DeclaringType == null || typeRef.DeclaringType == null)
					return false;
				if (!IsReferencedBy(type.DeclaringType, typeRef.DeclaringType))
					return false;
			}

			return true;
		}
	}
}
