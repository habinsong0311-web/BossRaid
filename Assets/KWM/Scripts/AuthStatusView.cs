using Firebase.Auth;
using TMPro;
using UnityEngine;

public class AuthStatusView : MonoBehaviour
{

    private void OnEnable()
    {
        FirebaseAuthService.OnAuthChanged += HandleAuth;
    }

    private void Start()
    {
        HandleAuth(FirebaseBootstrap.Auth?.CurrentUser);
    }

    private void HandleAuth(FirebaseUser user)
    {
        if (user == null)
            Debug.Log("로그아웃 상태");
        else
            Debug.Log($"로그인 상태 : {user.UserId}");
    }    

    private void OnDisable()
    {
        FirebaseAuthService.OnAuthChanged -= HandleAuth;
    }

}
