using UnityEngine;
using System;

namespace TogedaengData
{
    /// <summary>
    /// OAuth 소셜 로그인 사용자 정보
    /// </summary>
    [System.Serializable]
    public class OAuthUserInfo
    {
        public string email;
        public string provider;
        public string providerId;
    }

    /// <summary>
    /// OAuth Authorization Code 요청 데이터
    /// </summary>
    [System.Serializable]
    public class AuthorizationCodeRequest
    {
        public string code;
        public string redirectUri;
    }

    /// <summary>
    /// JWT 토큰 응답 데이터
    /// </summary>
    [System.Serializable]
    public class TokenResponse
    {
        public string accessToken;
        public string refreshToken;
    }

    /// <summary>
    /// 사용자 추가 정보 입력 요청 데이터
    /// </summary>
    [System.Serializable]
    public class UserInfoRequest
    {
        public string email;
        public string provider;
        public string providerId;
        public string nickname;
        public string gender;
        public string birth;
    }

    /// <summary>
    /// 닉네임 중복 확인 응답 데이터
    /// </summary>
    [System.Serializable]
    public class NicknameCheckResponse
    {
        public bool isAvailable;
    }

    /// <summary>
    /// 백엔드에서 받은 사용자 데이터
    /// </summary>
    [System.Serializable]
    public class UserData
    {
        public long id;
        public string nickname;
        public string gender;
        public string birth;
        public string email;
        public string provider;
        public string status;
        public string createdAt;
    }

    /// <summary>
    /// 회원가입/로그인 완료 후 전체 응답 데이터
    /// </summary>
    [System.Serializable]
    public class AuthResponse
    {
        public UserData user;
        public TokenResponse token;
    }

    /// <summary>
    /// API 에러 응답 데이터
    /// </summary>
    [System.Serializable]
    public class ErrorResponse
    {
        public string message;
        public int code;
        public string timestamp;
    }

    /// <summary>
    /// 일반적인 API 성공 응답 데이터
    /// </summary>
    [System.Serializable]
    public class ApiResponse<T>
    {
        public bool success;
        public T data;
        public string message;
    }
}

public class CommonDataTypes : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
