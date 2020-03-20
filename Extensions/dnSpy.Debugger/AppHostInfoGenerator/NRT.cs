#if NETFRAMEWORK
namespace System.Diagnostics.CodeAnalysis {
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	sealed class NotNullWhenAttribute : Attribute {
		public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
		public bool ReturnValue { get; }
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
