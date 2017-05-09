using System;
using System.Collections.Generic;

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Base class for view-model property comparers
	/// </summary>
	public abstract class BaseVMComparer<T> : IComparer<T> where T : class {
		/// <summary>
		/// Ordering info used for sorting collections using this comparer
		/// </summary>
		public SortInfo SortInfo { get; set; }

		/// <summary>
		/// Compares two view-model objects by a given property
		/// </summary>
		/// <param name="x">First view-model (not null)</param>
		/// <param name="y">Second view-model (not null)</param>
		/// <param name="propertyName">View-model property name</param>
		protected abstract int CompareByPropertyImpl(T x, T y, string propertyName);

		/// <summary>
		/// Compares two given view-model object using provided SortInfo
		/// </summary>
		public virtual int Compare(T x, T y) {
			if (string.IsNullOrEmpty(SortInfo?.PropertyName))
				return 0;

			int result = CompareByProperty(x, y, SortInfo.PropertyName);
			return SortInfo.Direction == SortDirection.Ascending ? result : -result;
		}

		/// <summary>
		/// Comparer method for nullable objects
		/// </summary>
		protected int CompareNullable<U>(U? x, U? y) where U : struct, IComparable {
			if (x == null && y == null)
				return 0;
			if (x == null)
				return -1;
			if (y == null)
				return 1;

			return x.Value.CompareTo(y.Value);
		}

		private int CompareByProperty(T x, T y, string propertyName) {
			if (string.IsNullOrEmpty(propertyName) ||
				ReferenceEquals(x, y))
				return 0;

			var xT = x as T;
			var yT = y as T;

			if (xT == null)
				return 1;
			if (yT == null)
				return -1;

			return CompareByPropertyImpl(xT, yT, propertyName);
		}
	}
}
