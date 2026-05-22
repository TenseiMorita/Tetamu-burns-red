using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    //プレイヤーを見る普通のカメラ
    public Vector3 normalOffset;

    //Npcと話しているときのカメラ
    public Vector3 talkOffset;

    //現在のカメラ座標を保存
    private Vector3 currentOffset;

    //話しているかを図る変数
    public bool isTalking;

    void Start()
    {
        //始まりは普通のカメラ視点
        currentOffset = normalOffset;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (isTalking)
        {
            currentOffset = Vector3.Lerp(currentOffset, talkOffset, Time.deltaTime * 3f);
        }
        else
        {
            currentOffset = Vector3.Lerp(currentOffset, normalOffset, Time.deltaTime * 3f);
        }

        transform.position = target.position + currentOffset;
    }
}
