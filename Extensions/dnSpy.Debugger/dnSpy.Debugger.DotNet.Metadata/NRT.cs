#if NETFRAMEWORK
namespace System.Diagnostics.CodeAnalysis {
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	sealed class NotNullWhenAttribute : Attribute {
		public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
		public bool ReturnValue { get; }
	}
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	sealed class DoesNotReturnIfAttribute : Attribute {
		public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;
		public bool ParameterValue { get; }
	}
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	sealed class AllowNullAttribute : Attribute {
		public AllowNullAttribute() { }
	}
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	sealed class DisallowNullAttribute : Attribute {
		public DisallowNullAttribute() { }
	}
}
#endif
