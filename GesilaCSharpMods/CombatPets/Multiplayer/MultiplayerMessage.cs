namespace CombatPets;

internal static class MultiplayerMessageType
{
    public const string ToggleFollowRequest = "ToggleFollowRequest";
    public const string ToggleFollowResult = "ToggleFollowResult";
    public const string AttackEffect = "AttackEffect";
    public const string PetHitEffect = "PetHitEffect";
    public const string RefreshRegistry = "RefreshRegistry";
}

public sealed class RefreshRegistryMessage
{
}

public sealed class ToggleFollowRequestMessage
{
    public string PetId { get; set; } = "";
}

public sealed class ToggleFollowResultMessage
{
    public string PetId { get; set; } = "";
    public string PetName { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public bool Success { get; set; }
    public bool IsFollowing { get; set; }
    public string ErrorCode { get; set; } = "";

    internal static ToggleFollowResultMessage SuccessToggle(PetManager manager, bool isFollowing)
    {
        return new ToggleFollowResultMessage
        {
            PetId = manager.PetId,
            PetName = manager.pet.Name,
            Success = true,
            IsFollowing = isFollowing
        };
    }

    internal static ToggleFollowResultMessage Failure(string petId, string petName, string errorCode)
    {
        return new ToggleFollowResultMessage
        {
            PetId = petId,
            PetName = petName,
            Success = false,
            ErrorCode = errorCode
        };
    }
}

public sealed class AttackEffectMessage
{
    public string LocationName { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Flipped { get; set; }
}

public sealed class PetHitEffectMessage
{
    public string PetId { get; set; } = "";
    public string PetName { get; set; } = "";
    public string LocationName { get; set; } = "";
    public int Damage { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public int InvincibleTicks { get; set; }
    public int AttackedTicks { get; set; }
    public bool Defeated { get; set; }
}

