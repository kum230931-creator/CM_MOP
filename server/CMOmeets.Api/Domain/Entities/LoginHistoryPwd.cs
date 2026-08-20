using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class LoginHistoryPwd
{
    public int Rid { get; set; }

    public string AuthId { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public DateTime LastLogin { get; set; }

    public string IpAdd { get; set; } = null!;

    public string SessionId { get; set; } = null!;

    public DateTime? LogoutTime { get; set; }

    public DateTime CreatedAt { get; set; }
}
