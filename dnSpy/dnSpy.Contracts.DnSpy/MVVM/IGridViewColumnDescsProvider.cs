/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Provides the column descs
	/// </summary>
	public interface IGridViewColumnDescsProvider {
		/// <summary>
		/// Gets the column descriptions list. The UI and the VM get notified when a column is selected (clicked).
		/// </summary>
		GridViewColumnDescs Descs { get; }
	}

	/// <summary>
	/// Contains all column descs. Notifies listeners (UI and VM) when a column is selected and sort direction is updated.
	/// </summary>
	public sealed class GridViewColumnDescs {
		/// <summary>
		/// All columns in UI order. Gets updated by the UI when the user drags a column.
		/// </summary>
		public GridViewColumnDesc[] Columns { get; set; } = Array.Empty<GridViewColumnDesc>();

		/// <summary>
		/// Currently selected column and sort direction
		/// </summary>
		public GridViewSortedColumn SortedColumn {
			get => sortedColumn;
			set {
				if (value.Column != sortedColumn.Column || value.Direction != sortedColumn.Direction) {
					sortedColumn = value;
					SortedColumnChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		GridViewSortedColumn sortedColumn;

		/// <summary>
		/// Raised when <see cref="SortedColumn"/> is changed
		/// </summary>
		public event EventHandler? SortedColumnChanged;
	}

	/// <summary>
	/// Contains the active column and sort direction
	/// </summary>
	public readonly struct GridViewSortedColumn {
		/// <summary>
		/// Column or null to use default sort order
		/// </summary>
		public readonly GridViewColumnDesc? Column;

		/// <summary>
		/// Sort direction
		/// </summary>
		public readonly GridViewSortDirection Direction;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="column">Column or null to use default sort order</param>
		/// <param name="direction">Sort direction</param>
		public GridViewSortedColumn(GridViewColumnDesc? column, GridViewSortDirection direction) {
			Column = column;
			Direction = direction;
		}

		/// <summary>
		/// Deconstruct
		/// </summary>
		/// <param name="column"></param>
		/// <param name="direction"></param>
		public void Deconstruct(out GridViewColumnDesc? column, out GridViewSortDirection direction) {
			column = Column;
			direction = Direction;
		}
	}

	/// <summary>
	/// Grid view column info needed by UI and VM
	/// </summary>
	public sealed class GridViewColumnDesc {
		/// <summary>
		/// A unique ID. No other coulumn in this grid view can have the same id.
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// Name shown in the UI or an empty string
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// true if the user can sort this column
		/// </summary>
		public bool CanBeSorted { get; set; }

		/// <summary>
		/// true if the column is visible in the UI
		/// </summary>
		public bool IsVisible => true;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id">A unique ID. No other coulumn in this grid view can have the same id.</param>
		/// <param name="name">Name shown in the UI or an empty string</param>
		public GridViewColumnDesc(int id, string name) {
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			CanBeSorted = name != string.Empty;
		}
	}

	/// <summary>
	/// Sort direction
	/// </summary>
	public enum GridViewSortDirection {
		/// <summary>
		/// Default order
		/// </summary>
		Default,

		/// <summary>
		/// Ascending order
		/// </summary>
		Ascending,

		/// <summary>
		/// Descending order
		/// </summary>
		Descending,
	}
}
