using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIController : MonoBehaviour
{
    [Header("UI 버튼 설정")]
    [SerializeField] private Button heartButton;
    
    [Header("패널 설정")]
    [SerializeField] private GameObject interactionPanel;  // Canvas → GameObject로 변경
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    private bool isCanvasVisible = false;
    private Coroutine fadeCoroutine;
    
    private void Start()
    {
        // 하트 버튼 이벤트 연결
        if (heartButton != null)
        {
            heartButton.onClick.AddListener(OnHeartButtonClicked);
        }
        
        // 패널 초기 상태 설정
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);  // gameObject 제거
            isCanvasVisible = false;
        }
        
        // CanvasGroup 자동 찾기
        if (canvasGroup == null && interactionPanel != null)
        {
            canvasGroup = interactionPanel.GetComponent<CanvasGroup>();
        }
        
        Debug.Log("UI 컨트롤러 초기화 완료");
    }
    
    private void OnHeartButtonClicked()
    {
        if (isCanvasVisible)
        {
            HideCanvasWithFade();
        }
        else
        {
            ShowCanvasWithFade();
        }
    }
    
    private void ShowCanvasWithFade()
    {
        if (interactionPanel != null && canvasGroup != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            interactionPanel.SetActive(true);  // gameObject 제거
            isCanvasVisible = true;
            
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 1f, fadeDuration));
        }
    }
    
    private void HideCanvasWithFade()
    {
        if (interactionPanel != null && canvasGroup != null && isCanvasVisible)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 0f, fadeDuration, () => {
                interactionPanel.SetActive(false);  // gameObject 제거
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
}
