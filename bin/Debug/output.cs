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

        public Board()
        {
            // Constructor
            squares = new int[8, 8];
            safeDiscs = new bool[8, 8];
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    squares[i, j] = Empty;
                    safeDiscs[i, j] = false;
                }
            }
            UpdateCounts();
        }

        public Board(Reversi.Board board)
        {
            // Constructor
            squares = new int[8, 8];
            safeDiscs = new bool[8, 8];
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    squares[i, j] = board.squares[i, j];
                    safeDiscs[i, j] = board.safeDiscs[i, j];
                }
            }
            blackCount = board.blackCount;
            whiteCount = board.whiteCount;
            emptyCount = board.emptyCount;
            blackSafeCount = board.blackSafeCount;
            whiteSafeCount = board.whiteSafeCount;
        }

        private static Board()
        {
            Black = -1;
            Empty = 0;
            White = 1;
        }

        public void SetForNewGame()
        {
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    squares[i, j] = Empty;
                    safeDiscs[i, j] = false;
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
                            safeDiscs[i, j] = true;
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
                        blackCount++;
                        if (flag2) {
                            blackFrontierCount++;
                        }
                        if (safeDiscs[i, j]) {
                            blackSafeCount++;
                        }
                    } else {
                        if (squares[i, j] == White) {
                            whiteCount++;
                            if (flag2) {
                                whiteFrontierCount++;
                            }
                            if (safeDiscs[i, j]) {
                                whiteSafeCount++;
                                goto BasicBlock_381;
                            } else {
                                goto BasicBlock_381;
                            }
                        }
                        emptyCount++;
                    }
                    BasicBlock_381:
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
