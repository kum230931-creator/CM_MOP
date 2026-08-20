using System;
using System.Collections.Generic;

namespace CMOmeets.Domain.Entities;

public partial class UserAuthenticationDetail
{
    public int Rid { get; set; }

    public string AuthId { get; set; } = null!;

    public string AuthPwd { get; set; } = null!;

    public string OfficialName { get; set; } = null!;

    public string? MobileNumber { get; set; }

    public string Designation { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public string LocationCode { get; set; } = null!;

    public string LocationName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Active { get; set; } = null!;
}
