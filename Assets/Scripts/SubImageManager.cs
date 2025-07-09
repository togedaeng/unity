using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class SubImageManager : MonoBehaviour
{
    [Header("서버 설정")]
    [SerializeField] private string serverUrl = "http://localhost:8080";
    
    [Header("서브 이미지 버튼들")]
    [SerializeField] private Button subImage1Button;
    [SerializeField] private Button subImage2Button;
    [SerializeField] private Button subImage3Button;
    
    [Header("UI 패널")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Button submitButton;
    
    [Header("씬 전환")]
    [SerializeField] private string nextSceneName = "Scene6"; // 다음 씬 이름
    
    // 이미지 상태 관리
    private Texture2D[] subImageTextures = new Texture2D[3];
    private bool[] isImageSelected = new bool[3];
    
    // 버튼 초기 상태 저장
    private Sprite[] originalButtonSprites = new Sprite[3];
    private Color[] originalButtonColors = new Color[3];
    
    // CustomId
    private int customId;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupUI();
        SetupEventListeners();
        LoadCustomId();
        SaveOriginalButtonStates();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LoadCustomId()
    {
        customId = PlayerPrefs.GetInt("CustomId", -1);
        if (customId == -1)
        {
            Debug.LogError("❌ CustomId를 찾을 수 없습니다. Scene4에서 강아지 등록을 다시 시도해주세요.");
        }
        else
        {
            Debug.Log($"✅ CustomId 로드 완료: {customId}");
        }
    }
    
    private void SaveOriginalButtonStates()
    {
        Button[] buttons = { subImage1Button, subImage2Button, subImage3Button };
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    originalButtonSprites[i] = buttonImage.sprite;
                    originalButtonColors[i] = buttonImage.color;
                }
            }
        }
    }
    
    private void SetupUI()
    {
        // 로딩 패널 숨김
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
            
        // 이미지 버튼들 초기화
        ResetAllImageButtons();
        
        // 제출 버튼 활성화 (서브 이미지는 선택사항이므로)
        if (submitButton != null)
            submitButton.interactable = true;
    }
    
    private void ResetAllImageButtons()
    {
        Button[] buttons = { subImage1Button, subImage2Button, subImage3Button };
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.sprite = originalButtonSprites[i];
                    buttonImage.color = originalButtonColors[i];
                }
            }
            isImageSelected[i] = false;
            subImageTextures[i] = null;
        }
    }
    
    private void SetupEventListeners()
    {
        // 서브 이미지 버튼들
        subImage1Button.onClick.AddListener(() => OnSubImageButtonClicked(0));
        subImage2Button.onClick.AddListener(() => OnSubImageButtonClicked(1));
        subImage3Button.onClick.AddListener(() => OnSubImageButtonClicked(2));
        
        // 제출 버튼
        submitButton.onClick.AddListener(OnSubmitClicked);
    }
    
    private void OnSubImageButtonClicked(int index)
    {
        CreateDummySubImage(index);
    }
    
    private void CreateDummySubImage(int index)
    {
        // 테스트용 더미 이미지 생성 (각기 다른 색상)
        Texture2D dummyTexture = new Texture2D(400, 300, TextureFormat.RGB24, false);
        Color[] pixels = new Color[400 * 300];
        
        // 각 이미지마다 다른 색상
        Color[] colors = {
            new Color(1f, 0.6f, 0.6f), // 빨간색 계열
            new Color(0.6f, 1f, 0.6f), // 초록색 계열
            new Color(0.6f, 0.6f, 1f)  // 파란색 계열
        };
        
        Color imageColor = colors[index];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = imageColor;
        }
        
        dummyTexture.SetPixels(pixels);
        dummyTexture.Apply();
        
        subImageTextures[index] = dummyTexture;
        UpdateImageButton(index);
    }
    
    private void UpdateImageButton(int index)
    {
        Button[] buttons = { subImage1Button, subImage2Button, subImage3Button };
        
        if (subImageTextures[index] != null && buttons[index] != null)
        {
            Image buttonImage = buttons[index].GetComponent<Image>();
            if (buttonImage != null)
            {
                Sprite newSprite = Sprite.Create(
                    subImageTextures[index],
                    new Rect(0, 0, subImageTextures[index].width, subImageTextures[index].height),
                    Vector2.zero
                );
                buttonImage.sprite = newSprite;
                buttonImage.color = Color.white;
            }
            
            isImageSelected[index] = true;
        }
    }
    
    public void OnSubmitClicked()
    {
        Debug.Log("📦 서브 이미지 업로드 시작");
        
        if (customId == -1)
        {
            Debug.LogError("❌ CustomId가 유효하지 않습니다.");
            return;
        }
        
        StartCoroutine(UploadSubImages());
    }
    
    private IEnumerator UploadSubImages()
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
            Debug.LogError("❌ 액세스 토큰을 찾을 수 없습니다. 로그인을 다시 시도해주세요.");
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }
        
        // 선택된 이미지만 수집
        List<Texture2D> selectedImages = new List<Texture2D>();
        for (int i = 0; i < 3; i++)
        {
            if (isImageSelected[i] && subImageTextures[i] != null)
            {
                selectedImages.Add(subImageTextures[i]);
            }
        }
        
        // 이미지가 하나도 없으면 바로 다음 씬으로
        if (selectedImages.Count == 0)
        {
            Debug.Log("ℹ️ 선택된 서브 이미지가 없습니다. 다음 씬으로 이동합니다.");
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }
        
        Debug.Log($"📋 서브 이미지 {selectedImages.Count}장 업로드 중...");
        
        // Multipart form data 생성
        WWWForm form = new WWWForm();
        
        try
        {
            // 서브 이미지들 추가
            for (int i = 0; i < selectedImages.Count; i++)
            {
                byte[] imageBytes = selectedImages[i].EncodeToJPG();
                form.AddBinaryData("subImages", imageBytes, $"sub_image_{i + 1}.jpg", "image/jpeg");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 서브 이미지 데이터 준비 중 오류 발생: {e.Message}");
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }
        
        // HTTP 요청
        using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/api/dog/{customId}/sub-image", form))
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.timeout = 30;
            
            yield return request.SendWebRequest();
            
            // 응답 처리
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ 서브 이미지 업로드 성공!");
                Debug.Log($"🎉 서브 이미지 {selectedImages.Count}장 업로드 완료!");
                
                // 다음 씬으로 이동
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogError($"❌ 서브 이미지 업로드 실패: {request.responseCode}");
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"서버 메시지: {request.downloadHandler.text}");
                }
            }
        }
        
        // 로딩 패널 숨김
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
}
