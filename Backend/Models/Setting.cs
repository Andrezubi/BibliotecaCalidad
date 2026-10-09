using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Setting
{
    public ushort Id { get; set; }

    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ushort? UserId { get; set; }

    public virtual User? User { get; set; }
}
