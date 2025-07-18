using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

public class DogInteractionController : MonoBehaviour
{
    [Header("UI 캔버스 설정")]
    [SerializeField] private Canvas interactionCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("UI 버튼 설정")]
    [SerializeField] private Button handButton;
    [SerializeField] private Button lieDownButton;
    [SerializeField] private Button sitButton;

    [Header("카메라 설정")]
    [SerializeField] private CinemachineCamera clearShotCamera;
    [SerializeField] private CinemachineCamera dogCloseCamera;
    
    [Header("카메라 Priority 설정")]
    [SerializeField] private int defaultCameraPriority = 10;
    [SerializeField] private int activeCameraPriority = 20;
    [SerializeField] private int inactiveCameraPriority = 5;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Animator dogAnimator;
    
    [Header("애니메이션 트리거 이름")]
    [SerializeField] private string handTrigger = "Hand";
    [SerializeField] private string sitTrigger = "Sit";
    [SerializeField] private string lieDownTrigger = "Down";
    
    [Header("클릭 감지 설정")]
    [SerializeField] private LayerMask dogLayerMask = -1;
    [SerializeField] private LayerMask voiceButtonLayer = -1; // Added for voice button
    
    private Camera sceneCamera;
    private bool isCanvasVisible = false;
    private bool isCameraTransitioned = false;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (interactionCanvas != null)
        {
            interactionCanvas.gameObject.SetActive(false);
            isCanvasVisible = false;
        }
    }
    
    private void Start()
    {
        sceneCamera = Camera.main;
        
        if (sceneCamera == null)
        {
            sceneCamera = FindFirstObjectByType<Camera>();
        }
        
        FindCameras();
        FindAnimator();
        FindAndSetupButtons();
        
        if (canvasGroup == null && interactionCanvas != null)
        {
            canvasGroup = interactionCanvas.GetComponent<CanvasGroup>();
        }
        
        SetupInitialCameras();
        CheckRequiredComponents();
        
        SetupButtonEventListeners();
        
        Debug.Log("강아지 상호작용 시스템 초기화 완료");
    }
    
    private void FindCameras()
    {
        if (clearShotCamera == null)
        {
            GameObject clearShotObj = GameObject.Find("CM ClearShot Main");
            if (clearShotObj != null)
            {
                clearShotCamera = clearShotObj.GetComponent<CinemachineCamera>();
            }
        }
        
        if (dogCloseCamera == null)
        {
            Transform dogCameraTransform = transform.Find("CM Dog Close Camera");
            if (dogCameraTransform != null)
            {
                dogCloseCamera = dogCameraTransform.GetComponent<CinemachineCamera>();
            }
        }
    }
    
    private void SetupInitialCameras()
    {
        if (clearShotCamera != null)
        {
            clearShotCamera.Priority = defaultCameraPriority;
        }
        
        if (dogCloseCamera != null)
        {
            dogCloseCamera.Priority = inactiveCameraPriority;
        }
    }
    
    private void FindAndSetupButtons()
    {
        if (interactionCanvas == null) return;
        
        Button[] allButtons = FindObjectsOfType<Button>();
        
        foreach (Button button in allButtons)
        {
            if (button.name == "paw")
            {
                handButton = button;
            }
            else if (button.name == "sit")
            {
                sitButton = button;
            }
            else if (button.name == "Lie down")
            {
                lieDownButton = button;
            }
        }
        
        SetupButtonListeners();
    }
    
    private void SetupButtonListeners()
    {
        if (handButton != null)
        {
            handButton.onClick.RemoveAllListeners();
            handButton.onClick.AddListener(PlayHandAnimation);
        }
        
        if (lieDownButton != null)
        {
            lieDownButton.onClick.RemoveAllListeners();
            lieDownButton.onClick.AddListener(PlayLieDownAnimation);
        }
        
        if (sitButton != null)
        {
            sitButton.onClick.RemoveAllListeners();
            sitButton.onClick.AddListener(PlaySitAnimation);
        }
    }
    
    private void SetupButtonEventListeners()
    {
        if (handButton != null)
        {
            handButton.onClick.RemoveAllListeners();
            handButton.onClick.AddListener(PlayHandAnimation);
        }
        
        if (lieDownButton != null)
        {
            lieDownButton.onClick.RemoveAllListeners();
            lieDownButton.onClick.AddListener(PlayLieDownAnimation);
        }
        
        if (sitButton != null)
        {
            sitButton.onClick.RemoveAllListeners();
            sitButton.onClick.AddListener(PlaySitAnimation);
        }
    }
    
    private void FindAnimator()
    {
        if (dogAnimator == null)
        {
            dogAnimator = GetComponent<Animator>();
            if (dogAnimator == null)
            {
                dogAnimator = GetComponentInChildren<Animator>();
            }
        }
    }
    
    private void CheckRequiredComponents()
    {
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError("강아지 오브젝트에 Collider가 없습니다!");
        }
        
        if (dogAnimator == null)
        {
            Debug.LogError("Animator를 찾을 수 없습니다!");
        }
        
        if (clearShotCamera == null)
        {
            Debug.LogError("ClearShot Camera를 찾을 수 없습니다!");
        }
        
        if (dogCloseCamera == null)
        {
            Debug.LogError("Dog Close Camera를 찾을 수 없습니다!");
        }
        
        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem이 필요합니다!");
        }
    }
    
    private Vector2 mouseDownPosition;
    private bool isDragDetected = false;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPosition = Input.mousePosition;
            isDragDetected = false;
        }
        else if (Input.GetMouseButton(0))
        {
            // 드래그 감지 (5픽셀 이상 이동)
            if (Vector2.Distance(Input.mousePosition, mouseDownPosition) > 5f)
            {
                isDragDetected = true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // 드래그가 아닌 클릭인 경우에만 HandleUnifiedClick 실행
            if (!isDragDetected)
            {
                HandleUnifiedClick();
            }
        }
    }
    
    private void HandleUnifiedClick()
    {
        if (EventSystem.current == null) return;
        
        Vector2 mousePos = Input.mousePosition;
        
        // 먼저 녹음 버튼 클릭 여부 확인
        if (IsClickingVoiceButton())
        {
            return; // 녹음 버튼 클릭 시 카메라 전환 하지 않음
        }
        
        // 캔버스가 표시된 상태에서 UI 클릭 확인
        if (isCanvasVisible && interactionCanvas != null)
        {
            GraphicRaycaster raycaster = interactionCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = mousePos;
                
                List<RaycastResult> results = new List<RaycastResult>();
                raycaster.Raycast(pointerData, results);
                
                // UI 버튼 클릭 처리
                foreach (RaycastResult result in results)
                {
                    string buttonName = result.gameObject.name;
                    
                    if (buttonName == "paw")
                    {
                        PlayHandAnimation();
                        return;
                    }
                    else if (buttonName == "sit")
                    {
                        PlaySitAnimation();
                        return;
                    }
                    else if (buttonName == "Lie down")
                    {
                        PlayLieDownAnimation();
                        return;
                    }
                }
                
                if (results.Count > 0)
                {
                    return;
                }
            }
            
            OnEmptySpaceClicked();
            return;
        }
        
        // 캔버스가 숨겨진 상태에서 강아지 클릭 감지
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, dogLayerMask))
        {
            if (hit.collider.gameObject == gameObject)
            {
                OnDogClicked();
                return;
            }
            else
            {
                OnOtherObjectClicked();
                return;
            }
        }
        
        OnEmptySpaceClicked();
    }
    
    private void OnDogClicked()
    {
        // 강아지 클릭 시 줌인만
        TransitionToDogCamera();
    }
    
    private void OnOtherObjectClicked()
    {
        // 다른 오브젝트 클릭 시 줌아웃
        TransitionToClearShotCamera();
    }
    
    private void OnEmptySpaceClicked()
    {
        // 빈 공간 클릭 시 줌아웃
        TransitionToClearShotCamera();
    }
    
    private void TransitionToDogCamera()
    {
        if (!isCameraTransitioned && dogCloseCamera != null && clearShotCamera != null)
        {
            dogCloseCamera.Priority = activeCameraPriority;
            clearShotCamera.Priority = inactiveCameraPriority;
            isCameraTransitioned = true;
        }
    }
    
    private void TransitionToClearShotCamera()
    {
        if (isCameraTransitioned && clearShotCamera != null && dogCloseCamera != null)
        {
            clearShotCamera.Priority = defaultCameraPriority;
            dogCloseCamera.Priority = inactiveCameraPriority;
            isCameraTransitioned = false;
        }
    }
    
    private void ShowCanvasWithFade()
    {
        if (interactionCanvas != null && canvasGroup != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            interactionCanvas.gameObject.SetActive(true);
            isCanvasVisible = true;
            
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 1f, fadeDuration));
        }
    }
    
    private void HideCanvasWithFade()
    {
        if (interactionCanvas != null && canvasGroup != null && isCanvasVisible)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 0f, fadeDuration, () => {
                interactionCanvas.gameObject.SetActive(false);
                isCanvasVisible = false;
            }));
        }
    }
    
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float targetAlpha, float duration, System.Action onComplete = null)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            float curveValue = fadeCurve.Evaluate(progress);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            
            yield return null;
        }
        
        group.alpha = targetAlpha;
        onComplete?.Invoke();
    }
    
    // 애니메이션 실행 메서드들
    public void PlaySitAnimation()
    {
        // 네비게이션 일시 중지
        DogNavigation dogNavigation = GetComponent<DogNavigation>();
        if (dogNavigation != null)
        {
            dogNavigation.PauseNavigation();
        }
        
        PlayAnimation(sitTrigger);
        
        // 4초 후 네비게이션 재개 (애니메이션 완료 추정 시간)
        StartCoroutine(ResumeNavigationAfterDelay(4f));
    }

    public void PlayHandAnimation()
    {
        // 네비게이션 일시 중지
        DogNavigation dogNavigation = GetComponent<DogNavigation>();
        if (dogNavigation != null)
        {
            dogNavigation.PauseNavigation();
        }
        
        PlayAnimation(handTrigger);
        
        // 3초 후 네비게이션 재개
        StartCoroutine(ResumeNavigationAfterDelay(3f));
    }

    public void PlayLieDownAnimation()
    {
        // 네비게이션 일시 중지
        DogNavigation dogNavigation = GetComponent<DogNavigation>();
        if (dogNavigation != null)
        {
            dogNavigation.PauseNavigation();
        }
        
        PlayAnimation(lieDownTrigger);
        
        // 5초 후 네비게이션 재개 (엎드려는 조금 더 길게)
        StartCoroutine(ResumeNavigationAfterDelay(5f));
    }
    
    private void PlayAnimation(string triggerName)
    {
        if (dogAnimator != null && dogAnimator.runtimeAnimatorController != null)
        {
            dogAnimator.SetTrigger(triggerName);
        }
        else
        {
            Debug.LogError("애니메이션 실행 실패: Animator 또는 Controller가 없습니다.");
        }
    }
    
    /// <summary>
    /// 지정된 시간 후 네비게이션 재개
    /// </summary>
    private System.Collections.IEnumerator ResumeNavigationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        DogNavigation dogNavigation = GetComponent<DogNavigation>();
        if (dogNavigation != null)
        {
            dogNavigation.ResumeNavigation();
        }
    }

    /// <summary>
    /// 카메라 전환에 영향을 받지 않는 UI 버튼들 클릭 여부 확인
    /// </summary>
    private bool IsClickingVoiceButton()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        
        foreach (RaycastResult result in results)
        {
            // 카메라 전환에 영향을 받지 않을 버튼들
            if (result.gameObject.name == "Button_voice" || 
                result.gameObject.name == "Button_water" ||
                result.gameObject.name == "Button_heart" ||
                result.gameObject.name == "Button_food" ||
                result.gameObject.name == "paw" ||          // 👈 추가
                result.gameObject.name == "sit" ||          // 👈 추가
                result.gameObject.name == "Lie down" ||     // 👈 추가
                result.gameObject.name.Contains("voice") ||
                result.gameObject.name.Contains("Voice"))
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// 카메라 드래그 중인지 확인
    /// </summary>
    private bool IsCameraDragging()
    {
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            // CameraController의 isDragging 상태 확인
            var field = cameraController.GetType().GetField("isDragging", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                return (bool)field.GetValue(cameraController);
            }
        }
        return false;
    }
}

