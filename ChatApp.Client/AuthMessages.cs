using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.Client;

public static class AuthMessages
{
    public static string FirebaseErrorMessage(string code)
    {
        if(code.StartsWith("WEAK_PASSWORD", StringComparison.Ordinal)
            || code.StartsWith("PASSWORD_DOES_NOT_MEET_REQUIREMENTS", StringComparison.Ordinal))
        {
            return "Mật khẩu chưa đủ mạnh. Vui lòng chọn mật khẩu khác.";
        }
        return code switch
        {
            "EMAIL_EXISTS" => "Email đã có tài khoản. Hãy đăng nhập hoặc hoàn tất hồ sơ.",
            "INVALID_LOGIN_CREDENTIALS" or "INVALID_PASSWORD" or "EMAIL_NOT_FOUND"
                => "Email hoặc mật khẩu không đúng.",
            "INVALID_EMAIL" => "Email không hợp lệ.",
            "USER_DISABLED" => "Tài khoản đã bị vô hiệu hóa.",
            "OPERATION_NOT_ALLOWED" => "Cần bật Email/Password trong Firebase Console.",
            "TOO_MANY_ATTEMPTS_TRY_LATER" => "Bạn thử quá nhiều lần. Vui lòng thử lại sau.",
            _ => "Firebase chưa xử lý được yêu cầu. Kiểm tra cấu hình và kết nối."
        };
    }
}
