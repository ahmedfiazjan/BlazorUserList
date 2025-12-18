namespace BlazorUserList.Models;

public class UserFilterCriteria
{
    public string GlobalSearch { get; set; } = string.Empty;
    public string FullNameQuery { get; set; } = string.Empty;
    public List<string> SelectedRoles { get; set; } = new();
    public string SelectedLicense { get; set; } = string.Empty;
    public string EmailQuery { get; set; } = string.Empty;
    public string SelectedStatus { get; set; } = string.Empty;
}
