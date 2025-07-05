using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.Events;
using TogedaengData;

public class GoogleOAuthManager : MonoBehaviour
{
    [Header("OAuth 설정")]
    public string androidClientId = "501518296843-lpp8uf9pvv7h340kuhgdfloo99hvdvd3.apps.googleusercontent.com";
    public string webClientId = "501518296843-b7rlj3ofer9eaa0293a1kops8m95e4ta.apps.googleusercontent.com";
    public string backendUrl = "http://localhost:8080";
    
    [Header("이벤트")]
    public UnityEvent<TokenResponse> OnLoginSuccess;
    public UnityEvent<OAuthUserInfo> OnNeedAdditionalInfo;
    public UnityEvent<string> OnLoginFailed;
    
    private string redirectUri;
    private string state;
    
    private string currentClientId;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 플랫폼별 클라이언트 ID 선택
        #if UNITY_EDITOR
            currentClientId = webClientId;
            redirectUri = "http://localhost:8080/oauth/callback/google";
        #elif UNITY_ANDROID || UNITY_IOS
            currentClientId = androidClientId;  // Android 클라이언트 ID 사용
            redirectUri = "com.DefaultCompany.socialLoginTest://oauth/callback";
        #endif
        
        // 백엔드 URL도 플랫폼별로 설정
        #if UNITY_EDITOR
            backendUrl = "http://localhost:8080";
        #else
            backendUrl = "http://localhost:8080";  // 실제 IP
        #endif
        
        // Deep Link 콜백 등록
        Application.deepLinkActivated += OnDeepLinkActivated;
        
        // 설정 확인 로그
        Debug.Log($"플랫폼: {Application.platform}");
        Debug.Log($"사용 중인 클라이언트 ID: {currentClientId}");
        Debug.Log($"리다이렉트 URI: {redirectUri}");
        
        // 앱 시작 시 Deep Link 확인
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            OnDeepLinkActivated(Application.absoluteURL);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 구글 로그인 시작 (버튼에서 호출)
    /// </summary>
    public void StartGoogleLogin()
    {
        Debug.Log("구글 로그인 시작");
        
        // State 생성 (보안용)
        state = Guid.NewGuid().ToString();
        
        // 구글 OAuth URL 생성
        string authUrl = BuildGoogleAuthUrl();
        Debug.Log($"OAuth URL: {authUrl}");
        
        // 외부 브라우저로 로그인 페이지 열기
        Application.OpenURL(authUrl);
    }
    
    private string BuildGoogleAuthUrl()
    {
        string scope = "email profile";
        
        string authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={currentClientId}&" +  // currentClientId 사용
               $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
               $"response_type=code&" +
               $"scope={Uri.EscapeDataString(scope)}&" +
               $"state={state}&" +
               $"access_type=offline";
        
        Debug.Log($"OAuth URL: {authUrl}");
        return authUrl;
    }
    
    private void OnDeepLinkActivated(string url)
    {
        Debug.Log($"Deep link 활성화: {url}");
        
        // 올바른 콜백 URL인지 확인
        if (url.Contains("oauth/callback") || url.Contains("://oauth"))
        {
            ProcessCallback(url);
        }
    }
    
    private void ProcessCallback(string callbackUrl)
    {
        try
        {
            // URL에서 파라미터 추출
            var uri = new Uri(callbackUrl);
            var query = ParseQueryString(uri.Query);
            
            string code = GetQueryValue(query, "code");
            string returnedState = GetQueryValue(query, "state");
            string error = GetQueryValue(query, "error");
            
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"OAuth 에러: {error}");
                OnLoginFailed?.Invoke($"로그인 실패: {error}");
                return;
            }
            
            // State 검증
            if (returnedState != state)
            {
                Debug.LogError("State 불일치 - 보안 위험");
                OnLoginFailed?.Invoke("보안 검증 실패");
                return;
            }
            
            if (!string.IsNullOrEmpty(code))
            {
                Debug.Log($"Authorization code 받음: {code}");
                StartCoroutine(SendCodeToBackend(code));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"콜백 처리 에러: {e.Message}");
            OnLoginFailed?.Invoke("콜백 처리 실패");
        }
    }
    
    private System.Collections.Generic.Dictionary<string, string> ParseQueryString(string query)
    {
        var dict = new System.Collections.Generic.Dictionary<string, string>();
        if (string.IsNullOrEmpty(query)) return dict;
        
        query = query.TrimStart('?');
        string[] pairs = query.Split('&');
        
        foreach (string pair in pairs)
        {
            string[] kv = pair.Split('=');
            if (kv.Length == 2)
            {
                dict[kv[0]] = Uri.UnescapeDataString(kv[1]);
            }
        }
        
        return dict;
    }
    
    private string GetQueryValue(System.Collections.Generic.Dictionary<string, string> query, string key)
    {
        return query.ContainsKey(key) ? query[key] : null;
    }
    
    private IEnumerator SendCodeToBackend(string code)
    {
        string apiUrl = $"{backendUrl}/auth/oauth/google";
        
        var requestData = new AuthorizationCodeRequest
        {
            code = code,
            redirectUri = redirectUri
        };
        
        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log($"백엔드 요청: {jsonData}");
        
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"백엔드 응답: {request.downloadHandler.text}");
                HandleLoginResponse(request.downloadHandler.text, request.responseCode);
            }
            else
            {
                Debug.LogError($"백엔드 요청 실패: {request.error}");
                OnLoginFailed?.Invoke($"서버 통신 실패: {request.error}");
            }
        }
    }
    
    private void HandleLoginResponse(string responseText, long responseCode)
    {
        if (responseCode == 200)
        {
            // 기존 사용자 - JWT 토큰 받음
            var tokenResponse = JsonUtility.FromJson<TokenResponse>(responseText);
            
            // 토큰 저장
            PlayerPrefs.SetString("AccessToken", tokenResponse.accessToken);
            PlayerPrefs.SetString("RefreshToken", tokenResponse.refreshToken);
            PlayerPrefs.Save();
            
            Debug.Log("로그인 성공!");
            OnLoginSuccess?.Invoke(tokenResponse);
        }
        else if (responseCode == 202)
        {
            // 신규 사용자 - 추가 정보 입력 필요
            var userInfo = JsonUtility.FromJson<OAuthUserInfo>(responseText);
            Debug.Log($"신규 사용자 - 추가 정보 필요: {userInfo.email}");
            OnNeedAdditionalInfo?.Invoke(userInfo);
        }
        else
        {
            Debug.LogError($"로그인 실패: {responseText}");
            OnLoginFailed?.Invoke("로그인 실패");
        }
    }
    
    void OnDestroy()
    {
        Application.deepLinkActivated -= OnDeepLinkActivated;
    }
}
