using System;
namespace Reversi
{
    public class Board
    {
        public static int Black;
        public static int Empty;
        public static int White;
        private int blackCount;
        private int whiteCount;
        private int emptyCount;
        private int blackFrontierCount;
        private int whiteFrontierCount;
        private int blackSafeCount;
        private int whiteSafeCount;
        private System.Int32[,] squares;
        private System.Boolean[,] safeDiscs;
        public int BlackCount {
        }
        public int WhiteCount {
        }
        public int EmptyCount {
        }
        public int BlackFrontierCount {
        }
        public int WhiteFrontierCount {
        }
        public int BlackSafeCount {
        }
        public int WhiteSafeCount {
        }
        public void SetForNewGame()
        {
            for (int i = 0; (i < 8); i = (i + 1)) {
                for (int j = 0; (j < 8); j = (j + 1)) {
                    (@this.squares).Set(i, j, (IL__ldsfld(Empty)));
                    (@this.safeDiscs).Set(i, j, 0);
                }
            }
            (@this.squares).Set(3, 3, (IL__ldsfld(White)));
            (@this.squares).Set(3, 4, (IL__ldsfld(Black)));
            (@this.squares).Set(4, 3, (IL__ldsfld(Black)));
            (@this.squares).Set(4, 4, (IL__ldsfld(White)));
            @this.UpdateCounts();
        }
        public int GetSquareContents(int row, int col)
        {
            return (@this.squares).Get(row, col);
        }
        public void MakeMove(int color, int row, int col)
        {
            (@this.squares).Set(row, col, color);
            for (int i = -1; (i <= 1); i = (i + 1)) {
                for (int j = -1; (j <= 1); j = (j + 1)) {
                    if (!(i)) {
                        if (!(!j)) {
                        }
                        else {
                            continue;
                        }
                    }
                    if (!(!@this.IsOutflanking(color, row, col, i, j))) {
                        int k = (row + i);
                        for (int l = (col + j); ((@this.squares).Get(k, l) == -color); l = (l + j)) {
                            (@this.squares).Set(k, l, color);
                            k = (k + i);
                        }
                    }
                }
            }
            @this.UpdateCounts();
        }
        public bool HasAnyValidMove(int color)
        {
            for (int i = 0; (i < 8); i = (i + 1)) {
                for (int j = 0; (j < 8); j = (j + 1)) {
                    if (!(!@this.IsValidMove(color, i, j))) {
                        return 1;
                    }
                }
            }
            return 0;
        }
        public bool IsValidMove(int color, int row, int col)
        {
            if (!((@this.squares).Get(row, col) == (IL__ldsfld(Empty)))) {
                return 0;
            }
            for (int i = -1; (i <= 1); i = (i + 1)) {
                for (int j = -1; (j <= 1); j = (j + 1)) {
                    if (!(i)) {
                        if (!(!j)) {
                        }
                        else {
                            continue;
                        }
                    }
                    if (!(!@this.IsOutflanking(color, row, col, i, j))) {
                        return 1;
                    }
                }
            }
            return 0;
        }
        public int GetValidMoveCount(int color)
        {
            int i = 0;
            for (int j = 0; (j < 8); j = (j + 1)) {
                for (int k = 0; (k < 8); k = (k + 1)) {
                    if (!(!@this.IsValidMove(color, j, k))) {
                        i = (i + 1);
                    }
                }
            }
            return i;
        }
        private bool IsOutflanking(int color, int row, int col, int dr, int dc)
        {
            int i = (row + dr);
            for (int j = (col + dc);;) {
                if (!(i < 0)) {
                    if (!(i >= 8)) {
                        if (!(j < 0)) {
                            if (!(j >= 8)) {
                                if (!((@this.squares).Get(i, j) == -color)) {
                                    break;
                                }
                                i = (i + dr);
                                j = (j + dc);
                            }
                            else {
                                break;
                            }
                        }
                        else {
                            break;
                        }
                    }
                    else {
                        break;
                    }
                }
                else {
                    break;
                }
            }
            if (!(i < 0)) {
                if (!(i > 7)) {
                    if (!(j < 0)) {
                        if (!(j > 7)) {
                            if (!((i - dr) != row)) {
                                if (!((j - dc) == col)) {
                                }
                                else {
                                    goto BasicBlock_202;
                                }
                            }
                            if (!((@this.squares).Get(i, j) == color)) {
                            }
                            else {
                                goto BasicBlock_203;
                            }
                        }
                    }
                }
            }
            BasicBlock_202:
            return 0;
            BasicBlock_203:
            return 1;
        }
        private void UpdateCounts()
        {
            @this.blackCount = 0;
            @this.whiteCount = 0;
            @this.emptyCount = 0;
            @this.blackFrontierCount = 0;
            @this.whiteFrontierCount = 0;
            @this.whiteSafeCount = 0;
            @this.blackSafeCount = 0;
            for (bool V_2 = 1; (V_2);) {
                V_2 = 0;
                for (int i = 0; (i < 8); i = (i + 1)) {
                    for (int j = 0; (j < 8); j = (j + 1)) {
                        if (!((@this.squares).Get(i, j) == (IL__ldsfld(Empty)))) {
                            if (!((@this.safeDiscs).Get(i, j))) {
                                if (!(@this.IsOutflankable(i, j))) {
                                    (@this.safeDiscs).Set(i, j, 1);
                                    V_2 = 1;
                                }
                            }
                        }
                    }
                }
            }
            i = 0;
            for (; (i < 8); i = (i + 1)) {
                j = 0;
                for (; (j < 8); j = (j + 1)) {
                    bool V_5 = 0;
                    if (!((@this.squares).Get(i, j) == (IL__ldsfld(Empty)))) {
                        for (int k = -1; (k <= 1); k = (k + 1)) {
                            for (int l = -1; (l <= 1); l = (l + 1)) {
                                if (!(k)) {
                                    if (!(!l)) {
                                    }
                                    else {
                                        continue;
                                    }
                                }
                                if (!((i + k) < 0)) {
                                    if (!((i + k) >= 8)) {
                                        if (!((j + l) < 0)) {
                                            if (!((j + l) >= 8)) {
                                                if (!((@this.squares).Get((i + k), (j + l)) != (IL__ldsfld(Empty)))) {
                                                    V_5 = 1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!((@this.squares).Get(i, j) != (IL__ldsfld(Black)))) {
                        IL__dup(@this);
                        object expr123 = expr122.blackCount;
                        int expr129 = expr123 + 1;
                        expr122.blackCount = expr129;
                        if (!(!V_5)) {
                            IL__dup(@this);
                            object expr135 = expr134.blackFrontierCount;
                            int expr13B = expr135 + 1;
                            expr134.blackFrontierCount = expr13B;
                        }
                        if (!(!(@this.safeDiscs).Get(i, j))) {
                            IL__dup(@this);
                            object expr152 = expr151.blackSafeCount;
                            int expr158 = expr152 + 1;
                            expr151.blackSafeCount = expr158;
                        }
                    }
                    else {
                        if (!((@this.squares).Get(i, j) != (IL__ldsfld(White)))) {
                            IL__dup(@this);
                            object expr176 = expr175.whiteCount;
                            int expr17C = expr176 + 1;
                            expr175.whiteCount = expr17C;
                            if (!(!V_5)) {
                                IL__dup(@this);
                                object expr188 = expr187.whiteFrontierCount;
                                int expr18E = expr188 + 1;
                                expr187.whiteFrontierCount = expr18E;
                            }
                            if (!(!(@this.safeDiscs).Get(i, j))) {
                                IL__dup(@this);
                                object expr1A5 = expr1A4.whiteSafeCount;
                                int expr1AB = expr1A5 + 1;
                                expr1A4.whiteSafeCount = expr1AB;
                                continue;
                            }
                            else {
                                continue;
                            }
                        }
                        IL__dup(@this);
                        object expr1B5 = expr1B4.emptyCount;
                        int expr1BB = expr1B5 + 1;
                        expr1B4.emptyCount = expr1BB;
                    }
                }
            }
        }
        private bool IsOutflankable(int row, int col)
        {
            int i = (@this.squares).Get(row, col);
            bool V_3 = 0;
            bool V_5 = 0;
            bool V_4 = 0;
            bool V_6 = 0;
            for (int k = 0;;) {
                if (!(k >= col)) {
                    if (!(!V_3)) {
                        break;
                    }
                    if (!((@this.squares).Get(row, k) != (IL__ldsfld(Empty)))) {
                        V_3 = 1;
                    }
                    else {
                        if (!((@this.squares).Get(row, k) != i)) {
                            if (!((@this.safeDiscs).Get(row, k))) {
                            }
                            else {
                                goto BasicBlock_451;
                            }
                        }
                        V_5 = 1;
                    }
                    BasicBlock_451:
                    k = (k + 1);
                }
                else {
                    break;
                }
            }
            k = (col + 1);
            for (;;) {
                if (!(k >= 8)) {
                    if (!(!V_4)) {
                        break;
                    }
                    if (!((@this.squares).Get(row, k) != (IL__ldsfld(Empty)))) {
                        V_4 = 1;
                    }
                    else {
                        if (!((@this.squares).Get(row, k) != i)) {
                            if (!((@this.safeDiscs).Get(row, k))) {
                            }
                            else {
                                goto BasicBlock_462;
                            }
                        }
                        V_6 = 1;
                    }
                    BasicBlock_462:
                    k = (k + 1);
                }
                else {
                    break;
                }
            }
            if (!(!V_3)) {
                if (!(V_4)) {
                }
                else {
                    goto BasicBlock_471;
                }
            }
            if (!(!V_3)) {
                if (!(V_6)) {
                }
                else {
                    goto BasicBlock_471;
                }
            }
            if (!(!V_5)) {
                if (!(!V_4)) {
                    BasicBlock_471:
                    return 1;
                }
            }
            V_3 = 0;
            V_4 = 0;
            V_5 = 0;
            V_6 = 0;
            for (int j = 0;;) {
                if (!(j >= row)) {
                    if (!(!V_3)) {
                        break;
                    }
                    if (!((@this.squares).Get(j, col) != (IL__ldsfld(Empty)))) {
                        V_3 = 1;
                    }
                    else {
                        if (!((@this.squares).Get(j, col) != i)) {
                            if (!((@this.safeDiscs).Get(j, col))) {
                            }
                            else {
                                goto BasicBlock_480;
                            }
                        }
                        V_5 = 1;
                    }
                    BasicBlock_480:
                    j = (j + 1);
                }
                else {
                    break;
                }
            }
            j = (row + 1);
            for (;;) {
                if (!(j >= 8)) {
                    if (!(!V_4)) {
                        break;
                    }
                    if (!((@this.squares).Get(j, col) != (IL__ldsfld(Empty)))) {
                        V_4 = 1;
                    }
                    else {
                        if (!((@this.squares).Get(j, col) != i)) {
                            if (!((@this.safeDiscs).Get(j, col))) {
                            }
                            else {
                                goto BasicBlock_491;
                            }
                        }
                        V_6 = 1;
                    }
                    BasicBlock_491:
                    j = (j + 1);
                }
                else {
                    break;
                }
            }
            if (!(!V_3)) {
                if (!(V_4)) {
                }
                else {
                    goto BasicBlock_500;
                }
            }
            if (!(!V_3)) {
                if (!(V_6)) {
                }
                else {
                    goto BasicBlock_500;
                }
            }
            if (!(!V_5)) {
                if (!(!V_4)) {
                    BasicBlock_500:
                    return 1;
                }
            }
            V_3 = 0;
            V_4 = 0;
            V_5 = 0;
            V_6 = 0;
            j = (row - 1);
            k = (col - 1);
            for (;;) {
                if (!(j < 0)) {
                    if (!(k < 0)) {
                        if (!(!V_3)) {
                            break;
                        }
                        if (!((@this.squares).Get(j, k) != (IL__ldsfld(Empty)))) {
                            V_3 = 1;
                        }
                        else {
                            if (!((@this.squares).Get(j, k) != i)) {
                                if (!((@this.safeDiscs).Get(j, k))) {
                                }
                                else {
                                    goto BasicBlock_509;
                                }
                            }
                            V_5 = 1;
                        }
                        BasicBlock_509:
                        j = (j - 1);
                        k = (k - 1);
                    }
                    else {
                        break;
                    }
                }
                else {
                    break;
                }
            }
            j = (row + 1);
            k = (col + 1);
            for (;;) {
                if (!(j >= 8)) {
                    if (!(k >= 8)) {
                        if (!(!V_4)) {
                            break;
                        }
                        if (!((@this.squares).Get(j, k) != (IL__ldsfld(Empty)))) {
                            V_4 = 1;
                        }
                        else {
                            if (!((@this.squares).Get(j, k) != i)) {
                                if (!((@this.safeDiscs).Get(j, k))) {
                                }
                                else {
                                    goto BasicBlock_521;
                                }
                            }
                            V_6 = 1;
                        }
                        BasicBlock_521:
                        j = (j + 1);
                        k = (k + 1);
                    }
                    else {
                        break;
                    }
                }
                else {
                    break;
                }
            }
            if (!(!V_3)) {
                if (!(V_4)) {
                }
                else {
                    goto BasicBlock_531;
                }
            }
            if (!(!V_3)) {
                if (!(V_6)) {
                }
                else {
                    goto BasicBlock_531;
                }
            }
            if (!(!V_5)) {
                if (!(!V_4)) {
                    BasicBlock_531:
                    return 1;
                }
            }
            V_3 = 0;
            V_4 = 0;
            V_5 = 0;
            V_6 = 0;
            j = (row - 1);
            k = (col + 1);
            for (;;) {
                if (!(j < 0)) {
                    if (!(k >= 8)) {
                        if (!(!V_3)) {
                            break;
                        }
                        if (!((@this.squares).Get(j, k) != (IL__ldsfld(Empty)))) {
                            V_3 = 1;
                        }
                        else {
                            if (!((@this.squares).Get(j, k) != i)) {
                                if (!((@this.safeDiscs).Get(j, k))) {
                                }
                                else {
                                    goto BasicBlock_540;
                                }
                            }
                            V_5 = 1;
                        }
                        BasicBlock_540:
                        j = (j - 1);
                        k = (k + 1);
                    }
                    else {
                        break;
                    }
                }
                else {
                    break;
                }
            }
            j = (row + 1);
            k = (col - 1);
            for (;;) {
                if (!(j >= 8)) {
                    if (!(k < 0)) {
                        if (!(!V_4)) {
                            break;
                        }
                        if (!((@this.squares).Get(j, k) != (IL__ldsfld(Empty)))) {
                            V_4 = 1;
                        }
                        else {
                            if (!((@this.squares).Get(j, k) != i)) {
                                if (!((@this.safeDiscs).Get(j, k))) {
                                }
                                else {
                                    goto BasicBlock_552;
                                }
                            }
                            V_6 = 1;
                        }
                        BasicBlock_552:
                        j = (j + 1);
                        k = (k - 1);
                    }
                    else {
                        break;
                    }
                }
                else {
                    break;
                }
            }
            if (!(!V_3)) {
                if (!(V_4)) {
                }
                else {
                    goto BasicBlock_562;
                }
            }
            if (!(!V_3)) {
                if (!(V_6)) {
                }
                else {
                    goto BasicBlock_562;
                }
            }
            if (!(!V_5)) {
                if (!(!V_4)) {
                    BasicBlock_562:
                    return 1;
                }
            }
            return 0;
        }
    }
}
