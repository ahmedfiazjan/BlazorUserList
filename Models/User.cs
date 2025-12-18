namespace BlazorUserList.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string License { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime LastActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InvitedBy { get; set; } = string.Empty;

    // Computed properties for UI
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName))
                return "??";

            var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0][..Math.Min(2, parts[0].Length)].ToUpper();

            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }
    }

    public string FormattedLastActive => LastActive.ToString("MMM dd, yyyy - HH:mm");

    public string RolesDisplay
    {
        get
        {
            if (Roles == null || Roles.Count == 0)
                return "No roles";

            if (Roles.Count == 1)
                return Roles[0];

            return $"{Roles[0]} +{Roles.Count - 1} more";
        }
    }

    public bool WasInvited => !string.IsNullOrWhiteSpace(InvitedBy);
}
