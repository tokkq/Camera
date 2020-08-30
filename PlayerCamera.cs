using Ekko.Unity.Attribute;
using UnityEngine;
public class PlayerCamera : MonoBehaviour
{
    [Header("Editable")]
    [SerializeField]
    Transform _target;

    [SerializeField]
    float _rotateSpeed;

    [SerializeField]
    Vector3 _cameraPositionOffset;
    [SerializeField]
    Vector3 _lookAtPositionOffset;

    [SerializeField]
    CameraState _state;
    [SerializeField]
    CameraTransferState _transferState;

    [Header("ReadOnly")]
    [SerializeField, ReadOnly]
    float _horizonalRotateDegree;
    [SerializeField, ReadOnly]
    float _verticalRotateDegree;

    [SerializeField, ReadOnly]
    Vector3 _destination;
    [SerializeField, ReadOnly]
    Vector3 _destinationLookAt;
    [SerializeField, ReadOnly]
    Vector3 _lookAt;

    [SerializeField, ReadOnly]
    Vector3 _destinationTohit;
    [SerializeField, ReadOnly]
    GameObject _obstacle;

    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            _verticalRotateDegree += Time.deltaTime * _rotateSpeed;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            _verticalRotateDegree -= Time.deltaTime * _rotateSpeed;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            _horizonalRotateDegree += Time.deltaTime * _rotateSpeed;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            _horizonalRotateDegree -= Time.deltaTime * _rotateSpeed;
        }
    }

    void LateUpdate()
    {
        switch (_state)
        {
            case CameraState.None:
                break;
            case CameraState.Chase:
                _updateSphereRotate();
                _updateLookAt();
                _updateDistance();
                break;
        }

        switch (_transferState)
        {
            case CameraTransferState.Warp:
                transform.position = _destination;
                break;
            case CameraTransferState.Lerp:
                transform.position = Vector3.Lerp(transform.position, _destination, .01f);
                break;
            case CameraTransferState.QuartIn:
                //transform.position = Easing.VectorEasing(Ease.QuartIn, , , , );
                break;
        }

        transform.LookAt(_lookAt, Vector3.up);
    }

    void _updateSphereRotate()
    {
        var rotatedCameraOffset = _getSphereRotatedVector(_cameraPositionOffset, _horizonalRotateDegree, _verticalRotateDegree);
        _destination = _target.position + rotatedCameraOffset;
    }
    void _updateLookAt()
    {
        _lookAt = _getLookAtPosition(transform.position, _target.position, _lookAtPositionOffset);
        _destinationLookAt = _getLookAtPosition(_destination, _target.position, _lookAtPositionOffset);
    }
    void _updateDistance()
    {
        var lookAtToDestination = _destination - _destinationLookAt;
        var targetToDestination = _destination - _target.position;

        var obstacleHitPosToLookAt = _getHitPosition(_destinationLookAt - lookAtToDestination, lookAtToDestination * 2f);
        var obstacleHitPosToTarget = _getHitPosition(_target.position, targetToDestination);
        if (obstacleHitPosToTarget.HasValue && obstacleHitPosToLookAt.HasValue)
        {
            _destinationTohit = obstacleHitPosToTarget.Value - _destination;
            _destination += _destinationTohit;
        }
        else if (obstacleHitPosToTarget.HasValue)
        {
            _destinationTohit = obstacleHitPosToTarget.Value - _destination;
            _destination += _destinationTohit;
        }
    }

    Vector3? _getHitPosition(Vector3 origin, Vector3 rayVec)
    {
        var results = new RaycastHit[1];
        Physics.RaycastNonAlloc(new Ray(origin, Vector3.Normalize(rayVec)), results, Vector3.Magnitude(rayVec));
        Debug.DrawRay(origin, rayVec, Color.red, Time.deltaTime);
        if (results[0].transform != null)
        {
            _obstacle = results[0].collider.gameObject;
            return results[0].point;
        }
        else
        {
            _obstacle = null;
            return null;
        }
    }

    Vector3 _getSphereRotatedVector(Vector3 vec, float horizonalDegree, float verticalDegree)
    {
        var rotatedVec = Quaternion.AngleAxis(horizonalDegree, Vector3.up) * vec;

        var verticalVecor = Vector3.Normalize(Vector3.Cross(rotatedVec, Vector3.up));
        rotatedVec = Quaternion.AngleAxis(verticalDegree, verticalVecor) * rotatedVec;

        return rotatedVec;
    }
    Vector3 _getLookAtPosition(Vector3 cameraPosition, Vector3 targetPosition, Vector3 offset)
    {
        var cameraToTargetVec = targetPosition - cameraPosition;
        var virtualVec = Vector3.Normalize(new Vector3(-cameraToTargetVec.z, 0, cameraToTargetVec.x));
        var offsetVec = virtualVec * offset.x + Vector3.up * offset.y + Vector3.forward * offset.z;
        var lootAtPos = cameraPosition + cameraToTargetVec + offsetVec;
        return lootAtPos;
    }
}