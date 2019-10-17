using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class OthelloBoard : MonoBehaviour
{
    [NonSerialized] public int CurrentTurn = 1;
    public GameObject ScoreBoard;
    public GameObject Template;
    public int BoardSize = 8;
    public List<Color> PlayerChipColors;
    static OthelloBoard instance;

    // 前回パスしているかどうか
    private bool prevPass = false;

    public static OthelloBoard Instance
    {
        get { return instance; }
    }

    OthelloCell[,] OthelloCells;
    OthelloCell nextCell;

    // 棋譜を保存する変数を用意する。
    List<string> history = new List<string>();

    // ファイルから読み込んだ棋譜のデータ
    string[] loadHistory;
    int loadPointer = 0;

    private int[,] points =
    {
        {100, -50, 0, 0, 0, 0, -50, 100},
        {-50, -50, 0, 0, 0, 0, -50, -50},
        {0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 0, 0, 0, 0, 0},
        {-50, -50, 0, 0, 0, 0, -50, -50},
        {100, -50, 0, 0, 0, 0, -50, 100},
    };

    private OthelloCell[,] currentField;

    void Start()
    {
        currentField = new OthelloCell[BoardSize, BoardSize];
        for (int i = 0; i < BoardSize; i++)
        {
            for (int j = 0; j < BoardSize; j++)
            {
                currentField[i, j] = new OthelloCell();
            }
        }

        instance = this;
        OthelloBoardIsSquareSize();

        OthelloCells = new OthelloCell[BoardSize, BoardSize];
        float cellAnchorSize = 1.0f / BoardSize;
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                CreateNewCell(x, y, cellAnchorSize);
            }
        }

        // Nextを出す
        CreateNewCell(10, 6, cellAnchorSize, false);

        ScoreBoard.GetComponent<RectTransform>().SetSiblingIndex(BoardSize * BoardSize + 1);

        GameObject.Destroy(Template);
        InitializeGame();
    }

    private void CreateNewCell(int x, int y, float cellAnchorSize, bool gameCell = true)
    {
        GameObject go = GameObject.Instantiate(Template, this.transform);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(x * cellAnchorSize, y * cellAnchorSize);
        r.anchorMax = new Vector2((x + 1) * cellAnchorSize, (y + 1) * cellAnchorSize);

        OthelloCell oc = go.GetComponent<OthelloCell>();
        if (gameCell)
        {
            OthelloCells[x, y] = oc;
            oc.Location.x = x;
            oc.Location.y = y;
        }
        else
        {
            nextCell = oc;
        }
    }

    private void OthelloBoardIsSquareSize()
    {
        RectTransform rect = this.GetComponent<RectTransform>();
        if (Screen.width > Screen.height)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.height);
        }
        else
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.width);
    }

    public void InitializeGame()
    {
        ScoreBoard.gameObject.SetActive(false);
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                OthelloCells[x, y].OwnerID = -1;
            }
        }

        OthelloCells[3, 3].OwnerID = 1;
        OthelloCells[4, 4].OwnerID = 1;
        OthelloCells[4, 3].OwnerID = 0;
        OthelloCells[3, 4].OwnerID = 0;
        CheckCellEnable();
    }


// ひっくり返せたら trueを返す ひっくり返せなかったらfalseを返す
    bool CheckAndReverse(OthelloCell[,] field, OthelloCell cell, int dx, int dy, int turn, bool doReverse)
    {
        // 一つ右横がOwnerIDが違うかどうか x,yは確認したいセル
        // 添字はint 型しか使えないので キャスト というもので変換している。
        int x = (int) cell.Location.x;
        int y = (int) cell.Location.y;

        // 相手の駒を見たかどうかの変数
        bool isChecked = false;

        while (true)
        {
            // 1つとなりにする。
            x += dx;
            y += dy;
            if (x >= 0 && x < BoardSize && y >= 0 && y < BoardSize)
            {
                // 何もしない
            }
            else
            {
                // ゲームの範囲外だったら終了
                return false;
            }

            if (field[x, y].OwnerID == -1)
            {
                // 何も置かれていないので終了
                return false;
            }
            else if (field[x, y].OwnerID != turn)
            {
                // 相手の駒が置いてある時 「相手の駒を見た」というフラグをたてる
                isChecked = true;
            }
            else if (field[x, y].OwnerID == turn)
            {
                // 自分の色の場合
                // すでに相手を見ていた場合
                if (isChecked)
                {
                    if (doReverse)
                    {
                        // 囲めるので置いたマスから一つ左のマスを自分の色にする
                        int sx = (int) cell.Location.x;
                        int sy = (int) cell.Location.y;
                        while (sx != x || sy != y)
                        {
                            // [x,y]じゃないのに注意
                            field[sx, sy].OwnerID = turn;
                            sx += dx;
                            sy += dy;
                        }
                    }

                    // ひっくり返せるときはtrueにして関数を抜ける
                    return true;
                }
                else
                {
                    // フラグが立っていなかったら、囲めないので処理終了
                    return false;
                }
            }
        }
    }

    bool CheckAndReverse(OthelloCell[,] field, OthelloCell cell, int turn, bool doReverse)
    {
        bool isReverse = false;
        if (cell.OwnerID != -1)
        {
            // すでに置かれていたところは置けない
            return false;
        }

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0)
                {
                    continue;
                }

                isReverse |= CheckAndReverse(field, cell, i, j, turn, doReverse);
            }
        }

        return isReverse;
    }

    public void PutCell(OthelloCell cell, bool isHuman = true)
    {
        if (isHuman)
        {
            // 人間が打った時
            if (CurrentTurn == 0)
            {
                // 0 (白)のときは無効にする
                return;
            }
        }

        // どれかが反転されたかどうか オーバーロード（多重定義で実現している）
        bool isReverse = CheckAndReverse(OthelloCells, cell, CurrentTurn, true);

        if (isReverse)
        {
            int x = (int) cell.Location.x;
            int y = (int) cell.Location.y;

            char xPos = (char) ('A' + x);
            string yPos = (8 - y).ToString();
            history.Add(xPos + yPos);
            TurnEnd();
        }
    }

    void TurnEnd()
    {
        CurrentTurn = (CurrentTurn + 1) % 2;
        CheckCellEnable();
        if (CurrentTurn == 0)
        {
            // 4つ目引数は何手先までを読むか
            AIResult result = Recursive(OthelloCells, CurrentTurn, false, 7);
            if (result.cell == null)
            {
                // null の場合は、CPUの番はパス
                return;
            }

            PutCell(result.cell, false);
        }
    }

    // AIの処理用の再帰関数の戻り値のためのクラス
    class AIResult
    {
        public int point; // そのときの点数
        public OthelloCell cell; // そのときに選んだセル
    }

    // 引数の組み合わせで、そのときのturnから見たときの点数が戻り値
    AIResult Recursive(OthelloCell[,] aiField, int turn, bool prevPass, int depth)
    {
        List<OthelloCell> cells = GetEnableCells(aiField, turn);
        if (cells.Count == 0)
        {
            // 打てるところがない
            if (prevPass)
            {
                // 前回パスしていたら、連続パスなのでゲーム終了 （AIが思考上でゲームが終わったときの判定も含む）
                // 白と黒の数をカウントする
                int white = 0;
                int black = 0;
                for (int i = 0; i < BoardSize; i++)
                {
                    for (int j = 0; j < BoardSize; j++)
                    {
                        if (aiField[i, j].OwnerID == 0)
                        {
                            white++;
                        }
                        else if (aiField[i, j].OwnerID == 1)
                        {
                            black++;
                        }
                    }
                }

                if ((turn == 0 && white > black) ||
                    (turn == 1 && white < black))
                {
                    return new AIResult {point = 99999}; // 勝ちなのでスコアとしてはでかい数を返す
                }

                return new AIResult {point = -99999}; // 負けなのでスコアとして小さい数を返す
            }
            else
            {
                // 前回パスしていなかったら 普通にパスする（ターンを変えて関数を呼ぶ）
                // 相手のターンでの点数が自分の逆なのでマイナスにする
                // 一旦AIResult型で受け取ってポイント部分をマイナスにして新しく返す
                AIResult result = Recursive(aiField, (turn + 1) % 2, true, depth);
                return new AIResult {point = -result.point};
            }
        }
        else
        {
            // 打てるところがあるので打っていく
            // 一番点数が高いものを選ぶ
            int maxPoint = int.MinValue;
            int maxIndex = 0;


            for (int index = 0; index < cells.Count; index++)
            {
                // 引数で与えられた盤面をコピーする
                for (int i = 0; i < BoardSize; i++)
                {
                    for (int j = 0; j < BoardSize; j++)
                    {
                        currentField[i, j].OwnerID = aiField[i, j].OwnerID;
                    }
                }

                // その選択でひっくり返えす
                CheckAndReverse(currentField, cells[index], turn, true);
                // 打った場所の点数
                int point = points[(int) cells[index].Location.y, (int) cells[index].Location.x];
                // 1手先、打つ処理に渡す　depthを1引いているのがポイント
                // Recursiveの呼び出しでは相手の点数によって自分の点数が下がるので 引く
                // 例えば、今、黒なら　ターンが変わって白が打つ → 戻り値は白が打ったときの点数→ 黒から見ると逆の点数になるので引いている
                if (depth > 0)
                {
                    point -= Recursive(currentField, (turn + 1) % 2, false, depth - 1).point;
                }

                if (maxPoint < point)
                {
                    // 点数の更新をする、その時、何番目を選んだかも保持する
                    maxPoint = point;
                    maxIndex = index;
                }
            }

            // 計算した点数と、そのときに選ぶセル返すようにする
            return new AIResult {point = maxPoint, cell = cells[maxIndex]};
        }
    }

// 置けるところをリストとして返す関数
    List<OthelloCell> GetEnableCells(OthelloCell[,] field, int turn)
    {
        List<OthelloCell> ret = new List<OthelloCell>();
        for (int i = 0; i < BoardSize; i++)
        {
            for (int j = 0; j < BoardSize; j++)
            {
                bool result = CheckAndReverse(field, OthelloCells[i, j], turn, false);
                if (result)
                {
                    ret.Add(OthelloCells[i, j]);
                }
            }
        }

        return ret;
    }

// 各セルの有効・無効を判定して切り替える処理
    void CheckCellEnable()
    {
// 次おく色を表示する
        nextCell.OwnerID = CurrentTurn;

// いったんすべてのセルを無効にしておく
        for (int i = 0;
            i < BoardSize;
            i++)
        {
            for (int j = 0; j < BoardSize; j++)
            {
                OthelloCells[i, j].GetComponent<Button>().interactable = false;
            }
        }

// ガイドを計算して表示する
        List<OthelloCell> cellList = GetEnableCells(OthelloCells, CurrentTurn);
        // そのターンで置けるものの一覧を取得しているので
        // このループは置けるものだけ入っている
        foreach (OthelloCell cell in cellList)
        {
            int i = (int) cell.Location.x;
            int j = (int) cell.Location.y;
            OthelloCells[i, j].GetComponent<Button>().interactable = true;
        }

// パスの条件は置けるセルの数が0個の時
        if (cellList.Count == 0)
        {
            if (prevPass)
            {
                // 連続してパスがされるので終了
                // 本来ならここでゲーム終了みたいな表示とかを出したい
                Debug.Log("ゲーム終了");
                int white = 0;
                int black = 0;
                for (int i = 0; i < BoardSize; i++)
                {
                    for (int j = 0; j < BoardSize; j++)
                    {
                        if (OthelloCells[i, j].OwnerID == 0)
                        {
                            white++;
                        }
                        else if (OthelloCells[i, j].OwnerID == 1)
                        {
                            black++;
                        }
                    }
                }

                Debug.Log($"黒{black}個 白{white}個");
                string data = string.Join("\r\n", history);
                File.WriteAllText(Application.dataPath + "/kifu.txt", data);
                return;
            }

            // パス prevPassはTurnEndの前で処理する
            prevPass = true;
            TurnEnd();
        }

        else
        {
            // ガイドがあるのでパスではない。
            prevPass = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // F1を押すと棋譜読み込みモードする
            // 棋譜データをファイルから開く
            loadHistory = File.ReadAllLines(Application.dataPath + "/kifu.txt");
            Debug.Log("ファイルを読み込みました");
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (loadHistory != null)
            {
                if (loadPointer < loadHistory.Length)
                {
                    string cell = loadHistory[loadPointer]; // "C4" とかの文字列
                    loadPointer++; // 1手見たので増やす
                    // string の
                    // cell[0] → 'C' などのcharの文字になる
                    int x = cell[0] - 'A';
                    int y = 8 - (cell[1] - '0');
                    PutCell(OthelloCells[x, y], false);
                }
            }
        }
    }
}