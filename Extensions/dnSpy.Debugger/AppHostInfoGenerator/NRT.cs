namespace System.Runtime.CompilerServices {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public sealed class NotNullWhenTrueAttribute : Attribute {
		public NotNullWhenTrueAttribute() { }
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
