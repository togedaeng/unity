using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TogedaengData;

public class LoginUIManager : MonoBehaviour
{
    [Header("UI 요소")]
    public Button googleLoginButton;
    public GameObject loadingPanel;
    
    [Header("OAuth 관리자")]
    public GoogleOAuthManager oauthManager;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 버튼 이벤트 연결
        googleLoginButton.onClick.AddListener(OnGoogleLoginClicked);
        
        // OAuth 매니저 이벤트 연결
        oauthManager.OnLoginSuccess.AddListener(OnLoginSuccess);
        oauthManager.OnNeedAdditionalInfo.AddListener(OnNeedAdditionalInfo);
        oauthManager.OnLoginFailed.AddListener(OnLoginFailed);
        
        // 초기 상태 설정
        SetLoadingState(false);
        
        // 자동 로그인 확인
        CheckAutoLogin();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// 구글 로그인 버튼 클릭 이벤트
    /// </summary>
    public void OnGoogleLoginClicked()
    {
        Debug.Log("구글 로그인 버튼 클릭됨");
        
        SetLoadingState(true);
        
        // =============================================================================
        // [임시 코드] 에디터에서 테스트용 - 배포 시 반드시 제거해야 함!
        // 실제 배포에서는 웹뷰를 통한 OAuth 로그인이 정상 작동하므로 이 코드는 삭제 필요
        // =============================================================================
        #if UNITY_EDITOR
        Debug.Log("[개발용] 에디터에서 5초 후 자동으로 다음 씬으로 이동합니다.");
        Invoke(nameof(SimulateLoginSuccess), 5f);
        #else
        // 실제 OAuth 매니저의 로그인 시작 (배포 버전에서 사용)
        oauthManager.StartGoogleLogin();
        #endif
    }
    
    // =============================================================================
    // [임시 메서드] 에디터 테스트용 - 배포 시 반드시 제거해야 함!
    // =============================================================================
    #if UNITY_EDITOR
    private void SimulateLoginSuccess()
    {
        Debug.Log("[개발용] 로그인 성공 시뮬레이션 - Scene2으로 이동");
        
        // 임시 소셜 로그인 정보 생성 및 저장
        TogedaengData.OAuthUserInfo testSocialInfo = new TogedaengData.OAuthUserInfo
        {
            email = "test@gmail.com",
            provider = "google", 
            providerId = "google_test_123456"
        };
        
        // Scene2에서 사용할 수 있도록 저장
        PlayerPrefs.SetString("SocialLoginInfo", JsonUtility.ToJson(testSocialInfo));
        PlayerPrefs.Save();
        
        Debug.Log($"임시 소셜 로그인 정보 저장됨: {testSocialInfo.email}");
        
        SetLoadingState(false);
        LoadMainScene();
    }
    #endif
    
    private void OnLoginSuccess(TokenResponse tokenResponse)
    {
        SetLoadingState(false);
        
        Debug.Log("로그인 성공 - 메인 씬으로 이동");
        
        // 1초 후 메인 씬으로 이동
        Invoke(nameof(LoadMainScene), 1f);
    }
    
    private void OnNeedAdditionalInfo(OAuthUserInfo userInfo)
    {
        SetLoadingState(false);
        
        Debug.Log("추가 정보 입력이 필요합니다 - 다음 씬으로 이동");
        
        // 추가 정보 입력을 위해 다음 씬으로 이동
        LoadMainScene();
    }
    
    private void OnLoginFailed(string errorMessage)
    {
        SetLoadingState(false);
        
        Debug.LogError($"로그인 실패: {errorMessage}");
    }
    
    private void SetLoadingState(bool isLoading)
    {
        loadingPanel.SetActive(isLoading);
        googleLoginButton.interactable = !isLoading;
    }
    
    private void CheckAutoLogin()
    {
        if (PlayerPrefs.HasKey("AccessToken"))
        {
            SetLoadingState(true);
            
            // 토큰 유효성 검사
            StartCoroutine(ValidateToken());
        }
    }
    
    private System.Collections.IEnumerator ValidateToken()
    {
        string accessToken = PlayerPrefs.GetString("AccessToken");
        
        using (UnityEngine.Networking.UnityWebRequest request = 
               UnityEngine.Networking.UnityWebRequest.Get($"{oauthManager.backendUrl}/auth/validate"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // 유효한 토큰
                Debug.Log("자동 로그인 성공!");
                LoadMainScene();
            }
            else
            {
                // 토큰 만료
                PlayerPrefs.DeleteKey("AccessToken");
                PlayerPrefs.DeleteKey("RefreshToken");
                SetLoadingState(false);
                Debug.Log("토큰 만료 - 다시 로그인해주세요.");
            }
        }
    }
    
    private void LoadMainScene()
    {
        SceneManager.LoadScene("Scene2");
    }
}
