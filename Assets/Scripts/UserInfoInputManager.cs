using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using TogedaengData;

// 메인 스크립트 클래스
public class UserInfoInputManager : MonoBehaviour
{
    [Header("서버 설정")]
    [SerializeField] private string serverUrl = "http://localhost:8080";
    
    [Header("UI 요소들")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private Button nicknameCheckButton;
    [SerializeField] private Button maleButton;
    [SerializeField] private Button femaleButton;
    [SerializeField] private TMP_Dropdown yearDropdown;
    [SerializeField] private TMP_Dropdown monthDropdown;
    [SerializeField] private TMP_Dropdown dayDropdown;
    [SerializeField] private Button submitButton;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("색상 설정")]
    [SerializeField] private Color selectedGenderColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color normalGenderColor = Color.white;
    [SerializeField] private Color nicknameAvailableColor = Color.green;
    [SerializeField] private Color nicknameUnavailableColor = Color.red;
    [SerializeField] private Color normalButtonColor = Color.white;
    
    [Header("씬 전환")]
    [SerializeField] private string mainSceneName = "Scene4";
    
    // 상태 변수들
    private OAuthUserInfo socialLoginInfo;
    private bool isNicknameAvailable = false;
    private bool isNicknameChecked = false;
    private string selectedGender = "";
    private Coroutine nicknameCheckCoroutine;
    
    private void Start()
    {
        LoadSocialLoginInfo();
        InitializeUI();
        SetupEventListeners();
    }
    
    private void LoadSocialLoginInfo()
    {
        string socialInfoJson = PlayerPrefs.GetString("SocialLoginInfo", "");
        
        if (!string.IsNullOrEmpty(socialInfoJson))
        {
            socialLoginInfo = JsonUtility.FromJson<OAuthUserInfo>(socialInfoJson);
            Debug.Log($"소셜 로그인 정보 로드됨: {socialLoginInfo.email}, {socialLoginInfo.provider}");
        }
        else
        {
            Debug.LogError("소셜 로그인 정보를 찾을 수 없습니다!");
            // 테스트용 데이터 생성
            socialLoginInfo = new OAuthUserInfo
            {
                email = "test2@example.com",
                provider = "google",
                providerId = "test_google_124"
            };
            Debug.Log("테스트용 소셜 로그인 정보 생성됨");
        }
    }
    
    private void InitializeUI()
    {
        SetupDateDropdowns();
        
        // 초기 상태 설정
        submitButton.interactable = false;
        loadingPanel.SetActive(false);
        
        // 버튼 초기 색상
        SetGenderButtonColor(maleButton, false);
        SetGenderButtonColor(femaleButton, false);
        SetNicknameCheckButtonColor(false);
    }
    
    private void SetupDateDropdowns()
    {
        // 년도 드롭다운
        yearDropdown.ClearOptions();
        yearDropdown.options.Add(new TMP_Dropdown.OptionData("년도"));
        int currentYear = DateTime.Now.Year;
        for (int year = currentYear; year >= 1950; year--)
        {
            yearDropdown.options.Add(new TMP_Dropdown.OptionData(year.ToString()));
        }
        yearDropdown.value = 0;
        
        // 월 드롭다운
        monthDropdown.ClearOptions();
        monthDropdown.options.Add(new TMP_Dropdown.OptionData("월"));
        for (int month = 1; month <= 12; month++)
        {
            monthDropdown.options.Add(new TMP_Dropdown.OptionData(month.ToString()));
        }
        monthDropdown.value = 0;
        
        // 일 드롭다운
        dayDropdown.ClearOptions();
        dayDropdown.options.Add(new TMP_Dropdown.OptionData("일"));
        for (int day = 1; day <= 31; day++)
        {
            dayDropdown.options.Add(new TMP_Dropdown.OptionData(day.ToString()));
        }
        dayDropdown.value = 0;
    }
    
    private void SetupEventListeners()
    {
        // 닉네임 관련
        nicknameInputField.onValueChanged.AddListener(OnNicknameChanged);
        nicknameCheckButton.onClick.AddListener(OnNicknameCheckClicked);
        
        // 성별 선택
        maleButton.onClick.AddListener(() => OnGenderSelected("M"));
        femaleButton.onClick.AddListener(() => OnGenderSelected("F"));
        
        // 생년월일 드롭다운
        yearDropdown.onValueChanged.AddListener(OnDateChanged);
        monthDropdown.onValueChanged.AddListener(OnDateChanged);
        dayDropdown.onValueChanged.AddListener(OnDateChanged);
        
        // 제출 버튼
        submitButton.onClick.AddListener(OnSubmitClicked);
    }
    
    private void OnNicknameChanged(string nickname)
    {
        // 닉네임 변경 시 중복 확인 상태 리셋
        isNicknameChecked = false;
        isNicknameAvailable = false;
        
        // 기존 중복 확인 코루틴 중지
        if (nicknameCheckCoroutine != null)
        {
            StopCoroutine(nicknameCheckCoroutine);
        }
        
        // 중복 확인 버튼 활성화/비활성화
        nicknameCheckButton.interactable = !string.IsNullOrEmpty(nickname.Trim());
        
        // 버튼 색상 초기화
        SetNicknameCheckButtonColor(false);
        
        CheckSubmitButtonState();
    }
    
    private void OnNicknameCheckClicked()
    {
        string nickname = nicknameInputField.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("❌ 닉네임을 입력해주세요.");
            return;
        }
        
        Debug.Log($"🔍 닉네임 중복 확인 요청: '{nickname}'");
        nicknameCheckCoroutine = StartCoroutine(CheckNicknameCoroutine(nickname));
    }
    
    private IEnumerator CheckNicknameCoroutine(string nickname)
    {
        nicknameCheckButton.interactable = false;
        
        string url = $"{serverUrl}/auth/nickname/check?nickname={UnityWebRequest.EscapeURL(nickname)}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    TogedaengData.NicknameCheckResponse response = JsonUtility.FromJson<TogedaengData.NicknameCheckResponse>(request.downloadHandler.text);
                    
                    isNicknameChecked = true;
                    isNicknameAvailable = response.isAvailable;
                    
                    if (response.isAvailable)
                    {
                        Debug.Log($"✅ 닉네임 '{nickname}' 사용 가능!");
                        SetNicknameCheckButtonColor(true);
                    }
                    else
                    {
                        Debug.Log($"❌ 닉네임 '{nickname}' 이미 사용 중");
                        SetNicknameCheckButtonColor(false);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ 닉네임 확인 응답 파싱 실패: {e.Message}");
                    isNicknameChecked = false;
                    isNicknameAvailable = false;
                }
            }
            else
            {
                Debug.LogError($"❌ 닉네임 확인 요청 실패: {request.error}");
                isNicknameChecked = false;
                isNicknameAvailable = false;
            }
            
            nicknameCheckButton.interactable = true;
            CheckSubmitButtonState();
        }
    }
    
    private void OnGenderSelected(string gender)
    {
        selectedGender = gender;
        
        // 버튼 색상 업데이트
        SetGenderButtonColor(maleButton, gender == "M");
        SetGenderButtonColor(femaleButton, gender == "F");
        
        CheckSubmitButtonState();
    }
    
    private void OnDateChanged(int value)
    {
        CheckSubmitButtonState();
    }
    
    private void CheckSubmitButtonState()
    {
        bool canSubmit = isNicknameChecked && 
                        isNicknameAvailable && 
                        !string.IsNullOrEmpty(selectedGender) &&
                        yearDropdown.value > 0 && 
                        monthDropdown.value > 0 && 
                        dayDropdown.value > 0;
        
        submitButton.interactable = canSubmit;
    }
    
    private void OnSubmitClicked()
    {
        if (socialLoginInfo == null)
        {
            Debug.LogError("❌ 소셜 로그인 정보가 없습니다.");
            return;
        }
        
        Debug.Log("🚀 회원가입 시작!");
        StartCoroutine(CreateUserCoroutine());
    }
    
    private IEnumerator CreateUserCoroutine()
    {
        loadingPanel.SetActive(true);
        
        // 생년월일 포맷팅
        string year = yearDropdown.options[yearDropdown.value].text;
        string month = monthDropdown.options[monthDropdown.value].text.PadLeft(2, '0');
        string day = dayDropdown.options[dayDropdown.value].text.PadLeft(2, '0');
        string birthDate = $"{year}-{month}-{day}";
        
        // 사용자 정보 생성
        TogedaengData.UserInfoRequest userInfo = new TogedaengData.UserInfoRequest
        {
            email = socialLoginInfo.email,
            provider = socialLoginInfo.provider,
            providerId = socialLoginInfo.providerId,
            nickname = nicknameInputField.text.Trim(),
            gender = selectedGender,
            birth = birthDate
        };
        
        Debug.Log($"📝 회원가입 정보 - 닉네임: {userInfo.nickname}, 성별: {userInfo.gender}, 생년월일: {userInfo.birth}");
        
        string jsonData = JsonUtility.ToJson(userInfo);
        string url = $"{serverUrl}/auth/create";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            loadingPanel.SetActive(false);
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    TogedaengData.AuthResponse response = JsonUtility.FromJson<TogedaengData.AuthResponse>(request.downloadHandler.text);
                    
                    // 토큰 저장
                    PlayerPrefs.SetString("AccessToken", response.token.accessToken);
                    PlayerPrefs.SetString("RefreshToken", response.token.refreshToken);
                    
                    // 사용자 정보 저장
                    PlayerPrefs.SetString("UserInfo", JsonUtility.ToJson(response.user));
                    
                    // 임시 소셜 로그인 정보 삭제
                    PlayerPrefs.DeleteKey("SocialLoginInfo");
                    PlayerPrefs.Save();
                    
                    Debug.Log($"🎉 회원가입 성공! 사용자 ID: {response.user.id}, 닉네임: {response.user.nickname}");
                    Debug.Log($"🔄 다음 씬으로 이동: {mainSceneName}");
                    
                    // 메인 씬으로 이동
                    UnityEngine.SceneManagement.SceneManager.LoadScene(mainSceneName);
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ 회원가입 응답 파싱 실패: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"❌ 회원가입 요청 실패: {request.error}");
            }
        }
    }
    
    // 색상 관련 메서드들
    private void SetGenderButtonColor(Button button, bool isSelected)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isSelected ? selectedGenderColor : normalGenderColor;
        }
    }
    
    private void SetNicknameCheckButtonColor(bool isAvailable)
    {
        Image buttonImage = nicknameCheckButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (isNicknameChecked)
            {
                buttonImage.color = isAvailable ? nicknameAvailableColor : nicknameUnavailableColor;
            }
            else
            {
                buttonImage.color = normalButtonColor;
            }
        }
    }
    
    private void OnDestroy()
    {
        if (nicknameCheckCoroutine != null)
        {
            StopCoroutine(nicknameCheckCoroutine);
        }
    }
}
