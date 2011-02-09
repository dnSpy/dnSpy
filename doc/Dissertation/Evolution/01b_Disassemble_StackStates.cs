namespace 
{
    abstract class QuickSortProgram
    {
        public static void Main(System.String[] args)
        {
            IL_00: nop                    # Pop0->Push0  
            // Stack: {}
            IL_01: ldarg args             # Pop0->Push1  
            // Stack: {IL_01}
            IL_02: ldlen                  # Popref->Pushi  
            // Stack: {IL_02}
            IL_03: conv.i4                # Pop1->Pushi  
            // Stack: {IL_03}
            IL_04: newarr System.Int32    # Popi->Pushref  
            // Stack: {IL_04}
            IL_09: stloc V_0              # Pop1->Push0  
            // Stack: {}
            IL_0A: ldc.i4 0               # Pop0->Pushi  
            // Stack: {IL_0A}
            IL_0B: stloc V_1              # Pop1->Push0  
            // Stack: {}
            IL_0C: br IL_1F               # Pop0->Push0 Flow=Branch 
            // Stack: {}
            IL_0E: nop                    # Pop0->Push0  
            // Stack: {}
            IL_0F: ldloc V_0              # Pop0->Push1  
            // Stack: {IL_0F}
            IL_10: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_0F, IL_10}
            IL_11: ldarg args             # Pop0->Push1  
            // Stack: {IL_0F, IL_10, IL_11}
            IL_12: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_0F, IL_10, IL_11, IL_12}
            IL_13: ldelem.ref             # Popref_popi->Pushref  
            // Stack: {IL_0F, IL_10, IL_13}
            IL_14: call Parse()           # Varpop->Varpush Flow=Call 
            // Stack: {IL_0F, IL_10, IL_14}
            IL_19: stelem.i4              # Popref_popi_popi->Push0  
            // Stack: {}
            IL_1A: nop                    # Pop0->Push0  
            // Stack: {}
            IL_1B: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_1B}
            IL_1C: ldc.i4 1               # Pop0->Pushi  
            // Stack: {IL_1B, IL_1C}
            IL_1D: add.ovf                # Pop1_pop1->Push1  
            // Stack: {IL_1D}
            IL_1E: stloc V_1              # Pop1->Push0  
            // Stack: {}
            IL_1F: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_1F}
            IL_20: ldloc V_0              # Pop0->Push1  
            // Stack: {IL_1F, IL_20}
            IL_21: ldlen                  # Popref->Pushi  
            // Stack: {IL_1F, IL_21}
            IL_22: conv.i4                # Pop1->Pushi  
            // Stack: {IL_1F, IL_22}
            IL_23: clt                    # Pop1_pop1->Pushi  
            // Stack: {IL_23}
            IL_25: stloc V_2              # Pop1->Push0  
            // Stack: {}
            IL_26: ldloc V_2              # Pop0->Push1  
            // Stack: {IL_26}
            IL_27: brtrue IL_0E           # Popi->Push0 Flow=Cond_Branch 
            // Stack: {}
            IL_29: ldloc V_0              # Pop0->Push1  
            // Stack: {IL_29}
            IL_2A: ldc.i4 0               # Pop0->Pushi  
            // Stack: {IL_29, IL_2A}
            IL_2B: ldloc V_0              # Pop0->Push1  
            // Stack: {IL_29, IL_2A, IL_2B}
            IL_2C: ldlen                  # Popref->Pushi  
            // Stack: {IL_29, IL_2A, IL_2C}
            IL_2D: conv.i4                # Pop1->Pushi  
            // Stack: {IL_29, IL_2A, IL_2D}
            IL_2E: ldc.i4 1               # Pop0->Pushi  
            // Stack: {IL_29, IL_2A, IL_2D, IL_2E}
            IL_2F: sub.ovf                # Pop1_pop1->Push1  
            // Stack: {IL_29, IL_2A, IL_2F}
            IL_30: call QuickSort()       # Varpop->Varpush Flow=Call 
            // Stack: {}
            IL_35: nop                    # Pop0->Push0  
            // Stack: {}
            IL_36: ldc.i4 0               # Pop0->Pushi  
            // Stack: {IL_36}
            IL_37: stloc V_1              # Pop1->Push0  
            // Stack: {}
            IL_38: br IL_5C               # Pop0->Push0 Flow=Branch 
            // Stack: {}
            IL_3A: nop                    # Pop0->Push0  
            // Stack: {}
            IL_3B: ldloc V_0              # Pop0->Push1  
            // Stack: {IL_3B}
            IL_3C: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_3B, IL_3C}
            IL_3D: ldelema System.Int32   # Popref_popi->Pushi  
            // Stack: {IL_3D}
            IL_42: call ToString()        # Varpop->Varpush Flow=Call 
            // Stack: {IL_42}
            IL_47: ldstr \" \"              # Pop0->Pushref  
            // Stack: {IL_42, IL_47}
            IL_4C: call Concat()          # Varpop->Varpush Flow=Call 
            // Stack: {IL_4C}
            IL_51: call Write()           # Varpop->Varpush Flow=Call 
            // Stack: {}
            IL_56: nop                    # Pop0->Push0  
            // Stack: {}
            IL_57: nop                    # Pop0->Push0  
            // Stack: {}
            IL_58: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_58}
            IL_59: ldc.i4 1               # Pop0->Pushi  
            // Stack: {IL_58, IL_59}
            IL_5A: add.ovf                # Pop1_pop1->Push1  
            // Stack: {IL_5A}
            IL_5B: stloc V_1              # Pop1->Push0  
            // Stack: {}
            IL_5C: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_5C}
            IL_5D: ldloc V_0              # Pop0->Push1  
            // Stack: {IL_5C, IL_5D}
            IL_5E: ldlen                  # Popref->Pushi  
            // Stack: {IL_5C, IL_5E}
            IL_5F: conv.i4                # Pop1->Pushi  
            // Stack: {IL_5C, IL_5F}
            IL_60: clt                    # Pop1_pop1->Pushi  
            // Stack: {IL_60}
            IL_62: stloc V_2              # Pop1->Push0  
            // Stack: {}
            IL_63: ldloc V_2              # Pop0->Push1  
            // Stack: {IL_63}
            IL_64: brtrue IL_3A           # Popi->Push0 Flow=Cond_Branch 
            // Stack: {}
            IL_66: ret                    # Varpop->Push0 Flow=Return 
            // Stack: {}
        }
        public static void QuickSort(System.Int32[] array, int left, int right)
        {
            IL_00: nop                    # Pop0->Push0  
            // Stack: {}
            IL_01: ldarg right            # Pop0->Push1  
            // Stack: {IL_01}
            IL_02: ldarg left             # Pop0->Push1  
            // Stack: {IL_01, IL_02}
            IL_03: cgt                    # Pop1_pop1->Pushi  
            // Stack: {IL_03}
            IL_05: ldc.i4 0               # Pop0->Pushi  
            // Stack: {IL_03, IL_05}
            IL_06: ceq                    # Pop1_pop1->Pushi  
            // Stack: {IL_06}
            IL_08: stloc V_2              # Pop1->Push0  
            // Stack: {}
            IL_09: ldloc V_2              # Pop0->Push1  
            // Stack: {IL_09}
            IL_0A: brtrue IL_34           # Popi->Push0 Flow=Cond_Branch 
            // Stack: {}
            IL_0C: nop                    # Pop0->Push0  
            // Stack: {}
            IL_0D: ldarg left             # Pop0->Push1  
            // Stack: {IL_0D}
            IL_0E: ldarg right            # Pop0->Push1  
            // Stack: {IL_0D, IL_0E}
            IL_0F: add.ovf                # Pop1_pop1->Push1  
            // Stack: {IL_0F}
            IL_10: ldc.i4 2               # Pop0->Pushi  
            // Stack: {IL_0F, IL_10}
            IL_11: div                    # Pop1_pop1->Push1  
            // Stack: {IL_11}
            IL_12: stloc V_0              # Pop1->Push0  
            // Stack: {}
            IL_13: ldarg array            # Pop0->Push1  
            // Stack: {IL_13}
            IL_14: ldarg left             # Pop0->Push1  
            // Stack: {IL_13, IL_14}
            IL_15: ldarg right            # Pop0->Push1  
            // Stack: {IL_13, IL_14, IL_15}
            IL_16: ldloc V_0              # Pop0->Push1  
            // Stack: {IL_13, IL_14, IL_15, IL_16}
            IL_17: call Partition()       # Varpop->Varpush Flow=Call 
            // Stack: {IL_17}
            IL_1C: stloc V_1              # Pop1->Push0  
            // Stack: {}
            IL_1D: ldarg array            # Pop0->Push1  
            // Stack: {IL_1D}
            IL_1E: ldarg left             # Pop0->Push1  
            // Stack: {IL_1D, IL_1E}
            IL_1F: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_1D, IL_1E, IL_1F}
            IL_20: ldc.i4 1               # Pop0->Pushi  
            // Stack: {IL_1D, IL_1E, IL_1F, IL_20}
            IL_21: sub.ovf                # Pop1_pop1->Push1  
            // Stack: {IL_1D, IL_1E, IL_21}
            IL_22: call QuickSort()       # Varpop->Varpush Flow=Call 
            // Stack: {}
            IL_27: nop                    # Pop0->Push0  
            // Stack: {}
            IL_28: ldarg array            # Pop0->Push1  
            // Stack: {IL_28}
            IL_29: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_28, IL_29}
            IL_2A: ldc.i4 1               # Pop0->Pushi  
            // Stack: {IL_28, IL_29, IL_2A}
            IL_2B: add.ovf                # Pop1_pop1->Push1  
            // Stack: {IL_28, IL_2B}
            IL_2C: ldarg right            # Pop0->Push1  
            // Stack: {IL_28, IL_2B, IL_2C}
            IL_2D: call QuickSort()       # Varpop->Varpush Flow=Call 
            // Stack: {}
            IL_32: nop                    # Pop0->Push0  
            // Stack: {}
            IL_33: nop                    # Pop0->Push0  
            // Stack: {}
            IL_34: ret                    # Varpop->Push0 Flow=Return 
            // Stack: {}
        }
        private static int Partition(System.Int32[] array, int left, int right, int pivotIndex)
        {
            IL_00: nop                    # Pop0->Push0  
            // Stack: {}
            IL_01: ldarg array            # Pop0->Push1  
            // Stack: {IL_01}
            IL_02: ldarg pivotIndex       # Pop0->Push1  
            // Stack: {IL_01, IL_02}
            IL_03: ldelem.i4              # Popref_popi->Pushi  
            // Stack: {IL_03}
            IL_04: stloc V_0              # Pop1->Push0  
            // Stack: {}
            IL_05: ldarg array            # Pop0->Push1  
            // Stack: {IL_05}
            IL_06: ldarg pivotIndex       # Pop0->Push1  
            // Stack: {IL_05, IL_06}
            IL_07: ldarg right            # Pop0->Push1  
            // Stack: {IL_05, IL_06, IL_07}
            IL_08: call Swap()            # Varpop->Varpush Flow=Call 
            // Stack: {}
            IL_0D: nop                    # Pop0->Push0  
            // Stack: {}
            IL_0E: ldarg left             # Pop0->Push1  
            // Stack: {IL_0E}
            IL_0F: stloc V_1              # Pop1->Push0  
            // Stack: {}
            IL_10: ldarg left             # Pop0->Push1  
            // Stack: {IL_10}
            IL_11: stloc V_2              # Pop1->Push0  
            // Stack: {}
            IL_12: br IL_35               # Pop0->Push0 Flow=Branch 
            // Stack: {}
            IL_14: nop                    # Pop0->Push0  
            // Stack: {}
            IL_15: ldarg array            # Pop0->Push1  
            // Stack: {IL_15}
            IL_16: ldloc V_2              # Pop0->Push1  
            // Stack: {IL_15, IL_16}
            IL_17: ldelem.i4              # Popref_popi->Pushi  
            // Stack: {IL_17}
            IL_18: ldloc V_0              # Pop0->Push1  
            // Stack: {IL_17, IL_18}
            IL_19: cgt                    # Pop1_pop1->Pushi  
            // Stack: {IL_19}
            IL_1B: stloc V_4              # Pop1->Push0  
            // Stack: {}
            IL_1D: ldloc V_4              # Pop0->Push1  
            // Stack: {IL_1D}
            IL_1F: brtrue IL_30           # Popi->Push0 Flow=Cond_Branch 
            // Stack: {}
            IL_21: nop                    # Pop0->Push0  
            // Stack: {}
            IL_22: ldarg array            # Pop0->Push1  
            // Stack: {IL_22}
            IL_23: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_22, IL_23}
            IL_24: ldloc V_2              # Pop0->Push1  
            // Stack: {IL_22, IL_23, IL_24}
            IL_25: call Swap()            # Varpop->Varpush Flow=Call 
            // Stack: {}
            IL_2A: nop                    # Pop0->Push0  
            // Stack: {}
            IL_2B: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_2B}
            IL_2C: ldc.i4 1               # Pop0->Pushi  
            // Stack: {IL_2B, IL_2C}
            IL_2D: add.ovf                # Pop1_pop1->Push1  
            // Stack: {IL_2D}
            IL_2E: stloc V_1              # Pop1->Push0  
            // Stack: {}
            IL_2F: nop                    # Pop0->Push0  
            // Stack: {}
            IL_30: nop                    # Pop0->Push0  
            // Stack: {}
            IL_31: ldloc V_2              # Pop0->Push1  
            // Stack: {IL_31}
            IL_32: ldc.i4 1               # Pop0->Pushi  
            // Stack: {IL_31, IL_32}
            IL_33: add.ovf                # Pop1_pop1->Push1  
            // Stack: {IL_33}
            IL_34: stloc V_2              # Pop1->Push0  
            // Stack: {}
            IL_35: ldloc V_2              # Pop0->Push1  
            // Stack: {IL_35}
            IL_36: ldarg right            # Pop0->Push1  
            // Stack: {IL_35, IL_36}
            IL_37: clt                    # Pop1_pop1->Pushi  
            // Stack: {IL_37}
            IL_39: stloc V_4              # Pop1->Push0  
            // Stack: {}
            IL_3B: ldloc V_4              # Pop0->Push1  
            // Stack: {IL_3B}
            IL_3D: brtrue IL_14           # Popi->Push0 Flow=Cond_Branch 
            // Stack: {}
            IL_3F: ldarg array            # Pop0->Push1  
            // Stack: {IL_3F}
            IL_40: ldarg right            # Pop0->Push1  
            // Stack: {IL_3F, IL_40}
            IL_41: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_3F, IL_40, IL_41}
            IL_42: call Swap()            # Varpop->Varpush Flow=Call 
            // Stack: {}
            IL_47: nop                    # Pop0->Push0  
            // Stack: {}
            IL_48: ldloc V_1              # Pop0->Push1  
            // Stack: {IL_48}
            IL_49: stloc V_3              # Pop1->Push0  
            // Stack: {}
            IL_4A: br IL_4C               # Pop0->Push0 Flow=Branch 
            // Stack: {}
            IL_4C: ldloc V_3              # Pop0->Push1  
            // Stack: {IL_4C}
            IL_4D: ret                    # Varpop->Push0 Flow=Return 
            // Stack: {}
        }
        private static void Swap(System.Int32[] array, int index1, int index2)
        {
            IL_00: nop                    # Pop0->Push0  
            // Stack: {}
            IL_01: ldarg array            # Pop0->Push1  
            // Stack: {IL_01}
            IL_02: ldarg index1           # Pop0->Push1  
            // Stack: {IL_01, IL_02}
            IL_03: ldelem.i4              # Popref_popi->Pushi  
            // Stack: {IL_03}
            IL_04: stloc V_0              # Pop1->Push0  
            // Stack: {}
            IL_05: ldarg array            # Pop0->Push1  
            // Stack: {IL_05}
            IL_06: ldarg index1           # Pop0->Push1  
            // Stack: {IL_05, IL_06}
            IL_07: ldarg array            # Pop0->Push1  
            // Stack: {IL_05, IL_06, IL_07}
            IL_08: ldarg index2           # Pop0->Push1  
            // Stack: {IL_05, IL_06, IL_07, IL_08}
            IL_09: ldelem.i4              # Popref_popi->Pushi  
            // Stack: {IL_05, IL_06, IL_09}
            IL_0A: stelem.i4              # Popref_popi_popi->Push0  
            // Stack: {}
            IL_0B: ldarg array            # Pop0->Push1  
            // Stack: {IL_0B}
            IL_0C: ldarg index2           # Pop0->Push1  
            // Stack: {IL_0B, IL_0C}
            IL_0D: ldloc V_0              # Pop0->Push1  
            // Stack: {IL_0B, IL_0C, IL_0D}
            IL_0E: stelem.i4              # Popref_popi_popi->Push0  
            // Stack: {}
            IL_0F: ret                    # Varpop->Push0 Flow=Return 
            // Stack: {}
        }
    }
}