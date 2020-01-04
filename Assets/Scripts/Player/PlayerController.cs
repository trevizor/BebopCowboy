using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    Rigidbody body;
    Camera camera;
    
    //for mouse rotation
    public float speedH = 2.0f;
    public float speedV = 2.0f;
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    //for movement
    public float walkSpeed = 20f;

    // Start is called before the first frame update
    void Start()
    {
        RaycastHit hit;
        int terrainLayer = LayerMask.GetMask("Terrain");
        int layerMask = 1 << terrainLayer;
        if (Physics.Raycast(gameObject.transform.position, -Vector3.up, out hit, 2000, Physics.DefaultRaycastLayers))
        {
            gameObject.transform.position = new Vector3(gameObject.transform.position.x, (hit.point.y + 4f), gameObject.transform.position.z);
        }

        gameObject.AddComponent<Rigidbody>();
        gameObject.AddComponent<CapsuleCollider>();

        body = gameObject.GetComponent<Rigidbody>();
        camera = gameObject.GetComponentInChildren<Camera>();


        body.freezeRotation = true;



    }

    void Update()
    {
        updateCamera();
        updateMovement();
    }

    void updateMovement ()
    {
        float tempWalkSpeed = walkSpeed;
        if (Input.GetKey("q"))
            tempWalkSpeed = walkSpeed * 10;
        if(Input.GetKey("w"))
            body.velocity = new Vector3 ( camera.transform.forward.x * tempWalkSpeed, body.velocity.y, camera.transform.forward.z * tempWalkSpeed);
        if (Input.GetKey("e"))
            body.velocity = new Vector3(body.velocity.x, 20f, body.velocity.z);
        if (Input.GetKey("r"))
        {
            RenderSettings.fogStartDistance += 5;
            RenderSettings.fogEndDistance += 10;
        }
        if (Input.GetKey("t"))
        {
            RenderSettings.fogStartDistance -= 5;
            RenderSettings.fogEndDistance -= 10;
        }

    }

    void updateCamera()
    {
        yaw += speedH * Input.GetAxis("Mouse X");
        if (pitch - speedV * Input.GetAxis("Mouse Y") > -80 &&
            pitch - speedV * Input.GetAxis("Mouse Y") < 80)
            pitch -= speedV * Input.GetAxis("Mouse Y");

        camera.transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }
}
