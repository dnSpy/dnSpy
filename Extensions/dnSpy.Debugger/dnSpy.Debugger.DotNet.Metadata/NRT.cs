namespace System.Runtime.CompilerServices {
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	sealed class EnsuresNotNullAttribute : Attribute {
		public EnsuresNotNullAttribute() { }
	}
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	sealed class NotNullWhenTrueAttribute : Attribute {
		public NotNullWhenTrueAttribute() { }
	}
}
