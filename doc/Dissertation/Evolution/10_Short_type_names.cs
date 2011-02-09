using System;
abstract class QuickSortProgram
{
    public static void Main(string[] args)
    {
        int[] V_0 = new int[((int)args.Length)];
        for (int i = 0; (i < ((int)V_0.Length)); i = (i + 1)) {
            V_0[i] = Int32.Parse(args[i]);
        }
        QuickSort(V_0, 0, (((int)V_0.Length) - 1));
        for (int j = 0; (j < ((int)V_0.Length)); j = (j + 1)) {
            Console.Write((V_0[j]).ToString() + " ");
        }
    }
    public static void QuickSort(int[] array, int left, int right)
    {
        if (!(right <= left)) {
            int i = ((left + right) / 2);
            int j = Partition(array, left, right, i);
            QuickSort(array, left, (j - 1));
            QuickSort(array, (j + 1), right);
        }
    }
    private static int Partition(int[] array, int left, int right, int pivotIndex)
    {
        int i = array[pivotIndex];
        Swap(array, pivotIndex, right);
        int j = left;
        for (int k = left; (k < right); k = (k + 1)) {
            if (!(array[k] > i)) {
                Swap(array, j, k);
                j = (j + 1);
            }
        }
        Swap(array, right, j);
        return j;
    }
    private static void Swap(int[] array, int index1, int index2)
    {
        int i = array[index1];
        array[index1] = array[index2];
        array[index2] = i;
    }
}
