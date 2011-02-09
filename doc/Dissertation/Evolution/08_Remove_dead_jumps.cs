using System;
abstract class QuickSortProgram
{
    public static void Main(System.String[] args)
    {
        System.Int32[] V_0 = new int[((int)args.Length)];
        int i = 0;
        for (;;) {
            if (!(i < ((int)V_0.Length))) {
                break;
            }
            else {
            }
            V_0[i] = System.Int32.Parse(args[i]);
            i = (i + 1);
        }
        QuickSortProgram.QuickSort(V_0, 0, (((int)V_0.Length) - 1));
        int j = 0;
        for (;;) {
            if (!(j < ((int)V_0.Length))) {
                break;
            }
            else {
            }
            System.Console.Write(System.String.Concat((V_0[j]).ToString(), " "));
            j = (j + 1);
        }
    }
    public static void QuickSort(System.Int32[] array, int left, int right)
    {
        if (!(right <= left)) {
            int i = ((left + right) / 2);
            int j = QuickSortProgram.Partition(array, left, right, i);
            QuickSortProgram.QuickSort(array, left, (j - 1));
            QuickSortProgram.QuickSort(array, (j + 1), right);
        }
        else {
        }
    }
    private static int Partition(System.Int32[] array, int left, int right, int pivotIndex)
    {
        int i = array[pivotIndex];
        QuickSortProgram.Swap(array, pivotIndex, right);
        int j = left;
        int k = left;
        for (;;) {
            if (!(k < right)) {
                break;
            }
            else {
            }
            if (!(array[k] > i)) {
                QuickSortProgram.Swap(array, j, k);
                j = (j + 1);
            }
            else {
            }
            k = (k + 1);
        }
        QuickSortProgram.Swap(array, right, j);
        return j;
    }
    private static void Swap(System.Int32[] array, int index1, int index2)
    {
        int i = array[index1];
        array[index1] = array[index2];
        array[index2] = i;
    }
}
