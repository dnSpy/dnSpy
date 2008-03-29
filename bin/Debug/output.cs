using System;
namespace Reversi
{
    public class ReversiForm : System.Windows.Forms.Form
    {
        enum ToolBarButton
        {
            value__,
            NewGame,
            ResignGame,
            Separator,
            UndoAllMoves,
            UndoMove,
            ResumePlay,
            RedoMove,
            RedoAllMoves
        }
        enum Difficulty
        {
            value__,
            Beginner,
            Intermediate,
            Advanced,
            Expert
        }
        private enum GameState
        {
            value__,
            GameOver,
            InMoveAnimation,
            InPlayerMove,
            InComputerMove,
            MoveCompleted
        }
        private struct ComputerMove
        {
            public int row;
            public int col;
            public int rank;

            public ComputerMove(int row, int col)
            {
                row = row;
                col = col;
                rank = 0;
            }
        }
        private struct MoveRecord
        {
            public Reversi.Board board;
            public int currentColor;
            public System.Windows.Forms.ListViewItem moveListItem;

            public MoveRecord(Reversi.Board board, int currentColor, System.Windows.Forms.ListViewItem moveListItem)
            {
                board = new Reversi.Board(board);
                currentColor = currentColor;
                moveListItem = moveListItem;
            }
        }
        class UpdateStatusProgressDelegate : System.MulticastDelegate
        {
            public UpdateStatusProgressDelegate(object @object, System.IntPtr method)
            {
            }

            public virtual void Invoke()
            {
            }

            public virtual System.IAsyncResult BeginInvoke(System.AsyncCallback callback, object @object)
            {
            }

            public virtual void EndInvoke(System.IAsyncResult result)
            {
            }
        }
        class MakeComputerMoveDelegate : System.MulticastDelegate
        {
            public MakeComputerMoveDelegate(object @object, System.IntPtr method)
            {
            }

            public virtual void Invoke(int row, int col)
            {
            }

            public virtual System.IAsyncResult BeginInvoke(int row, int col, System.AsyncCallback callback, object @object)
            {
            }

            public virtual void EndInvoke(System.IAsyncResult result)
            {
            }
        }
        private System.Windows.Forms.MainMenu mainMenu;
        private System.Windows.Forms.MenuItem gameMenuItem;
        private System.Windows.Forms.MenuItem newGameMenuItem;
        private System.Windows.Forms.MenuItem resignGameMenuItem;
        private System.Windows.Forms.MenuItem gameSeparator1MenuItem;
        private System.Windows.Forms.MenuItem optionsMenuItem;
        private System.Windows.Forms.MenuItem statisticsMenuItem;
        private System.Windows.Forms.MenuItem gameSeparator2MenuItem;
        private System.Windows.Forms.MenuItem exitMenuItem;
        private System.Windows.Forms.MenuItem moveMenuItem;
        private System.Windows.Forms.MenuItem undoMoveMenuItem;
        private System.Windows.Forms.MenuItem undoAllMovesMenuItem;
        private System.Windows.Forms.MenuItem redoMoveMenuItem;
        private System.Windows.Forms.MenuItem redoAllMovesMenuItem;
        private System.Windows.Forms.MenuItem moveSeparatorMenuItem;
        private System.Windows.Forms.MenuItem resumePlayMenuItem;
        private System.Windows.Forms.MenuItem helpMenuItem;
        private System.Windows.Forms.MenuItem helpTopicsMenuItem;
        private System.Windows.Forms.MenuItem helpSeparatorMenuItem;
        private System.Windows.Forms.MenuItem aboutMenuItem;
        private System.Windows.Forms.ToolBar playToolBar;
        private System.Windows.Forms.ImageList playImageList;
        private System.Windows.Forms.ToolBarButton newGameToolBarButton;
        private System.Windows.Forms.ToolBarButton resignGameToolBarButton;
        private System.Windows.Forms.ToolBarButton separatorToolBarButton;
        private System.Windows.Forms.ToolBarButton undoAllMovesToolBarButton;
        private System.Windows.Forms.ToolBarButton undoMoveToolBarButton;
        private System.Windows.Forms.ToolBarButton resumePlayToolBarButton;
        private System.Windows.Forms.ToolBarButton redoMoveToolBarButton;
        private System.Windows.Forms.ToolBarButton redoAllMovesToolBarButton;
        private System.Windows.Forms.Panel boardPanel;
        private System.Windows.Forms.Label cornerLabel;
        private System.Windows.Forms.Panel squaresPanel;
        private System.Windows.Forms.Panel infoPanel;
        private System.Windows.Forms.Label whiteTextLabel;
        private System.Windows.Forms.Label whiteCountLabel;
        private System.Windows.Forms.Label blackTextLabel;
        private System.Windows.Forms.Label blackCountLabel;
        private System.Windows.Forms.Label currentColorTextLabel;
        private System.Windows.Forms.Panel currentColorPanel;
        private System.Windows.Forms.ListView moveListView;
        private System.Windows.Forms.ColumnHeader moveNullColumn;
        private System.Windows.Forms.ColumnHeader moveNumberColumn;
        private System.Windows.Forms.ColumnHeader movePlayerColumn;
        private System.Windows.Forms.ColumnHeader movePositionColumn;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ProgressBar statusProgressBar;
        private System.ComponentModel.Container components;
        private Reversi.Board board;
        private System.Windows.Forms.Label[] colLabels;
        private System.Windows.Forms.Label[] rowLabels;
        private Reversi.SquareControl[,] squareControls;
        private Reversi.Options options;
        private Reversi.Statistics statistics;
        private Reversi.ReversiForm/GameState gameState;
        private int currentColor;
        private int moveNumber;
        private System.Windows.Forms.Timer animationTimer;
        private static int animationTimerInterval;
        private int lookAheadDepth;
        private int forfeitWeight;
        private int frontierWeight;
        private int mobilityWeight;
        private int stabilityWeight;
        private System.Threading.Thread calculateComputerMoveThread;
        private static int maxRank;
        private static string alpha;
        private int keyedColNumber;
        private int keyedRowNumber;
        private System.Collections.ArrayList moveHistory;
        private int lastMoveColor;
        private bool isComputerPlaySuspended;
        private Reversi.Statistics oldStatistics;
        private System.Drawing.Rectangle windowSettings;
        private Reversi.ProgramSettings settings;
        private static string programSettingsFileName;
        private static string helpFileName;

        public ReversiForm()
        {
            options = new Reversi.Options();
            statistics = new Reversi.Statistics();
            animationTimer = new System.Windows.Forms.Timer();
            // Constructor
            InitializeComponent();
            board = new Reversi.Board();
            squareControls = new Reversi.SquareControl[8, 8];
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    squareControls[i, j] = new Reversi.SquareControl(i, j);
                    squareControls[i, j].Left = j * squareControls[i, j].Width;
                    squareControls[i, j].Top = i * squareControls[i, j].Height;
                    squaresPanel.Controls.Add(squareControls[i, j]);
                    squareControls[i, j].add_MouseMove(new System.Windows.Forms.MouseEventHandler(this, IL__ldftn(SquareControl_MouseMove())));
                    squareControls[i, j].add_MouseLeave(new System.EventHandler(this, IL__ldftn(SquareControl_MouseLeave())));
                    squareControls[i, j].add_Click(new System.EventHandler(this, IL__ldftn(SquareControl_Click())));
                }
            }
            colLabels = new System.Windows.Forms.Label[8];
            i = 0;
            for (; i < 8; i++) {
                colLabels[i] = new System.Windows.Forms.Label();
                colLabels[i].Text = Reversi.ReversiForm.alpha.Substring(i, 1);
                colLabels[i].BackColor = cornerLabel.BackColor;
                colLabels[i].ForeColor = cornerLabel.ForeColor;
                colLabels[i].TextAlign = 32;
                colLabels[i].Width = squareControls[0, 0].Width;
                colLabels[i].Height = cornerLabel.Height;
                colLabels[i].Left = cornerLabel.Width + i * colLabels[0].Width;
                colLabels[i].Top = 0;
                boardPanel.Controls.Add(colLabels[i]);
            }
            rowLabels = new System.Windows.Forms.Label[8];
            i = 0;
            for (; i < 8; i++) {
                rowLabels[i] = new System.Windows.Forms.Label();
                expr26F = rowLabels[i];
                int k = i + 1;
                expr26F.Text = IL__ldloca(k).ToString();
                rowLabels[i].BackColor = cornerLabel.BackColor;
                rowLabels[i].ForeColor = cornerLabel.ForeColor;
                rowLabels[i].TextAlign = 32;
                rowLabels[i].Width = cornerLabel.Height;
                rowLabels[i].Height = squareControls[0, 0].Height;
                rowLabels[i].Left = 0;
                rowLabels[i].Top = cornerLabel.Height + i * rowLabels[0].Width;
                boardPanel.Controls.Add(rowLabels[i]);
            }
            gameState = 0;
            animationTimer.Interval = Reversi.ReversiForm.animationTimerInterval;
            animationTimer.add_Tick(new System.EventHandler(this, IL__ldftn(AnimateMove())));
            expr37D = this;
            System.Drawing.Point V_3 = DesktopLocation;
            expr387 = IL__ldloca(V_3).X;
            System.Drawing.Point V_4 = DesktopLocation;
            expr396 = IL__ldloca(V_4).Y;
            System.Drawing.Size V_5 = ClientSize;
            expr3A5 = IL__ldloca(V_5).Width;
            System.Drawing.Size V_6 = ClientSize;
            expr37D.windowSettings = new System.Drawing.Rectangle(expr387, expr396, expr3A5, (IL__ldloca(V_6).Height));
            settings = new Reversi.ProgramSettings((Reversi.ReversiForm.programSettingsFileName));
            LoadProgramSettings();
        }

        private static ReversiForm()
        {
            Reversi.ReversiForm.animationTimerInterval = 50;
            Reversi.ReversiForm.maxRank = 2147483583;
            Reversi.ReversiForm.alpha = "ABCDEFGH";
            Reversi.ReversiForm.programSettingsFileName = "Reversi.xml";
            Reversi.ReversiForm.helpFileName = "Reversi.chm";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && components) {
                components.Dispose();
            }
            Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.Resources.ResourceManager V_0 = new System.Resources.ResourceManager((Type.GetTypeFromHandle(typeof(Reversi.ReversiForm).TypeHandle)));
            infoPanel = new System.Windows.Forms.Panel();
            currentColorPanel = new System.Windows.Forms.Panel();
            whiteCountLabel = new System.Windows.Forms.Label();
            whiteTextLabel = new System.Windows.Forms.Label();
            currentColorTextLabel = new System.Windows.Forms.Label();
            blackCountLabel = new System.Windows.Forms.Label();
            blackTextLabel = new System.Windows.Forms.Label();
            moveListView = new System.Windows.Forms.ListView();
            moveNullColumn = new System.Windows.Forms.ColumnHeader();
            moveNumberColumn = new System.Windows.Forms.ColumnHeader();
            movePlayerColumn = new System.Windows.Forms.ColumnHeader();
            movePositionColumn = new System.Windows.Forms.ColumnHeader();
            squaresPanel = new System.Windows.Forms.Panel();
            mainMenu = new System.Windows.Forms.MainMenu();
            gameMenuItem = new System.Windows.Forms.MenuItem();
            newGameMenuItem = new System.Windows.Forms.MenuItem();
            resignGameMenuItem = new System.Windows.Forms.MenuItem();
            gameSeparator1MenuItem = new System.Windows.Forms.MenuItem();
            optionsMenuItem = new System.Windows.Forms.MenuItem();
            statisticsMenuItem = new System.Windows.Forms.MenuItem();
            gameSeparator2MenuItem = new System.Windows.Forms.MenuItem();
            exitMenuItem = new System.Windows.Forms.MenuItem();
            moveMenuItem = new System.Windows.Forms.MenuItem();
            undoMoveMenuItem = new System.Windows.Forms.MenuItem();
            redoMoveMenuItem = new System.Windows.Forms.MenuItem();
            undoAllMovesMenuItem = new System.Windows.Forms.MenuItem();
            redoAllMovesMenuItem = new System.Windows.Forms.MenuItem();
            moveSeparatorMenuItem = new System.Windows.Forms.MenuItem();
            resumePlayMenuItem = new System.Windows.Forms.MenuItem();
            helpMenuItem = new System.Windows.Forms.MenuItem();
            helpTopicsMenuItem = new System.Windows.Forms.MenuItem();
            helpSeparatorMenuItem = new System.Windows.Forms.MenuItem();
            aboutMenuItem = new System.Windows.Forms.MenuItem();
            boardPanel = new System.Windows.Forms.Panel();
            cornerLabel = new System.Windows.Forms.Label();
            statusProgressBar = new System.Windows.Forms.ProgressBar();
            statusLabel = new System.Windows.Forms.Label();
            statusPanel = new System.Windows.Forms.Panel();
            playToolBar = new System.Windows.Forms.ToolBar();
            newGameToolBarButton = new System.Windows.Forms.ToolBarButton();
            resignGameToolBarButton = new System.Windows.Forms.ToolBarButton();
            separatorToolBarButton = new System.Windows.Forms.ToolBarButton();
            undoAllMovesToolBarButton = new System.Windows.Forms.ToolBarButton();
            undoMoveToolBarButton = new System.Windows.Forms.ToolBarButton();
            resumePlayToolBarButton = new System.Windows.Forms.ToolBarButton();
            redoMoveToolBarButton = new System.Windows.Forms.ToolBarButton();
            redoAllMovesToolBarButton = new System.Windows.Forms.ToolBarButton();
            playImageList = new System.Windows.Forms.ImageList((components));
            infoPanel.SuspendLayout();
            boardPanel.SuspendLayout();
            statusPanel.SuspendLayout();
            SuspendLayout();
            infoPanel.Anchor = 11;
            infoPanel.Controls.Add(currentColorPanel);
            infoPanel.Controls.Add(whiteCountLabel);
            infoPanel.Controls.Add(whiteTextLabel);
            infoPanel.Controls.Add(currentColorTextLabel);
            infoPanel.Controls.Add(blackCountLabel);
            infoPanel.Controls.Add(blackTextLabel);
            infoPanel.Controls.Add(moveListView);
            infoPanel.Location = new System.Drawing.Point(296, 32);
            infoPanel.Name = "infoPanel";
            infoPanel.Size = new System.Drawing.Size(168, 276);
            infoPanel.TabIndex = 3;
            currentColorPanel.BorderStyle = 1;
            currentColorPanel.Location = new System.Drawing.Point(88, 56);
            currentColorPanel.Name = "currentColorPanel";
            currentColorPanel.Size = new System.Drawing.Size(16, 16);
            currentColorPanel.TabIndex = 5;
            currentColorPanel.Visible = 0;
            whiteCountLabel.Location = new System.Drawing.Point(80, 32);
            whiteCountLabel.Name = "whiteCountLabel";
            whiteCountLabel.Size = new System.Drawing.Size(24, 13);
            whiteCountLabel.TabIndex = 3;
            whiteCountLabel.Text = "0";
            whiteCountLabel.TextAlign = 64;
            whiteTextLabel.AutoSize = 1;
            whiteTextLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, 0, 3, 0);
            whiteTextLabel.Location = new System.Drawing.Point(40, 32);
            whiteTextLabel.Name = "whiteTextLabel";
            whiteTextLabel.Size = new System.Drawing.Size(39, 16);
            whiteTextLabel.TabIndex = 2;
            whiteTextLabel.Text = "White: ";
            currentColorTextLabel.AutoSize = 1;
            currentColorTextLabel.Location = new System.Drawing.Point(32, 56);
            currentColorTextLabel.Name = "currentColorTextLabel";
            currentColorTextLabel.Size = new System.Drawing.Size(48, 16);
            currentColorTextLabel.TabIndex = 4;
            currentColorTextLabel.Text = "Current: ";
            currentColorTextLabel.Visible = 0;
            blackCountLabel.Location = new System.Drawing.Point(80, 8);
            blackCountLabel.Name = "blackCountLabel";
            blackCountLabel.Size = new System.Drawing.Size(24, 13);
            blackCountLabel.TabIndex = 1;
            blackCountLabel.Text = "0";
            blackCountLabel.TextAlign = 64;
            blackTextLabel.AutoSize = 1;
            blackTextLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, 0, 3, 0);
            blackTextLabel.Location = new System.Drawing.Point(40, 8);
            blackTextLabel.Name = "blackTextLabel";
            blackTextLabel.Size = new System.Drawing.Size(38, 16);
            blackTextLabel.TabIndex = 0;
            blackTextLabel.Text = "Black: ";
            moveListView.Anchor = 15;
            moveListView.BorderStyle = 1;
            expr5ED = moveListView.Columns;
            System.Windows.Forms.ColumnHeader[] V_1 = new System.Windows.Forms.ColumnHeader[4];
            V_1[0] = moveNullColumn;
            V_1[1] = moveNumberColumn;
            V_1[2] = movePlayerColumn;
            V_1[3] = movePositionColumn;
            expr5ED.AddRange(V_1);
            moveListView.FullRowSelect = 1;
            moveListView.HeaderStyle = 1;
            moveListView.Location = new System.Drawing.Point(2, 88);
            moveListView.Name = "moveListView";
            moveListView.Size = new System.Drawing.Size(164, 188);
            moveListView.TabIndex = 6;
            moveListView.TabStop = 0;
            moveListView.View = 1;
            moveNullColumn.Text = "";
            moveNullColumn.TextAlign = 1;
            moveNullColumn.Width = 0;
            moveNumberColumn.Text = "#";
            moveNumberColumn.TextAlign = 1;
            moveNumberColumn.Width = 32;
            movePlayerColumn.Text = "Player";
            movePlayerColumn.TextAlign = 1;
            movePlayerColumn.Width = 52;
            movePositionColumn.Text = "Position";
            movePositionColumn.TextAlign = 2;
            movePositionColumn.Width = 62;
            squaresPanel.Anchor = 15;
            squaresPanel.Location = new System.Drawing.Point(16, 16);
            squaresPanel.Name = "squaresPanel";
            squaresPanel.Size = new System.Drawing.Size(256, 256);
            squaresPanel.TabIndex = 1;
            expr79C = mainMenu.MenuItems;
            System.Windows.Forms.MenuItem[] V_2 = new System.Windows.Forms.MenuItem[3];
            V_2[0] = gameMenuItem;
            V_2[1] = moveMenuItem;
            V_2[2] = helpMenuItem;
            expr79C.AddRange(V_2);
            gameMenuItem.Index = 0;
            expr7DB = gameMenuItem.MenuItems;
            System.Windows.Forms.MenuItem[] V_3 = new System.Windows.Forms.MenuItem[7];
            V_3[0] = newGameMenuItem;
            V_3[1] = resignGameMenuItem;
            V_3[2] = gameSeparator1MenuItem;
            V_3[3] = optionsMenuItem;
            V_3[4] = statisticsMenuItem;
            V_3[5] = gameSeparator2MenuItem;
            V_3[6] = exitMenuItem;
            expr7DB.AddRange(V_3);
            gameMenuItem.ShowShortcut = 0;
            gameMenuItem.Text = "&Game";
            newGameMenuItem.Index = 0;
            newGameMenuItem.Shortcut = 131150;
            newGameMenuItem.Text = "&New Game";
            newGameMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(newGameMenuItem_Click())));
            resignGameMenuItem.Enabled = 0;
            resignGameMenuItem.Index = 1;
            resignGameMenuItem.Shortcut = 131154;
            resignGameMenuItem.Text = "&Resign Game";
            resignGameMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(resignGameMenuItem_Click())));
            gameSeparator1MenuItem.Index = 2;
            gameSeparator1MenuItem.Text = "-";
            optionsMenuItem.Index = 3;
            optionsMenuItem.Shortcut = 131151;
            optionsMenuItem.Text = "&Options...";
            optionsMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(optionsMenuItem_Click())));
            statisticsMenuItem.Index = 4;
            statisticsMenuItem.Shortcut = 131155;
            statisticsMenuItem.Text = "&Statistics...";
            statisticsMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(statisticsMenuItem_Click())));
            gameSeparator2MenuItem.Index = 5;
            gameSeparator2MenuItem.Text = "-";
            exitMenuItem.Index = 6;
            exitMenuItem.Shortcut = 131160;
            exitMenuItem.Text = "E&xit";
            exitMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(exitMenuItem_Click())));
            moveMenuItem.Index = 1;
            expr9ED = moveMenuItem.MenuItems;
            System.Windows.Forms.MenuItem[] V_4 = new System.Windows.Forms.MenuItem[6];
            V_4[0] = undoMoveMenuItem;
            V_4[1] = redoMoveMenuItem;
            V_4[2] = undoAllMovesMenuItem;
            V_4[3] = redoAllMovesMenuItem;
            V_4[4] = moveSeparatorMenuItem;
            V_4[5] = resumePlayMenuItem;
            expr9ED.AddRange(V_4);
            moveMenuItem.ShowShortcut = 0;
            moveMenuItem.Text = "&Move";
            undoMoveMenuItem.Enabled = 0;
            undoMoveMenuItem.Index = 0;
            undoMoveMenuItem.Shortcut = 131162;
            undoMoveMenuItem.Text = "&Undo Move";
            undoMoveMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(undoMoveMenuItem_Click())));
            redoMoveMenuItem.Enabled = 0;
            redoMoveMenuItem.Index = 1;
            redoMoveMenuItem.Shortcut = 131161;
            redoMoveMenuItem.Text = "&Redo Move";
            redoMoveMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(redoMoveMenuItem_Click())));
            undoAllMovesMenuItem.Enabled = 0;
            undoAllMovesMenuItem.Index = 2;
            undoAllMovesMenuItem.Shortcut = 196698;
            undoAllMovesMenuItem.Text = "U&ndo All Moves";
            undoAllMovesMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(undoAllMovesmenuItem_Click())));
            redoAllMovesMenuItem.Enabled = 0;
            redoAllMovesMenuItem.Index = 3;
            redoAllMovesMenuItem.Shortcut = 196697;
            redoAllMovesMenuItem.Text = "Re&do All Moves";
            redoAllMovesMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(redoAllMovesMenuItem_Click())));
            moveSeparatorMenuItem.Index = 4;
            moveSeparatorMenuItem.Text = "-";
            resumePlayMenuItem.Enabled = 0;
            resumePlayMenuItem.Index = 5;
            resumePlayMenuItem.Shortcut = 131152;
            resumePlayMenuItem.Text = "Resume &Play";
            resumePlayMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(resumePlayMenuItem_Click())));
            helpMenuItem.Index = 2;
            exprC12 = helpMenuItem.MenuItems;
            System.Windows.Forms.MenuItem[] V_5 = new System.Windows.Forms.MenuItem[3];
            V_5[0] = helpTopicsMenuItem;
            V_5[1] = helpSeparatorMenuItem;
            V_5[2] = aboutMenuItem;
            exprC12.AddRange(V_5);
            helpMenuItem.ShowShortcut = 0;
            helpMenuItem.Text = "&Help";
            helpTopicsMenuItem.Index = 0;
            helpTopicsMenuItem.Shortcut = 131144;
            helpTopicsMenuItem.Text = "&Help Topics";
            helpTopicsMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(helpTopicsMenuItem_Click())));
            helpSeparatorMenuItem.Index = 1;
            helpSeparatorMenuItem.Text = "-";
            aboutMenuItem.Index = 2;
            aboutMenuItem.Shortcut = 131137;
            aboutMenuItem.Text = "&About";
            aboutMenuItem.add_Click(new System.EventHandler(this, IL__ldftn(aboutMenuItem_Click())));
            boardPanel.Anchor = 15;
            boardPanel.BackColor = Drawing.SystemColors.Control;
            boardPanel.Controls.Add(cornerLabel);
            boardPanel.Controls.Add(squaresPanel);
            boardPanel.Location = new System.Drawing.Point(8, 32);
            boardPanel.Name = "boardPanel";
            boardPanel.Size = new System.Drawing.Size(272, 272);
            boardPanel.TabIndex = 2;
            cornerLabel.BackColor = Drawing.SystemColors.ControlDark;
            cornerLabel.ForeColor = Drawing.SystemColors.ControlLightLight;
            cornerLabel.Location = new System.Drawing.Point(0, 0);
            cornerLabel.Name = "cornerLabel";
            cornerLabel.Size = new System.Drawing.Size(16, 16);
            cornerLabel.TabIndex = 0;
            statusProgressBar.Anchor = 9;
            statusProgressBar.BackColor = Drawing.SystemColors.ControlLight;
            statusProgressBar.Location = new System.Drawing.Point(350, 2);
            statusProgressBar.Name = "statusProgressBar";
            statusProgressBar.Size = new System.Drawing.Size(104, 16);
            statusProgressBar.Step = 1;
            statusProgressBar.TabIndex = 1;
            statusProgressBar.Visible = 0;
            statusLabel.AutoSize = 1;
            statusLabel.Location = new System.Drawing.Point(16, 2);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new System.Drawing.Size(0, 16);
            statusLabel.TabIndex = 0;
            statusLabel.TextAlign = 16;
            statusPanel.Anchor = 14;
            statusPanel.BorderStyle = 2;
            statusPanel.Controls.Add(statusProgressBar);
            statusPanel.Controls.Add(statusLabel);
            statusPanel.Location = new System.Drawing.Point(8, 312);
            statusPanel.Name = "statusPanel";
            statusPanel.Size = new System.Drawing.Size(456, 24);
            statusPanel.TabIndex = 4;
            exprF60 = playToolBar.Buttons;
            System.Windows.Forms.ToolBarButton[] V_6 = new System.Windows.Forms.ToolBarButton[8];
            V_6[0] = newGameToolBarButton;
            V_6[1] = resignGameToolBarButton;
            V_6[2] = separatorToolBarButton;
            V_6[3] = undoAllMovesToolBarButton;
            V_6[4] = undoMoveToolBarButton;
            V_6[5] = resumePlayToolBarButton;
            V_6[6] = redoMoveToolBarButton;
            V_6[7] = redoAllMovesToolBarButton;
            exprF60.AddRange(V_6);
            playToolBar.Divider = 0;
            playToolBar.DropDownArrows = 1;
            playToolBar.ImageList = playImageList;
            playToolBar.Location = new System.Drawing.Point(0, 0);
            playToolBar.Name = "playToolBar";
            playToolBar.ShowToolTips = 1;
            playToolBar.Size = new System.Drawing.Size(472, 26);
            playToolBar.TabIndex = 1;
            playToolBar.add_ButtonClick(new System.Windows.Forms.ToolBarButtonClickEventHandler(this, IL__ldftn(playToolBar_ButtonClick())));
            newGameToolBarButton.ImageIndex = 0;
            newGameToolBarButton.ToolTipText = "New Game";
            resignGameToolBarButton.Enabled = 0;
            resignGameToolBarButton.ImageIndex = 1;
            resignGameToolBarButton.ToolTipText = "Resign Game";
            separatorToolBarButton.Style = 3;
            undoAllMovesToolBarButton.Enabled = 0;
            undoAllMovesToolBarButton.ImageIndex = 2;
            undoAllMovesToolBarButton.ToolTipText = "Undo All Moves";
            undoMoveToolBarButton.Enabled = 0;
            undoMoveToolBarButton.ImageIndex = 3;
            undoMoveToolBarButton.ToolTipText = "Undo Move";
            resumePlayToolBarButton.Enabled = 0;
            resumePlayToolBarButton.ImageIndex = 4;
            resumePlayToolBarButton.ToolTipText = "Resume Play";
            redoMoveToolBarButton.Enabled = 0;
            redoMoveToolBarButton.ImageIndex = 5;
            redoMoveToolBarButton.ToolTipText = "Redo Move";
            redoAllMovesToolBarButton.Enabled = 0;
            redoAllMovesToolBarButton.ImageIndex = 6;
            redoAllMovesToolBarButton.ToolTipText = "Redo All Moves";
            playImageList.ImageSize = new System.Drawing.Size(16, 16);
            playImageList.ImageStream = (System.Windows.Forms.ImageListStreamer)V_0.GetObject("playImageList.ImageStream");
            playImageList.TransparentColor = Drawing.Color.Transparent;
            AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            ClientSize = new System.Drawing.Size(472, 345);
            Controls.Add(playToolBar);
            Controls.Add(boardPanel);
            Controls.Add(infoPanel);
            Controls.Add(statusPanel);
            Icon = (System.Drawing.Icon)V_0.GetObject("$this.Icon");
            KeyPreview = 1;
            Menu = mainMenu;
            Name = "ReversiForm";
            Text = "Reversi";
            add_Resize(new System.EventHandler(this, IL__ldftn(ReversiForm_Resize())));
            add_Closing(new System.ComponentModel.CancelEventHandler(this, IL__ldftn(ReversiForm_Closing())));
            add_KeyPress(new System.Windows.Forms.KeyPressEventHandler(this, IL__ldftn(ReversiForm_KeyPress())));
            add_Move(new System.EventHandler(this, IL__ldftn(ReversiForm_Move())));
            add_Closed(new System.EventHandler(this, IL__ldftn(ReversiForm_Closed())));
            infoPanel.ResumeLayout(0);
            boardPanel.ResumeLayout(0);
            statusPanel.ResumeLayout(0);
            ResumeLayout(0);
        }

        private static void Main()
        {
            Windows.Forms.Application.Run(new Reversi.ReversiForm());
        }

        private void StartGame()
        {
            StartGame(0);
        }

        private void StartGame(bool isRestart)
        {
            expr01 = newGameMenuItem;
            expr12 = playToolBar.Buttons.Item;
            expr18 = 0;
            bool flag = expr18;
            expr12.Enabled = expr18;
            expr01.Enabled = flag;
            expr26 = resignGameMenuItem;
            expr37 = playToolBar.Buttons.Item;
            expr3D = 1;
            bool flag2 = expr3D;
            expr37.Enabled = expr3D;
            expr26.Enabled = flag2;
            if (!isRestart) {
                expr51 = undoMoveMenuItem;
                expr57 = undoAllMovesMenuItem;
                expr68 = playToolBar.Buttons.Item;
                expr79 = playToolBar.Buttons.Item;
                expr7F = 0;
                bool flag3 = expr7F;
                expr79.Enabled = expr7F;
                expr87 = flag3;
                bool flag4 = expr87;
                expr68.Enabled = expr87;
                expr8F = flag4;
                bool flag5 = expr8F;
                expr57.Enabled = expr8F;
                expr51.Enabled = flag5;
                expr9F = redoMoveMenuItem;
                exprA5 = redoAllMovesMenuItem;
                exprB6 = playToolBar.Buttons.Item;
                exprC7 = playToolBar.Buttons.Item;
                exprCD = 0;
                bool flag6 = exprCD;
                exprC7.Enabled = exprCD;
                exprD7 = flag6;
                bool flag7 = exprD7;
                exprB6.Enabled = exprD7;
                exprE1 = flag7;
                bool flag8 = exprE1;
                exprA5.Enabled = exprE1;
                expr9F.Enabled = flag8;
                exprF1 = resumePlayMenuItem;
                expr102 = playToolBar.Buttons.Item;
                expr108 = 0;
                bool flag9 = expr108;
                expr102.Enabled = expr108;
                exprF1.Enabled = flag9;
            }
            currentColorTextLabel.Visible = 1;
            currentColorPanel.Visible = 1;
            if (!isRestart) {
                moveNumber = 1;
                moveListView.Items.Clear();
                moveListView.Refresh();
                moveHistory = new System.Collections.ArrayList(60);
                lastMoveColor = Reversi.Board.Empty;
                isComputerPlaySuspended = 0;
                board.SetForNewGame();
                UpdateBoardDisplay();
                statusLabel.Text = "";
                statusPanel.Refresh();
                currentColor = options.FirstMove;
            }
            StartTurn();
        }

        private void EndGame()
        {
            EndGame(0);
        }

        private void EndGame(bool isResignation)
        {
            gameState = 0;
            animationTimer.Stop();
            expr13 = newGameMenuItem;
            expr24 = playToolBar.Buttons.Item;
            expr2A = 1;
            bool flag = expr2A;
            expr24.Enabled = expr2A;
            expr13.Enabled = flag;
            expr38 = resignGameMenuItem;
            expr49 = playToolBar.Buttons.Item;
            expr4F = 0;
            bool flag2 = expr4F;
            expr49.Enabled = expr4F;
            expr38.Enabled = flag2;
            expr5F = undoMoveMenuItem;
            expr65 = undoAllMovesMenuItem;
            expr76 = playToolBar.Buttons.Item;
            expr87 = playToolBar.Buttons.Item;
            expr8D = 0;
            bool flag3 = expr8D;
            expr87.Enabled = expr8D;
            expr97 = flag3;
            bool flag4 = expr97;
            expr76.Enabled = expr97;
            exprA1 = flag4;
            bool flag5 = exprA1;
            expr65.Enabled = exprA1;
            expr5F.Enabled = flag5;
            exprB1 = redoMoveMenuItem;
            exprB7 = redoAllMovesMenuItem;
            exprC8 = playToolBar.Buttons.Item;
            exprD9 = playToolBar.Buttons.Item;
            exprDF = 0;
            bool flag6 = exprDF;
            exprD9.Enabled = exprDF;
            exprE9 = flag6;
            bool flag7 = exprE9;
            exprC8.Enabled = exprE9;
            exprF3 = flag7;
            bool flag8 = exprF3;
            exprB7.Enabled = exprF3;
            exprB1.Enabled = flag8;
            expr103 = resumePlayMenuItem;
            expr114 = playToolBar.Buttons.Item;
            expr11A = 0;
            bool flag9 = expr11A;
            expr114.Enabled = expr11A;
            expr103.Enabled = flag9;
            currentColorTextLabel.Visible = 0;
            currentColorPanel.BackColor = .Empty;
            currentColorPanel.Visible = 0;
            statusProgressBar.Visible = 0;
            statusPanel.Refresh();
            int i = Reversi.Board.Empty;
            int j = Reversi.Board.Empty;
            if (IsComputerPlayer(Reversi.Board.Black) && !IsComputerPlayer(Reversi.Board.White)) {
                i = Reversi.Board.Black;
                j = Reversi.Board.White;
            }
            if (IsComputerPlayer(Reversi.Board.White) && !IsComputerPlayer(Reversi.Board.Black)) {
                i = Reversi.Board.White;
                j = Reversi.Board.Black;
            }
            oldStatistics = new Reversi.Statistics((statistics));
            if (isResignation) {
                if (IsComputerPlayer(Reversi.Board.Black) && IsComputerPlayer(Reversi.Board.White)) {
                    statusLabel.Text = "Game aborted.";
                    goto BasicBlock_131;
                }
                int k = currentColor;
                if (IsComputerPlayer(Reversi.Board.Black) || IsComputerPlayer(Reversi.Board.White)) {
                    k = j;
                }
                if (k == Reversi.Board.Black) {
                    statusLabel.Text = "Black resigns.";
                    statistics.Update(0, 64, i, j);
                    goto BasicBlock_131;
                }
                statusLabel.Text = "White resigns.";
                statistics.Update(64, 0, i, j);
            } else {
                if (board.BlackCount > board.WhiteCount) {
                    statusLabel.Text = "Black wins.";
                } else {
                    if (board.WhiteCount > board.BlackCount) {
                        statusLabel.Text = "White wins.";
                        goto BasicBlock_130;
                    }
                    statusLabel.Text = "Draw.";
                }
                BasicBlock_130:
                statistics.Update(board.BlackCount, board.WhiteCount, i, j);
            }
            BasicBlock_131:
            statusPanel.Refresh();
            expr30E = undoMoveMenuItem;
            expr314 = undoAllMovesMenuItem;
            expr325 = playToolBar.Buttons.Item;
            expr32B = undoAllMovesMenuItem;
            expr33C = playToolBar.Buttons.Item;
            expr342 = 1;
            bool flag10 = expr342;
            expr33C.Enabled = expr342;
            expr34C = flag10;
            bool flag11 = expr34C;
            expr32B.Enabled = expr34C;
            expr356 = flag11;
            bool flag12 = expr356;
            expr325.Enabled = expr356;
            expr360 = flag12;
            bool flag13 = expr360;
            expr314.Enabled = expr360;
            expr30E.Enabled = flag13;
        }

        private void StartTurn()
        {
            if (!board.HasAnyValidMove(currentColor)) {
                currentColor = currentColor * -1;
                if (!board.HasAnyValidMove(currentColor)) {
                    EndGame();
                    return;
                }
            }
            expr3B = "{0}'s";
            if (currentColor != Reversi.Board.Black) {
                expr4D = "White";
            } else {
                expr54 = "Black";
            }
            string V_0 = String.Format(expr3B, expr4D);
            if (options.ComputerPlaysBlack && !options.ComputerPlaysWhite || options.ComputerPlaysWhite && !options.ComputerPlaysBlack) {
                if (!IsComputerPlayer(currentColor)) {
                    exprA1 = "Your";
                } else {
                    exprA8 = "My";
                }
                V_0 = exprA1;
            }
            if (currentColor == Reversi.Board.Black) {
                currentColorPanel.BackColor = Drawing.Color.Black;
            } else {
                currentColorPanel.BackColor = Drawing.Color.White;
            }
            currentColorPanel.Refresh();
            if (IsComputerPlayer(currentColor)) {
                gameState = 3;
                if (isComputerPlaySuspended) {
                    expr109 = resumePlayMenuItem;
                    expr11A = playToolBar.Buttons.Item;
                    expr120 = 1;
                    bool flag = expr120;
                    expr11A.Enabled = expr120;
                    expr109.Enabled = flag;
                    string V_1 = IL__box(System.Windows.Forms.Shortcut, resumePlayMenuItem.Shortcut).ToString();
                    System.Text.RegularExpressions.Regex V_2 = new System.Text.RegularExpressions.Regex("(Alt|Ctrl|Shift)", 1);
                    V_1 = V_2.Replace(V_1, "$1+");
                    statusLabel.Text = String.Format("{0} move... (Suspended, press {1} to resume play.)", V_0, V_1);
                    statusProgressBar.Visible = 0;
                    goto BasicBlock_238;
                }
                statusLabel.Text = String.Format("{0} move, thinking... ", V_0);
                statusProgressBar.Minimum = 0;
                statusProgressBar.Maximum = board.GetValidMoveCount(currentColor);
                statusProgressBar.Value = 0;
                statusProgressBar.Left = statusLabel.Left + statusLabel.Width;
                statusProgressBar.Visible = 1;
                calculateComputerMoveThread = new System.Threading.Thread((new System.Threading.ThreadStart(this, IL__ldftn(CalculateComputerMove()))));
                calculateComputerMoveThread.IsBackground = 1;
                calculateComputerMoveThread.Priority = 0;
                calculateComputerMoveThread.Name = "Calculate Computer Move";
                calculateComputerMoveThread.Start();
            } else {
                gameState = 2;
                keyedColNumber = -1;
                keyedRowNumber = -1;
                if (options.ShowValidMoves) {
                    HighlightValidMoves();
                    squaresPanel.Refresh();
                }
                statusLabel.Text = String.Format("{0} move...", V_0);
                statusProgressBar.Visible = 0;
                IL__pop(Focus());
            }
            BasicBlock_238:
            statusPanel.Refresh();
        }

        private bool IsComputerPlayer(int color)
        {
            if (!options.ComputerPlaysBlack || color != Reversi.Board.Black) {
                if (options.ComputerPlaysWhite) {
                    return color == (Reversi.Board.White != 0);
                }
                return false;
            }
            return true;
        }

        private void MakeMove(int row, int col)
        {
            for (; moveHistory.Count > moveNumber - 1;) {
                moveHistory.RemoveAt(moveHistory.Count - 1);
            }
            string V_0 = "Black";
            if (currentColor == Reversi.Board.White) {
                V_0 = "White";
            }
            string[] V_6 = new string[4];
            V_6[0] = .Empty;
            V_6[1] = IL__ldflda(moveNumber, this).ToString();
            V_6[2] = V_0;
            expr6D = V_6;
            expr6F = 3;
            expr7B = IL__box(System.Char, Reversi.ReversiForm.alpha.Chars);
            int k = row + 1;
            expr6D[expr6F] = String.Concat(expr7B, IL__ldloca(k).ToString());
            string[] V_1 = V_6;
            System.Windows.Forms.ListViewItem V_2 = new System.Windows.Forms.ListViewItem(V_1);
            IL__pop(moveListView.Items.Add(V_2));
            moveListView.EnsureVisible(moveListView.Items.Count - 1);
            IL__pop(moveHistory.Add(IL__box(Reversi.ReversiForm/MoveRecord, new Reversi.ReversiForm/MoveRecord((board), (currentColor), V_2))));
            exprEF = undoMoveMenuItem;
            exprF5 = undoAllMovesMenuItem;
            expr106 = playToolBar.Buttons.Item;
            expr117 = playToolBar.Buttons.Item;
            expr11D = 1;
            bool flag = expr11D;
            expr117.Enabled = expr11D;
            expr127 = flag;
            bool flag2 = expr127;
            expr106.Enabled = expr127;
            expr131 = flag2;
            bool flag3 = expr131;
            exprF5.Enabled = expr131;
            exprEF.Enabled = flag3;
            expr141 = redoMoveMenuItem;
            expr147 = redoAllMovesMenuItem;
            expr158 = playToolBar.Buttons.Item;
            expr169 = playToolBar.Buttons.Item;
            expr16F = 0;
            bool flag4 = expr16F;
            expr169.Enabled = expr16F;
            expr179 = flag4;
            bool flag5 = expr179;
            expr158.Enabled = expr179;
            expr183 = flag5;
            bool flag6 = expr183;
            expr147.Enabled = expr183;
            expr141.Enabled = flag6;
            moveNumber++;
            statusLabel.Text = "";
            statusProgressBar.Visible = 0;
            statusPanel.Refresh();
            UnHighlightSquares();
            Reversi.Board V_3 = new Reversi.Board((board));
            board.MakeMove(currentColor, row, col);
            if (options.AnimateMoves) {
                for (int i = 0; i < 8; i++) {
                    for (int j = 0; j < 8; j++) {
                        if (i == row && j == col) {
                            squareControls[i, j].IsNew = 1;
                        } else {
                            if (board.GetSquareContents(i, j) != V_3.GetSquareContents(i, j)) {
                                squareControls[i, j].AnimationCounter = Reversi.SquareControl.AnimationStart;
                            }
                        }
                    }
                }
            }
            UpdateBoardDisplay();
            lastMoveColor = currentColor;
            if (options.AnimateMoves) {
                gameState = 1;
                animationTimer.Start();
                return;
            }
            EndMove();
        }

        private void EndMove()
        {
            gameState = 4;
            currentColor = currentColor * -1;
            StartTurn();
        }

        private void AnimateMove(object sender, System.EventArgs e)
        {
            {
                expr06 = board;
                Reversi.Board V_3 = expr06;
                Threading.Monitor.Enter(expr06);
                if (gameState != 1) goto BasicBlock_442; 
                bool flag = true;
                for (int i = 0; i < 8; i++) {
                    for (int j = 0; j < 8; j++) {
                        if (squareControls[i, j].AnimationCounter <= Reversi.SquareControl.AnimationStop) goto BasicBlock_435; 
                        expr46 = squareControls[i, j];
                        expr46.AnimationCounter = expr46.AnimationCounter - 1;
                        flag = false;
                        BasicBlock_435:
                    }
                }
                squaresPanel.Refresh();
                if (!flag) goto BasicBlock_442; 
                StopMoveAnimation();
                UpdateBoardDisplay();
                EndMove();
                BasicBlock_442:
                IL__leave(IL_8E);
            }
            Threading.Monitor.Exit(V_3);
            IL__endfinally();
        }

        private void StopMoveAnimation()
        {
            animationTimer.Stop();
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    Reversi.SquareControl V_0 = (Reversi.SquareControl)squaresPanel.Controls.Item;
                    V_0.AnimationCounter = Reversi.SquareControl.AnimationStop;
                    V_0.IsNew = 0;
                }
            }
        }

        private void MakePlayerMove(int row, int col)
        {
            isComputerPlaySuspended = 0;
            MakeMove(row, col);
        }

        private void KillComputerMoveThread()
        {
            {
                if (!calculateComputerMoveThread) goto BasicBlock_491; 
                if (calculateComputerMoveThread.ThreadState != 16) goto BasicBlock_492; 
                BasicBlock_491:
                return;
                BasicBlock_492:
                calculateComputerMoveThread.Abort();
                calculateComputerMoveThread.Join();
                IL__leave(IL_33);
            }
            {
                IL__pop(expr30);
                IL__leave(IL_33);
            }
            IL__leave(IL_3D);
            calculateComputerMoveThread = null;
            IL__endfinally();
        }

        private void UpdateStatusProgress()
        {
            if (statusProgressBar.Value < statusProgressBar.Maximum) {
                expr1E = statusProgressBar;
                expr1E.Value = expr1E.Value + 1;
                statusProgressBar.Refresh();
            }
        }

        private void MakeComputerMove(int row, int col)
        {
            {
                expr06 = board;
                Reversi.Board V_0 = expr06;
                Threading.Monitor.Enter(expr06);
                MakeMove(row, col);
                IL__leave(IL_1E);
            }
            Threading.Monitor.Exit(V_0);
            IL__endfinally();
        }

        private void CalculateComputerMove()
        {
            SetAIParameters();
            Reversi.ReversiForm/ComputerMove V_0 = GetBestMove(board);
            object[] V_3 = new object[2];
            V_3[0] = IL__box(System.Int32, IL__ldloca(V_0).row);
            V_3[1] = IL__box(System.Int32, IL__ldloca(V_0).col);
            object[] V_1 = V_3;
            Reversi.ReversiForm/MakeComputerMoveDelegate V_2 = new Reversi.ReversiForm/MakeComputerMoveDelegate(this, IL__ldftn(MakeComputerMove()));
            IL__pop(BeginInvoke(V_2, V_1));
        }

        private Reversi.ReversiForm/ComputerMove GetBestMove(Reversi.Board board)
        {
            int i = Reversi.ReversiForm.maxRank + 64;
            int j = -i;
            return GetBestMove(board, currentColor, 1, i, j);
        }

        private Reversi.ReversiForm/ComputerMove GetBestMove(Reversi.Board board, int color, int depth, int alpha, int beta)
        {
            // Constructor
            IL__ldloca(V_0).rank = -color * Reversi.ReversiForm.maxRank;
            int i = board.GetValidMoveCount(color);
            System.Random V_2 = new System.Random();
            int j = V_2.Next(8);
            int k = V_2.Next(8);
            for (int l = 0; l < 8; l++) {
                for (int m = 0; m < 8; m++) {
                    int n = (j + l) % 8;
                    int o = (k + m) % 8;
                    if (board.IsValidMove(color, n, o)) {
                        if (depth == 1) {
                            IL__pop(BeginInvoke(new Reversi.ReversiForm/UpdateStatusProgressDelegate(this, IL__ldftn(UpdateStatusProgress()))));
                        }
                        // Constructor
                        Reversi.Board V_10 = new Reversi.Board(board);
                        V_10.MakeMove(color, IL__ldloca(V_9).row, IL__ldloca(V_9).col);
                        int p = V_10.WhiteCount - V_10.BlackCount;
                        int q = -color;
                        int r = 0;
                        bool flag = false;
                        int s = V_10.GetValidMoveCount(q);
                        if (!s) {
                            r = color;
                            q = -q;
                            if (!V_10.HasAnyValidMove(q)) {
                                flag = true;
                            }
                        }
                        if (flag || depth == lookAheadDepth) {
                            if (flag) {
                                if (p < 0) {
                                    IL__ldloca(V_9).rank = -Reversi.ReversiForm.maxRank + p;
                                    goto BasicBlock_561;
                                }
                                if (p > 0) {
                                    IL__ldloca(V_9).rank = Reversi.ReversiForm.maxRank + p;
                                    goto BasicBlock_561;
                                }
                                IL__ldloca(V_9).rank = 0;
                                goto BasicBlock_561;
                            }
                            IL__ldloca(V_9).rank = forfeitWeight * r + frontierWeight * (V_10.BlackFrontierCount - V_10.WhiteFrontierCount) + mobilityWeight * color * (i - s) + stabilityWeight * (V_10.WhiteSafeCount - V_10.BlackSafeCount) + p;
                        } else {
                            Reversi.ReversiForm/ComputerMove V_16 = GetBestMove(V_10, q, depth + 1, alpha, beta);
                            IL__ldloca(V_9).rank = IL__ldloca(V_16).rank;
                            if (r && Math.Abs(IL__ldloca(V_9).rank) < Reversi.ReversiForm.maxRank) {
                                expr1CA = IL__ldloca(V_9);
                                expr1CA.rank = expr1CA.rank + forfeitWeight * r;
                            }
                            if (color == Reversi.Board.White && IL__ldloca(V_9).rank > beta) {
                                IL__starg(beta, IL__ldloca(V_9).rank);
                            }
                            if (color == Reversi.Board.Black && IL__ldloca(V_9).rank < alpha) {
                                IL__starg(alpha, IL__ldloca(V_9).rank);
                            }
                        }
                        BasicBlock_561:
                        if (color == Reversi.Board.White && IL__ldloca(V_9).rank > alpha) {
                            IL__ldloca(V_9).rank = alpha;
                            return V_9;
                        }
                        if (color == Reversi.Board.Black && IL__ldloca(V_9).rank < beta) {
                            IL__ldloca(V_9).rank = beta;
                            return V_9;
                        }
                        if (IL__ldloca(V_0).row < 0) {
                            Reversi.ReversiForm/ComputerMove V_0 = V_9;
                            goto BasicBlock_572;
                        }
                        if (color * IL__ldloca(V_9).rank > color * IL__ldloca(V_0).rank) {
                            V_0 = V_9;
                        }
                    }
                    BasicBlock_572:
                }
            }
            return V_0;
        }

        private void SetAIParameters()
        {
            {
                Reversi.ReversiForm/Difficulty V_0 = options.Difficulty;
                IL__switch(Decompiler.ByteCode[], V_0);
                forfeitWeight = 0;
                frontierWeight = 0;
                mobilityWeight = 0;
                stabilityWeight = 0;
                goto BasicBlock_720;
            }
            {
                forfeitWeight = 2;
                frontierWeight = 1;
                mobilityWeight = 0;
                stabilityWeight = 3;
                goto BasicBlock_720;
            }
            {
                forfeitWeight = 3;
                frontierWeight = 1;
                mobilityWeight = 0;
                stabilityWeight = 5;
                goto BasicBlock_720;
            }
            {
                forfeitWeight = 7;
                frontierWeight = 2;
                mobilityWeight = 1;
                stabilityWeight = 10;
                goto BasicBlock_720;
            }
            {
                forfeitWeight = 35;
                frontierWeight = 10;
                mobilityWeight = 5;
                stabilityWeight = 50;
                goto BasicBlock_720;
            }
            {
                BasicBlock_720:
                lookAheadDepth = options.Difficulty + 3;
                if (moveNumber < 55 - options.Difficulty) goto BasicBlock_723; 
                lookAheadDepth = board.EmptyCount;
                BasicBlock_723:
                return;
            }
        }

        private void RestoreGameAt(int n)
        {
            Reversi.ReversiForm/MoveRecord V_0 = IL__unbox.any(Reversi.ReversiForm/MoveRecord, moveHistory.Item);
            StopMoveAnimation();
            UnHighlightSquares();
            board = new Reversi.Board((IL__ldloca(V_0).board));
            UpdateBoardDisplay();
            currentColor = IL__ldloca(V_0).currentColor;
            moveListView.Items.Clear();
            for (int i = 0; i < n; i++) {
                V_0 = IL__unbox.any(Reversi.ReversiForm/MoveRecord, moveHistory.Item);
                IL__pop(moveListView.Items.Add(IL__ldloca(V_0).moveListItem));
            }
            if (moveListView.Items.Count > 0) {
                moveListView.EnsureVisible(moveListView.Items.Count - 1);
            } else {
                moveListView.Refresh();
            }
            moveNumber = n + 1;
            exprD0 = undoMoveMenuItem;
            exprD6 = undoAllMovesMenuItem;
            exprE7 = playToolBar.Buttons.Item;
            exprF8 = playToolBar.Buttons.Item;
            expr106 = moveNumber > 1;
            bool flag = expr106;
            exprF8.Enabled = expr106;
            expr10E = flag;
            bool flag2 = expr10E;
            exprE7.Enabled = expr10E;
            expr116 = flag2;
            bool flag3 = expr116;
            exprD6.Enabled = expr116;
            exprD0.Enabled = flag3;
            expr126 = redoMoveMenuItem;
            expr12C = redoAllMovesMenuItem;
            expr13D = playToolBar.Buttons.Item;
            expr14E = playToolBar.Buttons.Item;
            expr166 = moveNumber < moveHistory.Count;
            bool flag4 = expr166;
            expr14E.Enabled = expr166;
            expr170 = flag4;
            bool flag5 = expr170;
            expr13D.Enabled = expr170;
            expr17A = flag5;
            bool flag6 = expr17A;
            expr12C.Enabled = expr17A;
            expr126.Enabled = flag6;
            expr18A = resumePlayMenuItem;
            expr19B = playToolBar.Buttons.Item;
            expr1A1 = 0;
            bool flag7 = expr1A1;
            expr19B.Enabled = expr1A1;
            expr18A.Enabled = flag7;
            isComputerPlaySuspended = 1;
        }

        private void UndoMove(bool undoAll)
        {
            {
                Reversi.ReversiForm/GameState V_0 = gameState;
                KillComputerMoveThread();
                expr13 = board;
                Reversi.Board V_1 = expr13;
                Threading.Monitor.Enter(expr13);
                if (moveHistory.Count >= moveNumber) goto BasicBlock_763; 
                IL__pop(moveHistory.Add(IL__box(Reversi.ReversiForm/MoveRecord, new Reversi.ReversiForm/MoveRecord((board), (-lastMoveColor), (new System.Windows.Forms.ListViewItem())))));
                BasicBlock_763:
                expr55 = this;
                if (undoAll) goto BasicBlock_767; 
                expr60 = moveNumber - 2;
                goto BasicBlock_768;
                BasicBlock_767:
                expr63 = 0;
                BasicBlock_768:
                expr55.RestoreGameAt(expr60);
                IL__leave(IL_72);
            }
            Threading.Monitor.Exit(V_1);
            IL__endfinally();
            goto BasicBlock_771;
            {
                BasicBlock_771:
                if (V_0) goto BasicBlock_773; 
                statistics = new Reversi.Statistics((oldStatistics));
                StartGame(1);
                return;
                BasicBlock_773:
                StartTurn();
                return;
            }
        }

        private void RedoMove(bool redoAll)
        {
            expr00 = this;
            if (!redoAll) {
                expr05 = moveNumber;
            } else {
                expr18 = moveHistory.Count - 1;
            }
            expr00.RestoreGameAt(expr05);
            StartTurn();
        }

        private void UpdateBoardDisplay()
        {
            expr01 = blackCountLabel;
            int k = board.BlackCount;
            expr01.Text = IL__ldloca(k).ToString();
            blackCountLabel.Refresh();
            expr2A = whiteCountLabel;
            int l = board.WhiteCount;
            expr2A.Text = IL__ldloca(l).ToString();
            whiteCountLabel.Refresh();
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    Reversi.SquareControl V_0 = (Reversi.SquareControl)squaresPanel.Controls.Item;
                    V_0.Contents = board.GetSquareContents(i, j);
                    V_0.PreviewContents = Reversi.Board.Empty;
                }
            }
            squaresPanel.Refresh();
        }

        private void HighlightValidMoves()
        {
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    Reversi.SquareControl V_0 = (Reversi.SquareControl)squaresPanel.Controls.Item;
                    if (board.IsValidMove(currentColor, i, j)) {
                        V_0.IsValid = 1;
                    } else {
                        V_0.IsValid = 0;
                    }
                }
            }
        }

        private void UnHighlightSquares()
        {
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    Reversi.SquareControl V_0 = (Reversi.SquareControl)squaresPanel.Controls.Item;
                    V_0.IsActive = 0;
                    V_0.IsValid = 0;
                    V_0.IsNew = 0;
                }
            }
        }

        private void SetSquareControlColors()
        {
            Reversi.SquareControl.ActiveSquareBackColor = options.ActiveSquareColor;
            Reversi.SquareControl.NormalBackColor = options.BoardColor;
            Reversi.SquareControl.MoveIndicatorColor = options.MoveIndicatorColor;
            Reversi.SquareControl.ValidMoveBackColor = options.ValidMoveColor;
        }

        private void LoadProgramSettings()
        {
            {
                int i = Int32.Parse(settings.GetValue("Window", "Left"));
                int j = Int32.Parse(settings.GetValue("Window", "Top"));
                int k = Int32.Parse(settings.GetValue("Window", "Width"));
                int l = Int32.Parse(settings.GetValue("Window", "Height"));
                StartPosition = 0;
                DesktopLocation = new System.Drawing.Point(i, j);
                ClientSize = new System.Drawing.Size(k, l);
                options.ShowValidMoves = Boolean.Parse(settings.GetValue("Options", "ShowValidMoves"));
                options.PreviewMoves = Boolean.Parse(settings.GetValue("Options", "PreviewMoves"));
                options.AnimateMoves = Boolean.Parse(settings.GetValue("Options", "AnimateMoves"));
                options.BoardColor = Drawing.Color.FromArgb(Int32.Parse(settings.GetValue("Options", "BoardColor")));
                options.ValidMoveColor = Drawing.Color.FromArgb(Int32.Parse(settings.GetValue("Options", "ValidMoveColor")));
                options.ActiveSquareColor = Drawing.Color.FromArgb(Int32.Parse(settings.GetValue("Options", "ActiveSquareColor")));
                options.MoveIndicatorColor = Drawing.Color.FromArgb(Int32.Parse(settings.GetValue("Options", "MoveIndicatorColor")));
                options.FirstMove = Int32.Parse(settings.GetValue("Options", "FirstMove"));
                options.ComputerPlaysBlack = Boolean.Parse(settings.GetValue("Options", "ComputerPlaysBlack"));
                options.ComputerPlaysWhite = Boolean.Parse(settings.GetValue("Options", "ComputerPlaysWhite"));
                options.Difficulty = IL__unbox.any(Reversi.ReversiForm/Difficulty, Enum.Parse(IL__box(Reversi.ReversiForm/Difficulty, options.Difficulty).GetType(), settings.GetValue("Options", "Difficulty"), 1));
                SetSquareControlColors();
                statistics.BlackWins = Int32.Parse(settings.GetValue("Statistics", "BlackWins"));
                statistics.WhiteWins = Int32.Parse(settings.GetValue("Statistics", "WhiteWins"));
                statistics.OverallDraws = Int32.Parse(settings.GetValue("Statistics", "OverallDraws"));
                statistics.BlackTotalScore = Int32.Parse(settings.GetValue("Statistics", "BlackTotalScore"));
                statistics.WhiteTotalScore = Int32.Parse(settings.GetValue("Statistics", "WhiteTotalScore"));
                statistics.ComputerWins = Int32.Parse(settings.GetValue("Statistics", "ComputerWins"));
                statistics.UserWins = Int32.Parse(settings.GetValue("Statistics", "UserWins"));
                statistics.VsComputerDraws = Int32.Parse(settings.GetValue("Statistics", "VsComputerDraws"));
                statistics.ComputerTotalScore = Int32.Parse(settings.GetValue("Statistics", "ComputerTotalScore"));
                statistics.UserTotalScore = Int32.Parse(settings.GetValue("Statistics", "UserTotalScore"));
                IL__leave(IL_3D0);
            }
            {
                IL__pop(expr3CD);
                IL__leave(IL_3D0);
            }
        }

        private void SaveProgramSettings()
        {
            expr01 = settings;
            expr06 = "Window";
            expr0B = "Left";
            int i = IL__ldflda(windowSettings, this).Left;
            expr01.SetValue(expr06, expr0B, IL__ldloca(i).ToString());
            expr29 = settings;
            expr2E = "Window";
            expr33 = "Top";
            int j = IL__ldflda(windowSettings, this).Top;
            expr29.SetValue(expr2E, expr33, IL__ldloca(j).ToString());
            expr51 = settings;
            expr56 = "Window";
            expr5B = "Width";
            int k = IL__ldflda(windowSettings, this).Width;
            expr51.SetValue(expr56, expr5B, IL__ldloca(k).ToString());
            expr79 = settings;
            expr7E = "Window";
            expr83 = "Height";
            int l = IL__ldflda(windowSettings, this).Height;
            expr79.SetValue(expr7E, expr83, IL__ldloca(l).ToString());
            settings.SetValue("Options", "ShowValidMoves", IL__ldflda(ShowValidMoves, options).ToString());
            settings.SetValue("Options", "PreviewMoves", IL__ldflda(PreviewMoves, options).ToString());
            settings.SetValue("Options", "AnimateMoves", IL__ldflda(AnimateMoves, options).ToString());
            expr110 = settings;
            expr115 = "Options";
            expr11A = "BoardColor";
            int m = IL__ldsflda(NormalBackColor).ToArgb();
            expr110.SetValue(expr115, expr11A, IL__ldloca(m).ToString());
            expr138 = settings;
            expr13D = "Options";
            expr142 = "ValidMoveColor";
            int n = IL__ldsflda(ValidMoveBackColor).ToArgb();
            expr138.SetValue(expr13D, expr142, IL__ldloca(n).ToString());
            expr160 = settings;
            expr165 = "Options";
            expr16A = "ActiveSquareColor";
            int o = IL__ldsflda(ActiveSquareBackColor).ToArgb();
            expr160.SetValue(expr165, expr16A, IL__ldloca(o).ToString());
            expr188 = settings;
            expr18D = "Options";
            expr192 = "MoveIndicatorColor";
            int p = IL__ldsflda(MoveIndicatorColor).ToArgb();
            expr188.SetValue(expr18D, expr192, IL__ldloca(p).ToString());
            settings.SetValue("Options", "FirstMove", IL__ldflda(FirstMove, options).ToString());
            settings.SetValue("Options", "ComputerPlaysBlack", IL__ldflda(ComputerPlaysBlack, options).ToString());
            settings.SetValue("Options", "ComputerPlaysWhite", IL__ldflda(ComputerPlaysWhite, options).ToString());
            settings.SetValue("Options", "Difficulty", IL__box(Reversi.ReversiForm/Difficulty, options.Difficulty).ToString());
            settings.SetValue("Statistics", "BlackWins", IL__ldflda(BlackWins, statistics).ToString());
            settings.SetValue("Statistics", "WhiteWins", IL__ldflda(WhiteWins, statistics).ToString());
            settings.SetValue("Statistics", "OverallDraws", IL__ldflda(OverallDraws, statistics).ToString());
            settings.SetValue("Statistics", "BlackTotalScore", IL__ldflda(BlackTotalScore, statistics).ToString());
            settings.SetValue("Statistics", "WhiteTotalScore", IL__ldflda(WhiteTotalScore, statistics).ToString());
            settings.SetValue("Statistics", "ComputerWins", IL__ldflda(ComputerWins, statistics).ToString());
            settings.SetValue("Statistics", "UserWins", IL__ldflda(UserWins, statistics).ToString());
            settings.SetValue("Statistics", "VsComputerDraws", IL__ldflda(VsComputerDraws, statistics).ToString());
            settings.SetValue("Statistics", "ComputerTotalScore", IL__ldflda(ComputerTotalScore, statistics).ToString());
            settings.SetValue("Statistics", "UserTotalScore", IL__ldflda(UserTotalScore, statistics).ToString());
            settings.Save();
        }

        private void ReversiForm_Closed(object sender, System.EventArgs e)
        {
            KillComputerMoveThread();
            SaveProgramSettings();
        }

        private void ReversiForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool flag = true;
            if (gameState) {
                Reversi.ConfirmDialog V_1 = new Reversi.ConfirmDialog("Exit the program?");
                if (V_1.ShowDialog(this) != 6) {
                    flag = false;
                }
                V_1.Dispose();
            }
            if (!flag) {
                e.Cancel = 1;
            }
        }

        private void ReversiForm_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            {
                if (gameState == 2) goto BasicBlock_940; 
                return;
                BasicBlock_940:
                char V_3 = e.KeyChar;
                string V_0 = IL__ldloca(V_3).ToString().ToUpper();
                int i = Reversi.ReversiForm.alpha.IndexOf(V_0);
                int j = Int32.Parse(V_0) - 1;
                IL__leave(IL_3A);
            }
            {
                IL__pop(expr35);
                j = -1;
                IL__leave(IL_3A);
            }
            {
                if (i < 0) goto BasicBlock_947; 
                if (i >= 8) goto BasicBlock_947; 
                keyedColNumber = i;
                keyedRowNumber = -1;
                return;
                BasicBlock_947:
                if (keyedColNumber < 0) goto BasicBlock_949; 
                if (keyedColNumber <= 7) goto BasicBlock_950; 
                BasicBlock_949:
                return;
                BasicBlock_950:
                if (j < 0) goto BasicBlock_952; 
                if (j <= 7) goto BasicBlock_953; 
                BasicBlock_952:
                keyedColNumber = -1;
                keyedRowNumber = -1;
                return;
                BasicBlock_953:
                keyedRowNumber = j;
                if (!board.IsValidMove(currentColor, keyedRowNumber, keyedColNumber)) goto BasicBlock_956; 
                MakePlayerMove(keyedRowNumber, keyedColNumber);
                return;
                BasicBlock_956:
                keyedColNumber = -1;
                keyedRowNumber = -1;
                return;
            }
        }

        private void ReversiForm_Move(object sender, System.EventArgs e)
        {
            if (!WindowState) {
                expr09 = IL__ldflda(windowSettings, this);
                System.Drawing.Point V_0 = DesktopLocation;
                expr09.X = IL__ldloca(V_0).X;
                expr22 = IL__ldflda(windowSettings, this);
                System.Drawing.Point V_1 = DesktopLocation;
                expr22.Y = IL__ldloca(V_1).Y;
            }
        }

        private void ReversiForm_Resize(object sender, System.EventArgs e)
        {
            int i = Math.Min(squaresPanel.Width, squaresPanel.Height) / 8;
            i = Math.Max(i, 8);
            for (int j = 0; j < 8; j++) {
                for (int k = 0; k < 8; k++) {
                    squareControls[j, k].Width = i;
                    squareControls[j, k].Height = i;
                    squareControls[j, k].Left = k * i;
                    squareControls[j, k].Top = j * i;
                }
            }
            j = 0;
            for (; j < 8; j++) {
                colLabels[j].Width = i;
                colLabels[j].Left = cornerLabel.Width + j * i;
                rowLabels[j].Height = i;
                rowLabels[j].Top = cornerLabel.Height + j * i;
            }
            infoPanel.Height = 8 * i + colLabels[0].Height;
            statusProgressBar.Left = statusLabel.Left + statusLabel.Width;
            if (!WindowState) {
                expr135 = IL__ldflda(windowSettings, this);
                System.Drawing.Size V_3 = ClientSize;
                expr135.Width = IL__ldloca(V_3).Width;
                expr14E = IL__ldflda(windowSettings, this);
                System.Drawing.Size V_4 = ClientSize;
                expr14E.Height = IL__ldloca(V_4).Height;
            }
        }

        private void newGameMenuItem_Click(object sender, System.EventArgs e)
        {
            StartGame();
        }

        private void resignGameMenuItem_Click(object sender, System.EventArgs e)
        {
            bool flag = true;
            if (gameState) {
                Reversi.ConfirmDialog V_1 = new Reversi.ConfirmDialog("Resign this game?");
                if (V_1.ShowDialog(this) != 6) {
                    flag = false;
                }
                V_1.Dispose();
            }
            if (flag) {
                KillComputerMoveThread();
                StopMoveAnimation();
                UnHighlightSquares();
                UpdateBoardDisplay();
                EndGame(1);
            }
        }

        private void optionsMenuItem_Click(object sender, System.EventArgs e)
        {
            {
                Reversi.OptionsDialog V_0 = new Reversi.OptionsDialog((options));
                if (V_0.ShowDialog(this) != 1) goto BasicBlock_1089; 
                options.ShowValidMoves = V_0.Options.ShowValidMoves;
                options.PreviewMoves = V_0.Options.PreviewMoves;
                options.AnimateMoves = V_0.Options.AnimateMoves;
                options.BoardColor = V_0.Options.BoardColor;
                options.ValidMoveColor = V_0.Options.ValidMoveColor;
                options.ActiveSquareColor = V_0.Options.ActiveSquareColor;
                options.MoveIndicatorColor = V_0.Options.MoveIndicatorColor;
                options.Difficulty = V_0.Options.Difficulty;
                SetSquareControlColors();
                if (!gameState) goto BasicBlock_1088; 
                exprE0 = board;
                Reversi.Board V_1 = exprE0;
                Threading.Monitor.Enter(exprE0);
                if (gameState != 1) goto BasicBlock_1071; 
                StopMoveAnimation();
                UpdateBoardDisplay();
                EndMove();
                BasicBlock_1071:
                UnHighlightSquares();
                if (V_0.Options.FirstMove == options.FirstMove) goto BasicBlock_1074; 
                if (lastMoveColor == Reversi.Board.Empty) goto BasicBlock_1076; 
                BasicBlock_1074:
                if (V_0.Options.ComputerPlaysBlack != options.ComputerPlaysBlack) goto BasicBlock_1076; 
                if (V_0.Options.ComputerPlaysWhite == options.ComputerPlaysWhite) goto BasicBlock_1081; 
                BasicBlock_1076:
                KillComputerMoveThread();
                options.FirstMove = V_0.Options.FirstMove;
                options.ComputerPlaysBlack = V_0.Options.ComputerPlaysBlack;
                options.ComputerPlaysWhite = V_0.Options.ComputerPlaysWhite;
                if (lastMoveColor != Reversi.Board.Empty) goto BasicBlock_1079; 
                currentColor = options.FirstMove;
                BasicBlock_1079:
                squaresPanel.Refresh();
                StartTurn();
                goto BasicBlock_1086;
                BasicBlock_1081:
                options.FirstMove = V_0.Options.FirstMove;
                options.ComputerPlaysBlack = V_0.Options.ComputerPlaysBlack;
                options.ComputerPlaysWhite = V_0.Options.ComputerPlaysWhite;
                if (!options.ShowValidMoves) goto BasicBlock_1085; 
                if (IsComputerPlayer(currentColor)) goto BasicBlock_1085; 
                HighlightValidMoves();
                BasicBlock_1085:
                squaresPanel.Refresh();
                BasicBlock_1086:
                IL__leave(IL_29A);
            }
            Threading.Monitor.Exit(V_1);
            IL__endfinally();
            BasicBlock_1088:
            options.FirstMove = V_0.Options.FirstMove;
            options.ComputerPlaysBlack = V_0.Options.ComputerPlaysBlack;
            options.ComputerPlaysWhite = V_0.Options.ComputerPlaysWhite;
            squaresPanel.Refresh();
            BasicBlock_1089:
            V_0.Dispose();
        }

        private void statisticsMenuItem_Click(object sender, System.EventArgs e)
        {
            Reversi.StatisticsDialog V_0 = new Reversi.StatisticsDialog((statistics));
            IL__pop(V_0.ShowDialog(this));
            V_0.Dispose();
        }

        private void exitMenuItem_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void undoMoveMenuItem_Click(object sender, System.EventArgs e)
        {
            UndoMove(0);
        }

        private void redoMoveMenuItem_Click(object sender, System.EventArgs e)
        {
            RedoMove(0);
        }

        private void undoAllMovesmenuItem_Click(object sender, System.EventArgs e)
        {
            UndoMove(1);
        }

        private void redoAllMovesMenuItem_Click(object sender, System.EventArgs e)
        {
            RedoMove(1);
        }

        private void resumePlayMenuItem_Click(object sender, System.EventArgs e)
        {
            expr01 = redoMoveMenuItem;
            expr07 = redoAllMovesMenuItem;
            expr18 = playToolBar.Buttons.Item;
            expr29 = playToolBar.Buttons.Item;
            expr2F = 0;
            bool flag = expr2F;
            expr29.Enabled = expr2F;
            expr37 = flag;
            bool flag2 = expr37;
            expr18.Enabled = expr37;
            expr3F = flag2;
            bool flag3 = expr3F;
            expr07.Enabled = expr3F;
            expr01.Enabled = flag3;
            expr4D = resumePlayMenuItem;
            expr5E = playToolBar.Buttons.Item;
            expr64 = 0;
            bool flag4 = expr64;
            expr5E.Enabled = expr64;
            expr4D.Enabled = flag4;
            gameState = 4;
            isComputerPlaySuspended = 0;
            StartTurn();
        }

        private void helpTopicsMenuItem_Click(object sender, System.EventArgs e)
        {
            System.IO.FileInfo V_0 = new System.IO.FileInfo((Reversi.ReversiForm.helpFileName));
            if (V_0.Exists) {
                Windows.Forms.Help.ShowHelp(this, Reversi.ReversiForm.helpFileName);
                return;
            }
            IL__pop(Windows.Forms.MessageBox.Show(this, String.Format("Help file '{0}' not found.", Reversi.ReversiForm.helpFileName), "File Not Found", 0, 16));
        }

        private void aboutMenuItem_Click(object sender, System.EventArgs e)
        {
            Reversi.AboutDialog V_0 = new Reversi.AboutDialog();
            IL__pop(V_0.ShowDialog(this));
            V_0.Dispose();
        }

        private void playToolBar_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e)
        {
            int i = playToolBar.Buttons.IndexOf(e.Button);
            IL__switch(Decompiler.ByteCode[], i);
            return;
            newGameMenuItem.PerformClick();
            return;
            resignGameMenuItem.PerformClick();
            return;
            undoAllMovesMenuItem.PerformClick();
            return;
            undoMoveMenuItem.PerformClick();
            return;
            resumePlayMenuItem.PerformClick();
            return;
            redoMoveMenuItem.PerformClick();
            return;
            redoAllMovesMenuItem.PerformClick();
        }

        private void SquareControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (gameState != 2) {
                return;
            }
            Reversi.SquareControl V_0 = (Reversi.SquareControl)sender;
            if (board.IsValidMove(currentColor, V_0.Row, V_0.Col)) {
                if (!V_0.IsActive && V_0.PreviewContents == Reversi.Board.Empty) {
                    if (options.ShowValidMoves) {
                        V_0.IsActive = 1;
                        if (!options.PreviewMoves) {
                            V_0.Refresh();
                        }
                    }
                    if (options.PreviewMoves) {
                        Reversi.Board V_1 = new Reversi.Board((board));
                        V_1.MakeMove(currentColor, V_0.Row, V_0.Col);
                        for (int i = 0; i < 8; i++) {
                            for (int j = 0; j < 8; j++) {
                                if (V_1.GetSquareContents(i, j) != board.GetSquareContents(i, j)) {
                                    squareControls[i, j].PreviewContents = V_1.GetSquareContents(i, j);
                                    squareControls[i, j].Refresh();
                                }
                            }
                        }
                    }
                }
                V_0.Cursor = Windows.Forms.Cursors.Hand;
            }
        }

        private void SquareControl_MouseLeave(object sender, System.EventArgs e)
        {
            Reversi.SquareControl V_0 = (Reversi.SquareControl)sender;
            if (V_0.IsActive) {
                V_0.IsActive = 0;
                V_0.Refresh();
            }
            if (V_0.PreviewContents != Reversi.Board.Empty) {
                for (int i = 0; i < 8; i++) {
                    for (int j = 0; j < 8; j++) {
                        if (squareControls[i, j].PreviewContents != Reversi.Board.Empty) {
                            squareControls[i, j].PreviewContents = Reversi.Board.Empty;
                            squareControls[i, j].Refresh();
                        }
                    }
                }
            }
            V_0.Cursor = Windows.Forms.Cursors.Default;
        }

        private void SquareControl_Click(object sender, System.EventArgs e)
        {
            if (gameState != 2) {
                return;
            }
            Reversi.SquareControl V_0 = (Reversi.SquareControl)sender;
            if (board.IsValidMove(currentColor, V_0.Row, V_0.Col)) {
                V_0.Cursor = Windows.Forms.Cursors.Default;
                MakePlayerMove(V_0.Row, V_0.Col);
            }
        }
    }
}
