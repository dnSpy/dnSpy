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
            get { return blackCount; }
        }
        public int WhiteCount {
            get { return whiteCount; }
        }
        public int EmptyCount {
            get { return emptyCount; }
        }
        public int BlackFrontierCount {
            get { return blackFrontierCount; }
        }
        public int WhiteFrontierCount {
            get { return whiteFrontierCount; }
        }
        public int BlackSafeCount {
            get { return blackSafeCount; }
        }
        public int WhiteSafeCount {
            get { return whiteSafeCount; }
        }

        public void SetForNewGame()
        {
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    squares[i, j] = Empty;
                    safeDiscs[i, j] = 0;
                }
            }
            squares[3, 3] = White;
            squares[3, 4] = Black;
            squares[4, 3] = Black;
            squares[4, 4] = White;
            UpdateCounts();
        }

        public int GetSquareContents(int row, int col)
        {
            return squares[row, col];
        }

        public void MakeMove(int color, int row, int col)
        {
            squares[row, col] = color;
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if ((i || j) && IsOutflanking(color, row, col, i, j)) {
                        int k = row + i;
                        for (int l = col + j; squares[k, l] == -color; l += j) {
                            squares[k, l] = color;
                            k += i;
                        }
                    }
                }
            }
            UpdateCounts();
        }

        public bool HasAnyValidMove(int color)
        {
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    if (IsValidMove(color, i, j)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsValidMove(int color, int row, int col)
        {
            if (squares[row, col] != Empty) {
                return false;
            }
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    if ((i || j) && IsOutflanking(color, row, col, i, j)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public int GetValidMoveCount(int color)
        {
            int i = 0;
            for (int j = 0; j < 8; j++) {
                for (int k = 0; k < 8; k++) {
                    if (IsValidMove(color, j, k)) {
                        i++;
                    }
                }
            }
            return i;
        }

        private bool IsOutflanking(int color, int row, int col, int dr, int dc)
        {
            int i = row + dr;
            for (int j = col + dc; i >= 0 && i < 8 && j >= 0 && j < 8 && squares[i, j] == -color; j += dc) {
                i += dr;
            }
            if (i < 0 || i > 7 || j < 0 || j > 7 || i - dr == row && j - dc == col || squares[i, j] != color) {
                return false;
            }
            return true;
        }

        private void UpdateCounts()
        {
            blackCount = 0;
            whiteCount = 0;
            emptyCount = 0;
            blackFrontierCount = 0;
            whiteFrontierCount = 0;
            whiteSafeCount = 0;
            blackSafeCount = 0;
            for (bool flag = true; flag;) {
                flag = false;
                for (int i = 0; i < 8; i++) {
                    for (int j = 0; j < 8; j++) {
                        if (squares[i, j] != Empty && !safeDiscs[i, j] && !IsOutflankable(i, j)) {
                            safeDiscs[i, j] = 1;
                            flag = true;
                        }
                    }
                }
            }
            i = 0;
            for (; i < 8; i++) {
                j = 0;
                for (; j < 8; j++) {
                    bool flag2 = false;
                    if (squares[i, j] != Empty) {
                        for (int k = -1; k <= 1; k++) {
                            for (int l = -1; l <= 1; l++) {
                                if ((k || l) && i + k >= 0 && i + k < 8 && j + l >= 0 && j + l < 8 && squares[(i + k), (j + l)] == Empty) {
                                    flag2 = true;
                                }
                            }
                        }
                    }
                    if (squares[i, j] == Black) {
                        IL__dup(this);
                        int expr123 = expr122.blackCount;
                        int expr129 = expr123 + 1;
                        expr122.blackCount = expr129;
                        if (flag2) {
                            IL__dup(this);
                            int expr135 = expr134.blackFrontierCount;
                            int expr13B = expr135 + 1;
                            expr134.blackFrontierCount = expr13B;
                        }
                        if (safeDiscs[i, j]) {
                            IL__dup(this);
                            int expr152 = expr151.blackSafeCount;
                            int expr158 = expr152 + 1;
                            expr151.blackSafeCount = expr158;
                        }
                    } else {
                        if (squares[i, j] == White) {
                            IL__dup(this);
                            int expr176 = expr175.whiteCount;
                            int expr17C = expr176 + 1;
                            expr175.whiteCount = expr17C;
                            if (flag2) {
                                IL__dup(this);
                                int expr188 = expr187.whiteFrontierCount;
                                int expr18E = expr188 + 1;
                                expr187.whiteFrontierCount = expr18E;
                            }
                            if (safeDiscs[i, j]) {
                                IL__dup(this);
                                int expr1A5 = expr1A4.whiteSafeCount;
                                int expr1AB = expr1A5 + 1;
                                expr1A4.whiteSafeCount = expr1AB;
                                goto BasicBlock_327;
                            } else {
                                goto BasicBlock_327;
                            }
                        }
                        IL__dup(this);
                        int expr1B5 = expr1B4.emptyCount;
                        int expr1BB = expr1B5 + 1;
                        expr1B4.emptyCount = expr1BB;
                    }
                    BasicBlock_327:
                }
            }
        }

        private bool IsOutflankable(int row, int col)
        {
            int i = squares[row, col];
            bool flag = false;
            bool flag3 = false;
            bool flag2 = false;
            bool flag4 = false;
            for (int k = 0; k < col && !flag; k++) {
                if (squares[row, k] == Empty) {
                    flag = true;
                } else {
                    if (squares[row, k] != i || !safeDiscs[row, k]) {
                        flag3 = true;
                    }
                }
            }
            k = col + 1;
            for (; k < 8 && !flag2; k++) {
                if (squares[row, k] == Empty) {
                    flag2 = true;
                } else {
                    if (squares[row, k] != i || !safeDiscs[row, k]) {
                        flag4 = true;
                    }
                }
            }
            if (flag && flag2 || flag && flag4 || flag3 && flag2) {
                return true;
            }
            flag = false;
            flag2 = false;
            flag3 = false;
            flag4 = false;
            for (int j = 0; j < row && !flag; j++) {
                if (squares[j, col] == Empty) {
                    flag = true;
                } else {
                    if (squares[j, col] != i || !safeDiscs[j, col]) {
                        flag3 = true;
                    }
                }
            }
            j = row + 1;
            for (; j < 8 && !flag2; j++) {
                if (squares[j, col] == Empty) {
                    flag2 = true;
                } else {
                    if (squares[j, col] != i || !safeDiscs[j, col]) {
                        flag4 = true;
                    }
                }
            }
            if (flag && flag2 || flag && flag4 || flag3 && flag2) {
                return true;
            }
            flag = false;
            flag2 = false;
            flag3 = false;
            flag4 = false;
            j = row - 1;
            k = col - 1;
            for (; j >= 0 && k >= 0 && !flag; k--) {
                if (squares[j, k] == Empty) {
                    flag = true;
                } else {
                    if (squares[j, k] != i || !safeDiscs[j, k]) {
                        flag3 = true;
                    }
                }
                j--;
            }
            j = row + 1;
            k = col + 1;
            for (; j < 8 && k < 8 && !flag2; k++) {
                if (squares[j, k] == Empty) {
                    flag2 = true;
                } else {
                    if (squares[j, k] != i || !safeDiscs[j, k]) {
                        flag4 = true;
                    }
                }
                j++;
            }
            if (flag && flag2 || flag && flag4 || flag3 && flag2) {
                return true;
            }
            flag = false;
            flag2 = false;
            flag3 = false;
            flag4 = false;
            j = row - 1;
            k = col + 1;
            for (; j >= 0 && k < 8 && !flag; k++) {
                if (squares[j, k] == Empty) {
                    flag = true;
                } else {
                    if (squares[j, k] != i || !safeDiscs[j, k]) {
                        flag3 = true;
                    }
                }
                j--;
            }
            j = row + 1;
            k = col - 1;
            for (; j < 8 && k >= 0 && !flag2; k--) {
                if (squares[j, k] == Empty) {
                    flag2 = true;
                } else {
                    if (squares[j, k] != i || !safeDiscs[j, k]) {
                        flag4 = true;
                    }
                }
                j++;
            }
            if (flag && flag2 || flag && flag4 || flag3 && flag2) {
                return true;
            }
            return false;
        }
    }
}
