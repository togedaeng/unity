using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("터치 감도")]
    public float touchSensitivity = 1f;
    
    [Header("좌우 회전 제한")]
    public float leftRotationLimit = 30f;   // 좌측 회전 제한 (양수)
    public float rightRotationLimit = 30f;  // 우측 회전 제한 (양수)
    
    [Header("부드러운 움직임")]
    public float rotationSmoothness = 5f;
    
    private GameObject clearShotCamera;
    private DogInteractionController dogController;
    
    private float currentHorizontalAngle = 0f;
    
    private bool isDragging = false;
    private Vector2 lastTouchPosition;
    
    private Vector3 initialPosition;
    private Vector3 initialRotation;
    
    void Start()
    {
        // DogInteractionController 찾기
        dogController = FindObjectOfType<DogInteractionController>();
        
        // ClearShot 카메라 찾기
        FindClearShotCamera();
        
        if (clearShotCamera != null)
        {
            // 초기 위치와 회전 저장
            initialPosition = clearShotCamera.transform.position;
            initialRotation = clearShotCamera.transform.eulerAngles;
            
            Debug.Log($"ClearShot 카메라 찾음: {clearShotCamera.name}");
            Debug.Log($"좌우 제한: -{leftRotationLimit}도 ~ +{rightRotationLimit}도");
        }
        else
        {
            Debug.LogError("ClearShot 카메라를 찾을 수 없습니다!");
        }
    }
    
    void Update()
    {
        if (clearShotCamera == null) return;
        
        // ClearShot 카메라가 활성화되어 있을 때만 드래그 허용
        if (IsClearShotCameraActive())
        {
            HandleTouchInput();
            UpdateCameraPosition();
        }
    }
    
    void FindClearShotCamera()
    {
        // "ClearShot" 이름을 가진 GameObject 찾기
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("ClearShot"))
            {
                clearShotCamera = obj;
                Debug.Log($"ClearShot 카메라 발견: {obj.name}");
                break;
            }
        }
    }
    
    bool IsClearShotCameraActive()
    {
        if (clearShotCamera == null) return false;
        
        // DogInteractionController에서 카메라 전환 상태 확인
        if (dogController != null)
        {
            var field = dogController.GetType().GetField("isCameraTransitioned", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                bool isTransitioned = (bool)field.GetValue(dogController);
                return !isTransitioned;
            }
        }
        
        return true;
    }
    
    void HandleTouchInput()
    {
        // 터치 입력 처리
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                isDragging = true;
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 touchDelta = touch.position - lastTouchPosition;
                UpdateHorizontalAngle(touchDelta.x);
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }
        
        // 에디터에서 테스트용 - 마우스 드래그
        #if UNITY_EDITOR
        HandleMouseInput();
        #endif
    }
    
    #if UNITY_EDITOR
    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastTouchPosition = Input.mousePosition;
            Debug.Log("ClearShot 카메라 드래그 시작!");
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 mouseDelta = (Vector2)Input.mousePosition - lastTouchPosition;
            UpdateHorizontalAngle(mouseDelta.x);
            lastTouchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            Debug.Log("ClearShot 카메라 드래그 종료!");
        }
        
        // R키로 카메라 리셋
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }
        
        // C키로 카메라 상태 확인
        if (Input.GetKeyDown(KeyCode.C))
        {
            CheckCameraStatus();
        }
    }
    #endif
    
    void UpdateHorizontalAngle(float horizontalInput)
    {
        // 수평 회전만 처리
        float angleChange = horizontalInput * touchSensitivity * 0.05f;
        currentHorizontalAngle += angleChange;
        
        // 좌우 제한 적용 (좌측은 음수, 우측은 양수)
        currentHorizontalAngle = Mathf.Clamp(currentHorizontalAngle, -leftRotationLimit, rightRotationLimit);
        
        Debug.Log($"수평 각도: {currentHorizontalAngle:F1}도 (제한: -{leftRotationLimit}도 ~ +{rightRotationLimit}도)");
    }
    
    void UpdateCameraPosition()
    {
        if (clearShotCamera == null) return;
        
        // 초기 회전값에서 수평 회전만 적용 (수직 회전은 고정)
        Vector3 rotationEuler = new Vector3(
            initialRotation.x, // 수직 회전 고정
            initialRotation.y + currentHorizontalAngle, // 수평 회전만 적용
            initialRotation.z
        );
        
        // 부드러운 회전 적용
        if (rotationSmoothness > 0)
        {
            Quaternion targetRotation = Quaternion.Euler(rotationEuler);
            clearShotCamera.transform.rotation = Quaternion.Slerp(
                clearShotCamera.transform.rotation, 
                targetRotation, 
                rotationSmoothness * Time.deltaTime
            );
        }
        else
        {
            clearShotCamera.transform.rotation = Quaternion.Euler(rotationEuler);
        }
    }
    
    public void ResetCamera()
    {
        currentHorizontalAngle = 0f;
        
        if (clearShotCamera != null)
        {
            clearShotCamera.transform.position = initialPosition;
            clearShotCamera.transform.eulerAngles = initialRotation;
            Debug.Log("ClearShot 카메라 리셋 완료!");
        }
    }
    
    public void ResetCameraButton()
    {
        ResetCamera();
    }
    
    void CheckCameraStatus()
    {
        Debug.Log("=== 카메라 상태 확인 ===");
        Debug.Log($"ClearShot 활성화: {IsClearShotCameraActive()}");
        Debug.Log($"현재 수평 각도: {currentHorizontalAngle:F1}도");
        Debug.Log($"좌우 제한: -{leftRotationLimit}도 ~ +{rightRotationLimit}도");
    }
}
