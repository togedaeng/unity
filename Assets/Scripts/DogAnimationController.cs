using UnityEngine;
using UnityEngine.UI;

public class DogAnimationController : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private Animator dogAnimator;
    
    [Header("버튼 설정")]
    [SerializeField] private Button handButton;      // 손 버튼
    [SerializeField] private Button lieDownButton;   // 엎드려 버튼
    [SerializeField] private Button sitButton;       // 앉아 버튼
    
    [Header("애니메이션 트리거 이름")]
    [SerializeField] private string handTrigger = "Hand";
    [SerializeField] private string lieDownTrigger = "LieDown";
    [SerializeField] private string sitTrigger = "Sit";
    
    private void Start()
    {
        SetupButtonListeners();
    }
    
    private void SetupButtonListeners()
    {
        // 각 버튼에 클릭 이벤트 연결
        if (handButton != null)
            handButton.onClick.AddListener(() => PlayAnimation(handTrigger));
            
        if (lieDownButton != null)
            lieDownButton.onClick.AddListener(() => PlayAnimation(lieDownTrigger));
            
        if (sitButton != null)
            sitButton.onClick.AddListener(() => PlayAnimation(sitTrigger));
    }
    
    private void PlayAnimation(string triggerName)
    {
        if (dogAnimator != null)
        {
            // 해당 트리거를 활성화하여 애니메이션 실행
            dogAnimator.SetTrigger(triggerName);
            
            // 디버그 로그 (선택사항)
            Debug.Log($"애니메이션 실행: {triggerName}");
        }
        else
        {
            Debug.LogWarning("Animator가 설정되지 않았습니다!");
        }
    }
    
    // 특정 애니메이션을 직접 호출하는 메서드들 (선택사항)
    public void PlayHandAnimation()
    {
        PlayAnimation(handTrigger);
    }
    
    public void PlayLieDownAnimation()
    {
        PlayAnimation(lieDownTrigger);
    }
    
    public void PlaySitAnimation()
    {
        PlayAnimation(sitTrigger);
    }
    
    // 현재 재생 중인 애니메이션 상태 확인 (선택사항)
    public bool IsAnimationPlaying(string stateName)
    {
        if (dogAnimator != null)
        {
            AnimatorStateInfo stateInfo = dogAnimator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(stateName);
        }
        return false;
    }
}
