using dnlib.DotNet;

namespace ICSharpCode.Decompiler
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

	public interface IGenericParam
	{
		GenericParam GenericParameter { get; }
	}
}
