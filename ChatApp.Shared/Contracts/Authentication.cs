using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.Shared.Contracts;

public sealed record FirebaseLoginRequest(string IdToken, string? Username, string? DisplayName);
public sealed record AuthenticationResult(UserSummary User);
public sealed record EmptyResult();