using Firebase;
using Firebase.Auth;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class FirebaseAuthService : MonoBehaviour
{
    public static event Action<FirebaseUser> OnAuthChanged;

   [SerializeField] private TMP_InputField emailInput;
   [SerializeField] private TMP_InputField passwordInput;
   [SerializeField] private TMP_Text messageText;

    private FirebaseAuth Auth => FirebaseBootstrap.Auth;

    private async void Start()
    {
        while(Auth == null)
        {
            await Task.Yield();
        }

        if(Auth.CurrentUser != null)
        {
            OnAuthChanged?.Invoke(Auth.CurrentUser);
            Auth.SignOut();
        }
    }


    private bool TryGetAuth()
    {
        if(Auth == null)
        {
            Debug.Log("파이어베이스 아직 준비 안됨");
            
            return false;
        }
        return true;
    }

    public async void OnClickSignUp()
    {
        if (!TryGetAuth()) return;

        try
        {
            var result = await Auth.CreateUserWithEmailAndPasswordAsync(emailInput.text, passwordInput.text);
            
            OnAuthChanged?.Invoke(result.User);
        }
        
        catch (Exception ex)
        {
            Debug.LogError(ex.GetBaseException().Message);
        }
    }

    public async void OnClickSignIn()
    {
        if (!TryGetAuth()) return;

        try
        {
            var result = await Auth.SignInWithEmailAndPasswordAsync(emailInput.text, passwordInput.text);
            
            OnAuthChanged?.Invoke(result.User);
        }
        
        catch (Exception ex)
        {
            Debug.LogError(ex.GetBaseException().Message);

            if (messageText != null)
            {
                var firebaseEx = ex.GetBaseException() as Firebase.FirebaseException;

                if (firebaseEx != null)
                {
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                    switch (errorCode)
                    {
                        case AuthError.InvalidEmail:
                        case AuthError.UserNotFound:
                        case AuthError.WrongPassword:
                            messageText.text = "Invalid email or password.";
                            break;

                        case AuthError.TooManyRequests:
                            messageText.text = "Too many requests. Please try again later.";
                            break;

                        default:
                            messageText.text = "Login failed. Please try again.";
                            break;
                    }
                }
                else
                {
                    messageText.text = "Network error. Check your connection.";
                }
            }
        }
    }

    public void OnClickSignOut()
    {
        if (!TryGetAuth()) return;

        Auth.SignOut();
        
        OnAuthChanged?.Invoke(null);
    }
}
