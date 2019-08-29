using System.Collections;
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
            ChipImage.color = OthelloBoard.Instance.PlayerChipColors[ownerID + 1];
            if (ownerID == -1)
                this.GetComponent<Button>().interactable = true;
            else
                this.GetComponent<Button>().interactable = false;
        }
    }

    public void CellPressed()
    {
        OthelloBoard.Instance.PutCell(this);

        Debug.Log(Location);
    }
}