using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private Vector3 startPosition;
    [Range(0,100)]
    public float slider = 0f;
    public float slide_max = 80;
    public bool move;
    private float delta;
    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = startPosition + slider * new Vector3(-1, 1, -1);
        if (move)
        {
            slider += delta;
            if (slider < slide_max)
            {
                delta += 0.001f;
                delta = Mathf.Min(delta, 0.1f);
            }
            else
            {
                delta -= 0.001f;
                delta = Mathf.Max(delta, 0f);
            }
        }
    }
}
