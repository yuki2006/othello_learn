﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OthelloCell : MonoBehaviour
{
    int ownerID = -1;

    public Image ChipImage;

    // ゲーム中の座標を表す
    public Vector2 Location;

    public int OwnerID
    {
        get { return ownerID; }
        set
        {
            ownerID = value;
            // 思考用のフィールドのためattachされないオブジェクトを考える必要があるため
            // 初期化されているかどうかを判定する
            if (ChipImage != null)
            {
                ChipImage.color = OthelloBoard.Instance.PlayerChipColors[ownerID + 1];
            }
        }
    }

    public void CellPressed()
    {
        OthelloBoard.Instance.PutCell(this);
    }
}