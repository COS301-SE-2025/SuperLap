using System;
using UnityEngine;

public class BikeAnimation : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float xMove = 5f;

    [SerializeField] private float zRot = 15f;
    [SerializeField] private float yRot = 5f;
    private Vector3 startPos;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3((float)Math.Sin(Time.time * speed) * xMove, 0.0f, 0.0f) + startPos;
        transform.rotation = Quaternion.Euler(0.0f, (float)Math.Sin(Time.time * speed) * yRot, (float)Math.Sin(Time.time * speed) * zRot);
    }
}
