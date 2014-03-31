using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.ILAst
{
	class GenericTypeParam : GenericVar, IGenericParam
	{
		public GenericTypeParam(GenericParam param)
			: base(param.Number)
		{
			GenericParameter = param;
		}

		public GenericParam GenericParameter { get; private set; }
	}

	class GenericMethodParam : GenericMVar, IGenericParam
	{
		public GenericMethodParam(GenericParam param)
			: base(param.Number)
		{
			GenericParameter = param;
		}

		public GenericParam GenericParameter { get; private set; }
	}

	interface IGenericParam
	{
		GenericParam GenericParameter { get; }
	}
}
