using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

// Whisper 패키지 임포트
using Whisper.Utils;

namespace Whisper.Samples
{
public class DogVoiceCommandController : MonoBehaviour
{
        [Header("STT Components")]
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;

        [Header("UI")]
    public Button recordButton;
        public TextMeshProUGUI buttonText;
        public TextMeshProUGUI statusText;
        
        [Header("강아지 상호작용 설정")]
        public DogInteractionController dogInteractionController;
        
        [Header("음성 인식 설정")]
        [SerializeField] private int maxLevenshteinDistance = 2;
        [SerializeField] private bool showDebugLogs = true;
        
        // 강아지 훈련 명령어 목록
    private readonly string[] _commandWords = {
            "앉아", "앉기", "손", "손줘", "엎드려", "누워", "다운"
        };
        
        // 명령어 매핑
        private readonly Dictionary<string, string> _commandNormalization = new Dictionary<string, string>
        {
            {"앉아", "앉아"}, {"앉기", "앉아"},
            {"손", "손"}, {"손줘", "손"},
            {"엎드려", "엎드려"}, {"누워", "엎드려"}, {"다운", "엎드려"}
        };
        
        // 내부 상태 변수들
        private bool _isProcessing = false;
        private float recordStartTime = 0f;           // 추가: 녹음 시작 시간
        private float minimumRecordTime = 0.5f;       // 추가: 최소 녹음 시간

        // 클래스 상단에 static 변수 추가
        // public static bool isRecordingButtonPressed = false; // 제거

        private void Awake()
        {
            InitializeComponents();
            SetupEventListeners();
            ConfigureWhisper();
        }

        private void InitializeComponents()
        {
            // DogInteractionController 자동 찾기
            if (dogInteractionController == null)
                dogInteractionController = FindObjectOfType<DogInteractionController>();
            
            // 자동 컴포넌트 찾기
            if (whisper == null)
                whisper = GetComponent<WhisperManager>();
                
            if (microphoneRecord == null)
                microphoneRecord = GetComponent<MicrophoneRecord>();
            
            // 필수 컴포넌트 확인
            if (dogInteractionController == null)
                Debug.LogError("DogInteractionController를 찾을 수 없습니다!");
                
            if (whisper == null)
                Debug.LogError("WhisperManager가 설정되지 않았습니다!");
                
            if (microphoneRecord == null)
                Debug.LogError("MicrophoneRecord가 설정되지 않았습니다!");
        }

        private void SetupEventListeners()
        {
            // STT 이벤트 연결
            if (microphoneRecord != null)
                microphoneRecord.OnRecordStop += OnRecordStop;
            
            // 버튼 이벤트 연결
            if (recordButton != null)
                recordButton.onClick.AddListener(OnButtonPressed);
        }

        private void ConfigureWhisper()
        {
            if (whisper != null)
            {
                whisper.language = "ko"; // 한국어로 고정
                whisper.translateToEnglish = false;
            }
        }

        private void Start()
        {
            UpdateUI();
            ShowStatus("🎤 녹음 버튼을 눌러 강아지에게 명령하세요\n(앉아, 손, 엎드려)");
        }

        private void OnButtonPressed()
        {
            if (_isProcessing)
            {
                ShowStatus("⏳ 처리 중입니다. 잠시만 기다려주세요...");
                return;
            }

            if (microphoneRecord == null)
            {
                ShowStatus("❌ 마이크 컴포넌트가 없습니다.");
                return;
            }

            if (!microphoneRecord.IsRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        private void StartRecording()
        {
            try
            {
                if (Microphone.devices.Length == 0)
                {
                    ShowStatus("❌ 마이크 장치를 찾을 수 없습니다.");
                    return;
                }

                if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                {
                    ShowStatus("❌ 마이크 권한이 필요합니다.");
                    return;
                }

                microphoneRecord.StartRecord();
                recordStartTime = Time.time;
                UpdateButtonText("🛑 녹음 중지");
                ShowStatus("🎤 듣고 있습니다... 명령을 말해주세요\n(최소 0.5초 이상 말해주세요)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"녹음 시작 실패: {e.Message}");
                ShowStatus("❌ 녹음 시작에 실패했습니다. 마이크를 확인해주세요.");
            }
        }

        private void StopRecording()
        {
            try
            {
                if (!microphoneRecord.IsRecording)
                {
                    ShowStatus("⚠️ 녹음이 진행 중이 아닙니다.");
                    return;
                }

                float recordDuration = Time.time - recordStartTime;
                if (recordDuration < minimumRecordTime)
                {
                    ShowStatus($"⚠️ 너무 짧습니다. 최소 {minimumRecordTime}초 이상 녹음해주세요.");
                    return;
                }

                microphoneRecord.StopRecord();
                UpdateButtonText("🎤 녹음 시작");
                ShowStatus("🔄 음성을 분석 중입니다...");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"녹음 중지 실패: {e.Message}");
                ShowStatus("❌ 녹음 중지에 실패했습니다.");
                
                _isProcessing = false;
                UpdateButtonText("🎤 녹음 시작");
                StartCoroutine(ResetStatusAfterDelay(3f));
            }
        }
        
        private async void OnRecordStop(AudioChunk recordedAudio)
        {
            if (_isProcessing) return;
            
            _isProcessing = true;
            UpdateButtonText("⏳ 처리 중...");
            
            try
            {
                if (whisper == null)
                {
                    ShowStatus("❌ WhisperManager가 없습니다.");
                    return;
                }

                var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
                
                if (res == null || string.IsNullOrEmpty(res.Result))
                {
                    ShowStatus("❌ 음성을 인식하지 못했습니다. 다시 시도해주세요.");
                    return;
                }

                var rawText = res.Result.Trim();
                var correctedCommand = CorrectWithLevenshtein(rawText);
                
                if (ExecuteVoiceCommand(correctedCommand))
                {
                    ShowStatus($"✅ '{correctedCommand}' 명령을 실행했습니다!");
                }
                else
                {
                    ShowStatus($"❓ '{rawText}'는 알 수 없는 명령입니다.\n사용 가능한 명령: 앉아, 손, 엎드려");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"음성 처리 중 오류 발생: {e.Message}");
                ShowStatus("❌ 음성 처리 중 오류가 발생했습니다.");
            }
            finally
            {
                _isProcessing = false;
                UpdateButtonText("🎤 녹음 시작");
                StartCoroutine(ResetStatusAfterDelay(3f));
            }
        }
        
        private bool ExecuteVoiceCommand(string command)
        {
            if (string.IsNullOrEmpty(command) || dogInteractionController == null)
                return false;
            
            try
            {
                switch (command)
                {
                    case "앉아":
                        dogInteractionController.PlaySitAnimation();
                        break;
                    case "손":
                        dogInteractionController.PlayHandAnimation();
                        break;
                    case "엎드려":
                        dogInteractionController.PlayLieDownAnimation();
                        break;
                    default:
                        return false;
                }
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"명령 실행 중 오류: {e.Message}");
                return false;
            }
        }
        
        private string CorrectWithLevenshtein(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            var words = input.Split(new char[] { ' ', ',', '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                var cleanWord = word.Trim().ToLower();
                var bestMatch = FindBestMatch(cleanWord, _commandWords, maxLevenshteinDistance);
                
                if (bestMatch != null && _commandNormalization.ContainsKey(bestMatch))
                {
                    return _commandNormalization[bestMatch];
                }
            }
            
            return input;
        }
        
        private string FindBestMatch(string input, string[] candidates, int maxDistance)
        {
            string bestMatch = null;
            int minDistance = int.MaxValue;
            
            foreach (var candidate in candidates)
            {
                int distance = LevenshteinDistance(input.ToLower(), candidate.ToLower());
                if (distance <= maxDistance && distance < minDistance)
                {
                    minDistance = distance;
                    bestMatch = candidate;
                }
            }
            
            return bestMatch;
        }
        
        private int LevenshteinDistance(string s1, string s2)
        {
            if (s1.Length == 0) return s2.Length;
            if (s2.Length == 0) return s1.Length;
            
            var d = new int[s1.Length + 1, s2.Length + 1];
            
            for (int i = 0; i <= s1.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++) d[0, j] = j;
            
            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    d[i, j] = System.Math.Min(System.Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            
            return d[s1.Length, s2.Length];
        }
        
        private IEnumerator ResetStatusAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowStatus("🎤 녹음 버튼을 눌러 강아지에게 명령하세요\n(앉아, 손, 엎드려)");
        }

        // private System.Collections.IEnumerator ResetRecordingButtonFlag() // 제거
        // {
        //     yield return new WaitForSeconds(0.2f);
        //     isRecordingButtonPressed = false;
        // }
        
        private void UpdateButtonText(string text)
        {
            if (buttonText != null)
                buttonText.text = text;
        }
        
        private void ShowStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
        
        private void UpdateUI()
        {
            UpdateButtonText("🎤 녹음 시작");
        }
        
        void OnDestroy()
        {
            if (microphoneRecord != null)
                microphoneRecord.OnRecordStop -= OnRecordStop;
        }
    }
}