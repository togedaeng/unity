using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using TogedaengData;

public class DogRegistrationManager : MonoBehaviour
{
    [Header("서버 설정")]
    [SerializeField] private string serverUrl = "http://43.201.51.65:8080";
    
    [Header("UI 요소들")]
    [SerializeField] private TMP_InputField dogNameInputField;
    [SerializeField] private TMP_InputField callNameInputField;
    
    [Header("생년월일 드롭다운")]
    [SerializeField] private TMP_Dropdown yearDropdown;
    [SerializeField] private TMP_Dropdown monthDropdown;
    [SerializeField] private TMP_Dropdown dayDropdown;
    
    [Header("성별 버튼")]
    [SerializeField] private Button maleButton;
    [SerializeField] private Button femaleButton;
    
    [Header("성격 드롭다운")]
    [SerializeField] private TMP_Dropdown personality1Dropdown;
    [SerializeField] private TMP_Dropdown personality2Dropdown;
    
    [Header("메인 이미지 업로드")]
    [SerializeField] private Button mainImageButton;
    
    [Header("테스트용 이미지")]
    [SerializeField] private Texture2D testImage; // 테스트용 하드코딩 이미지
    
    [Header("UI 패널")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Button submitButton;
    
    [Header("씬 전환")]
    [SerializeField] private string nextSceneName = "Scene5";
    
    [Header("디버그 UI (선택사항)")]
    [SerializeField] private TMPro.TextMeshProUGUI debugText;
    
    // 성격 데이터 (하드코딩 - 백엔드에 조회 API 없음)
    private readonly Dictionary<string, long> personalityMap = new Dictionary<string, long>
    {
        {"활발함", 1}, {"온순함", 2}, {"소심함", 3}, {"똑똑함", 4}, {"예민함", 5},
        {"호기심", 6}, {"장난기", 7}, {"게으름", 8}, {"식탐많음", 9}, {"애교쟁이", 10}
    };
    
    // 입력 상태 관리
    private string selectedGender = "";
    private Texture2D mainImageTexture;
    private bool isImageSelected = false;
    
    // 버튼 초기 상태 저장
    private Sprite originalButtonSprite;
    private Color originalButtonColor;
    
    private void Start()
    {
        SetupUI();
        SetupEventListeners();
        SaveOriginalButtonState();
    }
    
    private void SaveOriginalButtonState()
    {
        // 버튼의 초기 상태 저장
        if (mainImageButton != null)
        {
            Image buttonImage = mainImageButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                originalButtonSprite = buttonImage.sprite;
                originalButtonColor = buttonImage.color;
            }
        }
    }
    
    private void SetupUI()
    {
        // 로딩 패널 숨김
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
            
        // 이미지 버튼 초기화
        ResetImageButton();
            
        // 드롭다운 초기화
        SetupDropdowns();
        
        // 버튼 색상 초기화
        SetGenderButtonColors();
        
        // 제출 버튼 비활성화
        submitButton.interactable = false;
    }
    
    private void ResetImageButton()
    {
        if (mainImageButton != null)
        {
            // 버튼을 원래 상태로 복원
            Image buttonImage = mainImageButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = originalButtonSprite;
                buttonImage.color = originalButtonColor;
            }
        }
        isImageSelected = false;
    }
    
    private void SetupDropdowns()
    {
        // 생년월일 드롭다운은 DropdownPopulator.cs에서 관리하므로 여기서는 제외
        
        // 성격 드롭다운만 설정
        SetupPersonalityDropdowns();
    }
    
    private void SetupPersonalityDropdowns()
    {
        var personalityOptions = new List<TMP_Dropdown.OptionData>();
        personalityOptions.Add(new TMP_Dropdown.OptionData("성격 선택"));
        
        foreach (var personality in personalityMap.Keys)
        {
            personalityOptions.Add(new TMP_Dropdown.OptionData(personality));
        }
        
        personality1Dropdown.options = new List<TMP_Dropdown.OptionData>(personalityOptions);
        personality2Dropdown.options = new List<TMP_Dropdown.OptionData>(personalityOptions);
        
        personality1Dropdown.value = 0;
        personality2Dropdown.value = 0;
    }
    
    private void SetupEventListeners()
    {
        // 입력 필드 변경 이벤트
        dogNameInputField.onValueChanged.AddListener(OnInputChanged);
        callNameInputField.onValueChanged.AddListener(OnInputChanged);
        
        // 드롭다운 변경 이벤트
        personality1Dropdown.onValueChanged.AddListener(OnPersonality1Changed);
        personality2Dropdown.onValueChanged.AddListener(OnPersonality2Changed);
        
        // 성별 버튼
        maleButton.onClick.AddListener(() => OnGenderSelected("M"));
        femaleButton.onClick.AddListener(() => OnGenderSelected("F"));
        
        // 메인 이미지 버튼
        mainImageButton.onClick.AddListener(OnMainImageButtonClicked);
        
        // 제출 버튼
        submitButton.onClick.AddListener(OnSubmitClicked);
    }
    
    private void OnInputChanged(string value)
    {
        CheckSubmitButtonState();
    }
    
    private void OnPersonality1Changed(int value)
    {
        // 성격2에서 같은 항목 제거
        UpdatePersonality2Options();
        CheckSubmitButtonState();
    }
    
    private void OnPersonality2Changed(int value)
    {
        CheckSubmitButtonState();
    }
    
    private void UpdatePersonality2Options()
    {
        int selectedP1 = personality1Dropdown.value;
        
        var options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("성격 선택 (선택사항)"));
        
        int index = 1;
        foreach (var personality in personalityMap.Keys)
        {
            if (index != selectedP1) // 첫 번째 성격과 다른 것만 추가
            {
                options.Add(new TMP_Dropdown.OptionData(personality));
            }
            index++;
        }
        
        personality2Dropdown.options = options;
        personality2Dropdown.value = 0;
    }
    
    private void OnGenderSelected(string gender)
    {
        selectedGender = gender;
        SetGenderButtonColors();
        CheckSubmitButtonState();
    }
    
    private void SetGenderButtonColors()
    {
        // 남성 버튼 색상
        ColorBlock maleColors = maleButton.colors;
        maleColors.normalColor = selectedGender == "M" ? Color.blue : Color.white;
        maleButton.colors = maleColors;
        
        // 여성 버튼 색상
        ColorBlock femaleColors = femaleButton.colors;
        femaleColors.normalColor = selectedGender == "F" ? Color.magenta : Color.white;
        femaleButton.colors = femaleColors;
    }
    
    private void OnMainImageButtonClicked()
    {
        // 실제 구현에서는 파일 다이얼로그를 띄워야 함
        // 여기서는 테스트용으로 더미 이미지 생성
        CreateDummyMainImage();
    }
    
    private void CreateDummyMainImage()
    {
        // 테스트용 더미 이미지 생성 (읽기 가능한 텍스처)
        Texture2D dummyTexture = new Texture2D(400, 300, TextureFormat.RGB24, false);
        Color[] pixels = new Color[400 * 300];
        
        // 랜덤 색상으로 더미 이미지 생성
        Color randomColor = new Color(
            UnityEngine.Random.Range(0.3f, 1f),
            UnityEngine.Random.Range(0.3f, 1f),
            UnityEngine.Random.Range(0.3f, 1f)
        );
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = randomColor;
        }
        dummyTexture.SetPixels(pixels);
        dummyTexture.Apply();
        
        mainImageTexture = dummyTexture;
        
        // 버튼에 이미지 적용
        UpdateImageButton();
        CheckSubmitButtonState();
    }
    
    private void UpdateImageButton()
    {
        if (mainImageTexture != null && mainImageButton != null)
        {
            // 1. 버튼 이미지를 선택한 이미지로 변경
            Image buttonImage = mainImageButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Sprite newSprite = Sprite.Create(
                    mainImageTexture,
                    new Rect(0, 0, mainImageTexture.width, mainImageTexture.height),
                    Vector2.zero
                );
                buttonImage.sprite = newSprite;
                
                // 이미지가 버튼에 잘 보이도록 색상 조정
                buttonImage.color = Color.white;
            }
            
            isImageSelected = true;
        }
    }
    
    private void CheckSubmitButtonState()
    {
        // 원래 검증 로직으로 복원
        bool nameValid = !string.IsNullOrEmpty(dogNameInputField.text.Trim());
        bool callNameValid = !string.IsNullOrEmpty(callNameInputField.text.Trim());
        bool genderValid = !string.IsNullOrEmpty(selectedGender);
        bool birthValid = yearDropdown.value > 0 && monthDropdown.value > 0 && dayDropdown.value > 0;
        bool personalityValid = personality1Dropdown.value > 0;
        bool imageValid = isImageSelected && mainImageTexture != null;
        
        bool canSubmit = nameValid && callNameValid && genderValid && birthValid && personalityValid && imageValid;
        submitButton.interactable = canSubmit;
    }
    
    private void LogMessage(string message)
    {
        Debug.Log(message);
        
        // UI에도 표시 (선택사항)
        if (debugText != null)
        {
            debugText.text = message;
        }
    }
    
    public void OnSubmitClicked()
    {
        LogMessage("🐶 강아지 등록 시작");
        
        if (!ValidateInput())
            return;
            
        StartCoroutine(RegisterDog());
    }
    
    private bool ValidateInput()
    {
        if (string.IsNullOrEmpty(dogNameInputField.text.Trim()))
        {
            LogMessage("❌ 강아지 이름을 입력해주세요.");
            return false;
        }
        
        if (string.IsNullOrEmpty(callNameInputField.text.Trim()))
        {
            LogMessage("❌ 애칭을 입력해주세요.");
            return false;
        }
        
        if (string.IsNullOrEmpty(selectedGender))
        {
            LogMessage("❌ 성별을 선택해주세요.");
            return false;
        }
        
        if (yearDropdown.value < 0 || monthDropdown.value < 0 || dayDropdown.value < 0)
        {
            LogMessage("❌ 생년월일을 모두 선택해주세요.");
            return false;
        }
        
        if (personality1Dropdown.value == 0)
        {
            LogMessage("❌ 최소 하나의 성격을 선택해주세요.");
            return false;
        }
        
        if (!isImageSelected || mainImageTexture == null)
        {
            LogMessage("❌ 메인 이미지를 선택해주세요.");
            return false;
        }
        
        return true;
    }
    
    private IEnumerator RegisterDog()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        
        // JWT 토큰 가져오기
        string accessToken = PlayerPrefs.GetString("AccessToken", "");
        if (string.IsNullOrEmpty(accessToken))
            accessToken = PlayerPrefs.GetString("jwtToken", "");
        if (string.IsNullOrEmpty(accessToken))
            accessToken = PlayerPrefs.GetString("authToken", "");
            
        if (string.IsNullOrEmpty(accessToken))
        {
            LogMessage("❌ 액세스 토큰을 찾을 수 없습니다. 로그인을 다시 시도해주세요.");
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }
        
        // Multipart form data 생성
        WWWForm form = new WWWForm();
        
        try
        {
            // 기본 데이터 추가
            form.AddField("name", dogNameInputField.text.Trim());
            form.AddField("callName", callNameInputField.text.Trim());
            form.AddField("gender", selectedGender);
            
            // 생년월일 조합 수정 (패딩 추가)
            string year = yearDropdown.options[yearDropdown.value].text;
            string month = monthDropdown.options[monthDropdown.value].text.PadLeft(2, '0');
            string day = dayDropdown.options[dayDropdown.value].text.PadLeft(2, '0');
            string birthDate = $"{year}-{month}-{day}";
            form.AddField("birth", birthDate);
            
            // 성격 ID 추가
            string personality1Name = personality1Dropdown.options[personality1Dropdown.value].text;
            form.AddField("personalityId1", personalityMap[personality1Name].ToString());
            
            if (personality2Dropdown.value > 0)
            {
                string personality2Name = personality2Dropdown.options[personality2Dropdown.value].text;
                form.AddField("personalityId2", personalityMap[personality2Name].ToString());
            }
            
            // 메인 이미지 추가
            byte[] mainImageBytes = mainImageTexture.EncodeToJPG();
            form.AddBinaryData("mainImage", mainImageBytes, "main_image.jpg", "image/jpeg");
        }
        catch (Exception e)
        {
            LogMessage($"❌ 요청 데이터 준비 중 오류 발생: {e.Message}");
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }
        
        // HTTP 요청
        using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/api/dog/create", form))
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.timeout = 30;
            
            yield return request.SendWebRequest();
            
            // 응답 처리
            if (request.result == UnityWebRequest.Result.Success)
            {
                LogMessage("✅ 강아지 등록 성공!");
                
                string response = request.downloadHandler.text;
                
                if (long.TryParse(response, out long customId))
                {
                    PlayerPrefs.SetInt("CustomId", (int)customId);
                    PlayerPrefs.Save();
                    LogMessage($"💾 CustomId 저장 완료: {customId}");
                }
                else
                {
                    LogMessage($"❌ CustomId 파싱 실패: {response}");
                }
                
                LogMessage($"🎉 '{dogNameInputField.text.Trim()}' 강아지 등록 완료!");
                
                // 다음 씬으로 이동
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                LogMessage($"❌ 강아지 등록 실패: {request.responseCode}");
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    LogMessage($"서버 메시지: {request.downloadHandler.text}");
                }
            }
        }
        
        // 로딩 패널 숨김
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
} 