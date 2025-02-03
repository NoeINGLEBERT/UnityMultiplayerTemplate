using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;
using UnityEngine;
using TMPro;

public class PlayFabAuth : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput; // For username input
    [SerializeField] private TMP_InputField emailInput; // For email input
    [SerializeField] private TMP_InputField passwordInput; // For password input
    [SerializeField] private TMP_Text feedbackText; // For displaying feedback messages

    public static string AvatarUrl { get; private set; } = "";


    public void Register()
    {
        if (string.IsNullOrEmpty(emailInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            feedbackText.text = "Email and Password cannot be empty.";
            return;
        }

        if (!IsValidEmail(emailInput.text))
        {
            feedbackText.text = "Invalid email format.";
            return;
        }

        if (passwordInput.text.Length < 6)
        {
            feedbackText.text = "Password must be at least 6 characters long.";
            return;
        }

        var registerRequest = new RegisterPlayFabUserRequest
        {
            Email = emailInput.text,
            Password = passwordInput.text,
            Username = usernameInput.text,
            DisplayName = usernameInput.text
        };

        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, OnRegisterSuccess, OnRegisterFailure);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        feedbackText.text = "Registration successful! You can now log in.";
        Debug.Log("PlayFab Registration Success: " + result.PlayFabId);

        // Set default MMR
        var updateRequest = new UpdateUserDataRequest
        {
            Data = new System.Collections.Generic.Dictionary<string, string>
            {
                { "MMR", "1000" } // Default MMR value
            }
        };

        PlayFabClientAPI.UpdateUserData(updateRequest,
            updateResult => Debug.Log("Default MMR set during registration."),
            error => Debug.LogError("Failed to set default MMR: " + error.GenerateErrorReport()));

        Login();
    }

    private void OnRegisterFailure(PlayFabError error)
    {
        feedbackText.text = "Registration failed: " + error.ErrorMessage;
        Debug.LogError("Error during registration: " + error.GenerateErrorReport());
    }

    public void Login()
    {
        if (string.IsNullOrEmpty(usernameInput.text) && string.IsNullOrEmpty(emailInput.text))
        {
            feedbackText.text = "Please enter either a username or an email.";
            return;
        }

        if (string.IsNullOrEmpty(passwordInput.text))
        {
            feedbackText.text = "Password cannot be empty.";
            return;
        }

        // Determine if the input is an email or username
        if (!string.IsNullOrEmpty(emailInput.text) && IsValidEmail(emailInput.text))
        {
            // Login with email
            var loginRequest = new LoginWithEmailAddressRequest
            {
                Email = emailInput.text,
                Password = passwordInput.text
            };

            PlayFabClientAPI.LoginWithEmailAddress(loginRequest, OnLoginSuccess, OnLoginFailure);
        }
        else if (!string.IsNullOrEmpty(usernameInput.text))
        {
            // Login with username
            var loginRequest = new LoginWithPlayFabRequest
            {
                Username = usernameInput.text,
                Password = passwordInput.text
            };

            PlayFabClientAPI.LoginWithPlayFab(loginRequest, OnLoginSuccess, OnLoginFailure);
        }
        else
        {
            feedbackText.text = "Invalid email or username format.";
        }
    }

    private void OnLoginSuccess(LoginResult result)
    {
        feedbackText.text = "Login successful! Connecting to Photon...";
        Debug.Log("PlayFab Login Success: " + result.PlayFabId);

        // Retrieve avatar URL and set Photon nickname
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
            accountInfo =>
            {
                AvatarUrl = accountInfo.AccountInfo.TitleInfo.AvatarUrl ?? "";
                PhotonNetwork.NickName = accountInfo.AccountInfo.TitleInfo.DisplayName ?? "Unknown";
                ConnectToPhoton();
            },
            error => Debug.LogError($"Failed to get account info: {error.GenerateErrorReport()}"));
    }

    private void OnLoginFailure(PlayFabError error)
    {
        feedbackText.text = "Login failed: " + error.ErrorMessage;
        Debug.LogError("Error during login: " + error.GenerateErrorReport());
    }

    private void ConnectToPhoton()
    {
        PhotonNetwork.ConnectUsingSettings(); // Connect to Photon server
    }
}
