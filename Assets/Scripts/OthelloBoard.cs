﻿using System;
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

    void Start()
    {
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
            // ゲーム木　で　AIが1手目をどこまで打ったかを管理する
            int aiSelect = 0;
            int aiTurn = CurrentTurn;

            // 思考のために今の盤面からコピーしたテーブルを作る
            // コンピュータから見て人間の指すのを考えるまでする
            OthelloCell[,] currentField = new OthelloCell[BoardSize, BoardSize];
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    currentField[i, j] = new OthelloCell();
                    currentField[i, j].Location = OthelloCells[i, j].Location;
                    currentField[i, j].OwnerID = OthelloCells[i, j].OwnerID;
                }
            }

            // コンピューターに打って欲しいタイミング
            // 有効なマスを取得する
            List<OthelloCell> cells = GetEnableCells(currentField, aiTurn);
            if (cells.Count == 0)
            {
                // ゲーム終了
                return;
            }

            // AIが1手目を打つ この1手目は打てる手をすべて、一旦評価値を考えずに打つ 
            CheckAndReverse(currentField, cells[aiSelect], aiTurn, true);

            // AIから見た相手（人間）が打てる可能性を取得する
            List<OthelloCell> canCells = GetEnableCells(currentField, aiTurn);
            if (canCells.Count == 0)
            {
                // ゲーム終了
                return;
            }


            // int.MinValueはintで表す最小の値という意味で必ず一番小さい

            int max = int.MinValue; // ループの中で評価値自体の最大値を保持する
            int maxIndex = 0; // 最大値を更新した時に、それが何番目だったかを保持する。
            // 有効なものから1つ選んで評価値と照らし合わせる
            for (int i = 0; i < canCells.Count; i++)
            {
                int y = (int) canCells[i].Location.y;
                int x = (int) canCells[i].Location.x;
                if (max < points[y, x])
                {
                    max = points[y, x];
                    maxIndex = i;
                }
            }


            PutCell(cells[maxIndex], false);
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
        for (int i = 0; i < BoardSize; i++)
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