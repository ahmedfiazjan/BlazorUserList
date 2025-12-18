using System.Net.Http.Json;
using BlazorUserList.Models;

namespace BlazorUserList.Services;

public class UserService
{
    private readonly HttpClient _httpClient;
    private List<User>? _cachedUsers;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<User>> LoadUsersAsync()
    {
        if (_cachedUsers != null)
            return _cachedUsers;

        try
        {
            _cachedUsers = await _httpClient.GetFromJsonAsync<List<User>>("data/users.json");
            return _cachedUsers ?? new List<User>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading users: {ex.Message}");
            return new List<User>();
        }
    }

    public List<User> FilterUsers(List<User> users, UserFilterCriteria criteria)
    {
        var filtered = users.AsEnumerable();

        // 1. Global Search (Name or Email)
        if (!string.IsNullOrWhiteSpace(criteria.GlobalSearch))
        {
            var search = criteria.GlobalSearch.ToLower();
            filtered = filtered.Where(u =>
                u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search)
            );
        }

        // 1.1 Full Name Filter (Specific)
        if (!string.IsNullOrWhiteSpace(criteria.FullNameQuery))
        {
            var nameSearch = criteria.FullNameQuery.ToLower();
            filtered = filtered.Where(u => u.FullName.ToLower().Contains(nameSearch));
        }

        // 2. Roles Filter (Any selected role matches)
        if (criteria.SelectedRoles != null && criteria.SelectedRoles.Any())
        {
            filtered = filtered.Where(u => u.Roles.Any(r => criteria.SelectedRoles.Contains(r)));
        }

        // 3. License Filter
        if (!string.IsNullOrWhiteSpace(criteria.SelectedLicense))
        {
            filtered = filtered.Where(u =>
                u.License.Equals(criteria.SelectedLicense, StringComparison.OrdinalIgnoreCase)
            );
        }

        // 4. Email Filter (Specific column search)
        if (!string.IsNullOrWhiteSpace(criteria.EmailQuery))
        {
            filtered = filtered.Where(u =>
                u.Email.Contains(criteria.EmailQuery, StringComparison.OrdinalIgnoreCase)
            );
        }

        // 5. Status Filter
        if (!string.IsNullOrWhiteSpace(criteria.SelectedStatus) && criteria.SelectedStatus != "All")
        {
            filtered = filtered.Where(u =>
                u.Status.Equals(criteria.SelectedStatus, StringComparison.OrdinalIgnoreCase)
            );
        }

        return filtered.ToList();
    }

    public (List<User> PagedUsers, int TotalRecords) GetPagedUsers(
        List<User> users,
        int pageNumber,
        int pageSize
    )
    {
        var totalRecords = users.Count;
        var pagedUsers = users.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return (pagedUsers, totalRecords);
    }
}
