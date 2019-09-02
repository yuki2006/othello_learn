using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        ScoreBoard.GetComponent<RectTransform>().SetSiblingIndex(BoardSize * BoardSize + 1);
        GameObject.Destroy(Template);
        InitializeGame();
    }

    private void CreateNewCell(int x, int y, float cellAnchorSize)
    {
        GameObject go = GameObject.Instantiate(Template, this.transform);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(x * cellAnchorSize, y * cellAnchorSize);
        r.anchorMax = new Vector2((x + 1) * cellAnchorSize, (y + 1) * cellAnchorSize);
        OthelloCell oc = go.GetComponent<OthelloCell>();
        OthelloCells[x, y] = oc;
        oc.Location.x = x;
        oc.Location.y = y;
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
    bool CheckAndReverse(OthelloCell cell, int dx, int dy, bool doReverse)
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

            if (OthelloCells[x, y].OwnerID == -1)
            {
                // 何も置かれていないので終了
                return false;
            }
            else if (OthelloCells[x, y].OwnerID != CurrentTurn)
            {
                // 相手の駒が置いてある時 「相手の駒を見た」というフラグをたてる
                isChecked = true;
            }
            else if (OthelloCells[x, y].OwnerID == CurrentTurn)
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
                            OthelloCells[sx, sy].OwnerID = CurrentTurn;
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

    bool CheckAndReverse(OthelloCell cell, bool doReverse)
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

                isReverse |= CheckAndReverse(cell, i, j, doReverse);
            }
        }

        return isReverse;
    }

    public void PutCell(OthelloCell cell)
    {
        // どれかが反転されたかどうか オーバーロード
        bool isReverse = CheckAndReverse(cell, true);

        if (isReverse)
        {
            TurnEnd();
        }
    }

    void TurnEnd()
    {
        CurrentTurn = (CurrentTurn + 1) % 2;
        CheckCellEnable();
    }

    // 各セルの有効・無効を判定して切り替える処理
    void CheckCellEnable()
    {
        bool canPut = false;
        // ガイドを計算して表示する
        for (int i = 0; i < BoardSize; i++)
        {
            for (int j = 0; j < BoardSize; j++)
            {
                bool result = CheckAndReverse(OthelloCells[i, j], false);
                // resultがtrueならそこにおける、 falseなら置けない。
                OthelloCells[i, j].GetComponent<Button>().interactable = result;
                if (result)
                {
                    canPut = true;
                }
            }
        }

        // 1回もresult が trueにならなかったらパス
        if (!canPut)
        {
            if (prevPass)
            {
                // 連続してパスがされる
                // 本来ならここでゲーム終了みたいな表示とかを出したい
                Debug.Log("ゲーム終了");
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
}