using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{

    //プレイヤー移動速度
    public float speed = 5f;

    //CharacterControllerを入れる箱
    private CharacterController controller;

    //Playerのanimatorを保存する場所
    private Animator animator;

    //canMove...動けないようにする
    //bool...ここでcanMoveの切り替えをできるようにする
    public bool canMove = true;

    void Start()
    {

        //プレイヤーについているCharacterControllerを探して保存
        controller = GetComponent<CharacterController>();

        //animator取得、子オブジェクト含めて探す。
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Apply gravity even when movement is locked so they don't float!
        if (!canMove)
        {
            if (controller != null && controller.enabled && controller.gameObject.activeInHierarchy)
            {
                Vector3 gravDir = new Vector3(0f, -9.81f, 0f);
                controller.Move(gravDir * Time.deltaTime);
            }
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }
            return;
        }

        // Horizontal input
        float move = Input.GetAxisRaw("Horizontal");

        // Move horizontally and apply gravity vertically to stay grounded
        Vector3 dir = new Vector3(move * speed, -9.81f, 0f);

        if (controller != null && controller.enabled && controller.gameObject.activeInHierarchy)
        {
            controller.Move(dir * Time.deltaTime);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(move));
        }

        // Rotate character
        if (move > 0)
        {
            transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else if (move < 0)
        {
            transform.rotation = Quaternion.Euler(0, -90, 0);
        }
    }
}
