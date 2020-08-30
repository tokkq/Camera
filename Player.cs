using Ekko.Unity.Extend;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    float _speed = 1f;
    [SerializeField]
    PlayerCamera _camera;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += _camera.transform.forward.SetY(0) * Time.deltaTime * _speed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position += -_camera.transform.forward.SetY(0) * Time.deltaTime * _speed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position += -_camera.transform.right.SetY(0) * Time.deltaTime * _speed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += _camera.transform.right.SetY(0) * Time.deltaTime * _speed;
        }
        var rotateQuaternion = Quaternion.FromToRotation(transform.forward.SetY(0), _camera.transform.forward.SetY(0));
        transform.rotation *= rotateQuaternion;
    }
}
