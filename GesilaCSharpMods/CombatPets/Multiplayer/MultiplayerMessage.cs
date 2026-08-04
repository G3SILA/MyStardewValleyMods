namespace CombatPets;

internal static class MultiplayerMessageType
{
    public const string ToggleFollowRequest = "ToggleFollowRequest";
    public const string ToggleFollowResult = "ToggleFollowResult";
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
