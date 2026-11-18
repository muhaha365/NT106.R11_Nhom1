namespace ChatApp.Client;

public static class AuthMessages
{
    public static string AuthenticationErrorMessage(Exception error, string fallback)
    {
        if (error is FirebaseSignInException firebaseError)
        {
            return FirebaseErrorMessage(firebaseError.Code);
        }

        if (error is ProtocolRequestException serverError)
        {
            if (serverError.Code == "PROFILE_REQUIRED")
            {
                return "Tài khoản Firebase đã có. Mở Đăng ký, chọn Hoàn tất hồ sơ và nhập lại email/mật khẩu.";
            }

            return serverError.Message;
        }

        if (error is ArgumentException)
        {
            return error.Message;
        }

        return fallback;
    }

    public static string FirebaseErrorMessage(string code)
    {
        if(code.StartsWith("WEAK_PASSWORD", StringComparison.Ordinal)
            || code.StartsWith("PASSWORD_DOES_NOT_MEET_REQUIREMENTS", StringComparison.Ordinal))
        {
            return "Mật khẩu chưa đủ mạnh. Vui lòng chọn mật khẩu khác.";
        }
        switch (code)
        {
            case "EMAIL_EXISTS":
                return "Email đã có tài khoản. Hãy đăng nhập hoặc hoàn tất hồ sơ.";
            case "INVALID_LOGIN_CREDENTIALS":
            case "INVALID_PASSWORD":
            case "EMAIL_NOT_FOUND":
                return "Email hoặc mật khẩu không đúng.";
            case "INVALID_EMAIL":
                return "Email không hợp lệ.";
            case "USER_DISABLED":
                return "Tài khoản đã bị vô hiệu hóa.";
            case "OPERATION_NOT_ALLOWED":
                return "Cần bật Email/Password trong Firebase Console.";
            case "TOO_MANY_ATTEMPTS_TRY_LATER":
                return "Bạn thử quá nhiều lần. Vui lòng thử lại sau.";
            default:
                return "Firebase chưa xử lý được yêu cầu. Kiểm tra cấu hình và kết nối.";
        }
    }
}
