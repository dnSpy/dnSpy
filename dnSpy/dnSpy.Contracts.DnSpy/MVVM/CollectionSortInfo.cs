namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Sort direction for collection or list view column
	/// </summary>
	public enum SortDirection {
		/// <summary>
		/// Arrange items from smaller to bigger
		/// </summary>
		Ascending,
		/// <summary>
		/// Arrange items from bigger to smaller
		/// </summary>
		Descending
	}

	/// <summary>
	/// Stores sort order and property to sort collection by
	/// </summary>
	public class SortInfo {
		/// <summary>
		/// Collection item property
		/// </summary>
		public string PropertyName { get; set; }
		/// <summary>
		/// Collection sort direction
		/// </summary>
		public SortDirection Direction { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public SortInfo(string propertyName, SortDirection direction) {
			PropertyName = propertyName;
			Direction = direction;
		}
	}
}
