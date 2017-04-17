using System.Collections;

namespace dnSpy.Debugger {
	abstract class BaseVMComparer<T> : IComparer where T : class {
		public string CurrentProperty { get; set; }

		protected abstract int CompareByProperty(T x, T y, string propertyName);

		public int Compare(object x, object y) {
			if (string.IsNullOrEmpty(CurrentProperty) ||
				ReferenceEquals(x, y))
				return 0;

			var xT = x as T;
			var yT = y as T;

			if (xT == null)
				return 1;
			if (yT == null)
				return -1;

			return CompareByProperty(xT, yT, CurrentProperty);
		}
	}
}
