#if NETFRAMEWORK
namespace System.Diagnostics.CodeAnalysis {
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	sealed class DoesNotReturnIfAttribute : Attribute {
		public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;
		public bool ParameterValue { get; }
	}
}
#endif
