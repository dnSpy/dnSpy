// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Debugger
{
	/// <summary>
	/// Specifies the range of valid indicies for all array dimensions
	/// </summary>
	public class ArrayDimensions: IEnumerable<ArrayDimension>
	{
		List<ArrayDimension> dimensions = new List<ArrayDimension>();
		
		public IEnumerator<ArrayDimension> GetEnumerator()
		{
			return dimensions.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)dimensions).GetEnumerator();
		}
		
		/// <summary> Gets a given dimension </summary>
		public ArrayDimension this[int index] {
			get {
				return dimensions[index];
			}
		}
		
		/// <summary> Get the number of dimensions of the array </summary>
		public int Count {
			get {
				return dimensions.Count;
			}
		}
		
		/// <summary> Get the total number of elements within the bounds
		/// of an array specified by these dimensions. </summary>
		public int TotalElementCount {
			get {
				int totalCount = 1;
				foreach(ArrayDimension dim in this) {
					totalCount *= dim.Count;
				}
				return totalCount;
			}
		}
		
		/// <summary> Enumerate all vaild indicies in the array </summary>
		public IEnumerable<int[]> Indices {
			get {
				foreach(ArrayDimension dim in this) {
					if (dim.Count == 0) yield break;
				}
				
				int rank = this.Count;
				int[] indices = new int[rank];
				for(int i = 0; i < rank; i++) {
					indices[i] = this[i].LowerBound;
				}
				
				while(true) { // Go thought all combinations
					for (int i = rank - 1; i >= 1; i--) {
						if (indices[i] > this[i].UpperBound) {
							indices[i] = this[i].LowerBound;
							indices[i - 1]++;
						}
					}
					if (indices[0] > this[0].UpperBound) yield break; // We are done
					
					yield return (int[])indices.Clone();
					
					indices[rank - 1]++;
				}
			}
		}
		
		/// <summary> Determines whether the given index is a valid index for the array </summary>
		public bool IsIndexValid(int[] indices)
		{
			for (int i = 0; i < this.Count; i++) {
				if (!this[i].IsIndexValid(indices[i])) return false;
			}
			return true;
		}
		
		public ArrayDimensions(List<ArrayDimension> dimensions)
		{
			this.dimensions = dimensions;
		}
		
		public override string ToString()
		{
			string result = "[";
			bool isFirst = true;
			foreach(ArrayDimension dim in this) {
				if (!isFirst) result += ", ";
				result += dim.ToString();
				isFirst = false;
			}
			result += "]";
			return result;
		}
	}
}
