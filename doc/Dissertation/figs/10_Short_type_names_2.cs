using System;
abstract class QuickSortProgram
{
    public static void Main(string[] args)
    {
        int[] V_0 = new int[args.Length];
        for (int i = 0; i < V_0.Length; i++) {
            V_0[i] = Int32.Parse(args[i]);
        }
        QuickSortProgram.QuickSort(V_0, 0, V_0.Length - 1);
        for (int j = 0; j < V_0.Length; j++) {
            Console.Write(V_0[j].ToString() + " ");
        }
    }
    public static void QuickSort(int[] array, int left, int right)
    {
        if (right > left) {
            int i = (left + right) / 2;
            int j = QuickSortProgram.Partition(array, left, right, i);
            QuickSortProgram.QuickSort(array, left, j - 1);
            QuickSortProgram.QuickSort(array, j + 1, right);
        }
    }
    private static int Partition(int[] array, int left, int right, int pivotIndex)
    {
        int i = array[pivotIndex];
        QuickSortProgram.Swap(array, pivotIndex, right);
        int j = left;
        for (int k = left; k < right; k++) {
            if (array[k] <= i) {
                QuickSortProgram.Swap(array, j, k);
                j++;
            }
        }
        QuickSortProgram.Swap(array, right, j);
        return j;
    }
    private static void Swap(int[] array, int index1, int index2)
    {
        int i = array[index1];
        array[index1] = array[index2];
        array[index2] = i;
    }
}
