#if NETFRAMEWORK
namespace System.Diagnostics.CodeAnalysis {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	public sealed class NotNullWhenAttribute : Attribute {
		public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
		public bool ReturnValue { get; }
	}
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	public sealed class NotNullIfNotNullAttribute : Attribute {
		public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;
		public string ParameterName { get; }
	}
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
	public sealed class MaybeNullAttribute : Attribute {
		public MaybeNullAttribute() { }
	}
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	public sealed class DoesNotReturnIfAttribute : Attribute {
		public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;
		public bool ParameterValue { get; }
	}
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	public sealed class AllowNullAttribute : Attribute {
		public AllowNullAttribute() { }
	}
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	public sealed class DisallowNullAttribute : Attribute {
		public DisallowNullAttribute() { }
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
#endif
