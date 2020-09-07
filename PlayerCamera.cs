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
    float _cameraWallMinDistance = .5f;
    [SerializeField]
    float _cameraPlayerMinDistance = .5f;
    [SerializeField]
    float _cameraPlayerDistance = 1f;
    [SerializeField]
    float _zoomRateOnAim = 1f;

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
    float _currentZoomRate;

    [SerializeField, ReadOnly]
    Vector3 _destination;
    [SerializeField, ReadOnly]
    Vector3 _destinationLookAt;
    [SerializeField, ReadOnly]
    float _destinationZoomOffset;
    [SerializeField, ReadOnly]
    Vector3 _lookAt;

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
                _updateWall();
                break;
            case CameraState.Aim:
                _updateSphereRotate();
                _updateLookAt();
                _zoom(_zoomRateOnAim);
                _updateWall();
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

    Vector3 _wallDestination;
    void _updateWall()
    {
        var lookAtToDestination = _destination - _destinationLookAt;
        var targetToDestination = _destination - _target.position;

        var obstacleHitFromLookAt = _getHit(_destinationLookAt - lookAtToDestination, lookAtToDestination * 2f);
        var obstacleHitFromTarget = _getHit(_target.position, targetToDestination * 1.1f);
        if ((obstacleHitFromTarget.HasValue && obstacleHitFromLookAt.HasValue) || obstacleHitFromTarget.HasValue)
        {
            var normal = obstacleHitFromTarget.Value.normal;
            var nextCameraPosition = obstacleHitFromTarget.Value.point + normal * _cameraWallMinDistance;

            var targetAndHitDisntance = Vector3.Distance(obstacleHitFromTarget.Value.point, _target.position);
            if (Vector3.Magnitude(_cameraPositionOffset) < targetAndHitDisntance)
            {
                _wallDestination = Vector3.Lerp(_wallDestination, nextCameraPosition, 0.1f);
            }
            else
            {
                _wallDestination = nextCameraPosition;
            }
        }
        else
        {
            _wallDestination = Vector3.Lerp(_wallDestination, _destination, 0.1f);
        }

        _currentZoomRate = 1 - Vector3.Distance(transform.position, _target.position).WithLog("DistanceA") / Vector3.Magnitude(_cameraPositionOffset);
        var lookAtToTarget = _target.position - _lookAt;
        _lookAt += lookAtToTarget * _currentZoomRate;

        _destination = _wallDestination;
    }

    Vector3 _zoomDestination;
    void _zoom(float zoomRate)
    {
        var rotatedPositionOffset = _getRotatedPositionOffset();
        Vector3.Magnitude(rotatedPositionOffset).Log("DistanceB");
        var nextCameraPosition = rotatedPositionOffset * zoomRate;

        _zoomDestination = Vector3.Lerp(_zoomDestination, nextCameraPosition, 0.1f);

        var lookAtToTarget = _target.position - _lookAt;
        //_lookAt += lookAtToTarget * zoomRate;

        _currentZoomRate = zoomRate;
        _destination -= _zoomDestination;
    }

    RaycastHit? _getHit(Vector3 origin, Vector3 rayVec)
    {
        var results = new RaycastHit[1];
        Physics.RaycastNonAlloc(new Ray(origin, Vector3.Normalize(rayVec)), results, Vector3.Magnitude(rayVec));
        Debug.DrawRay(origin, rayVec, Color.red, Time.deltaTime);
        if (results[0].transform != null)
        {
            _obstacle = results[0].collider.gameObject;
            return results[0];
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
    Vector3 _getRotatedPositionOffset()
    {
        return _getSphereRotatedVector(_cameraPositionOffset, _horizonalRotateDegree, _verticalRotateDegree);
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