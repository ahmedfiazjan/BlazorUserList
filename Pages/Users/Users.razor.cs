using BlazorUserList.Models;
using BlazorUserList.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorUserList.Pages.Users;

public partial class Users : ComponentBase, IAsyncDisposable
{
    [Inject]
    protected UserService UserService { get; set; } = default!;

    [Inject]
    protected IJSRuntime JsRuntime { get; set; } = default!;

    // Private fields
    private List<User> _allUsers = new();
    private List<User> _filteredUsers = new();
    private List<User> _pagedUsers = new();

    // Filter State
    private string _globalSearch = string.Empty;
    private List<string> _selectedRoles = new();
    private string _selectedLicense = string.Empty;
    private string _filteredEmail = string.Empty;
    private string _filteredFullName = string.Empty;
    private string _selectedStatus = "All";

    // Data for Dropdowns
    private List<string> _availableRoles = new();

    // Dropdown UI State
    private bool _fullNameDropdownOpen;
    private bool _roleDropdownOpen;
    private bool _licenseDropdownOpen;
    private bool _emailDropdownOpen;
    private bool _statusDropdownOpen;

    // Computed: Active Filter Count
    private int ActiveFilterCount
    {
        get
        {
            int count = 0;
            if (_selectedRoles.Count > 0)
                count++;
            if (!string.IsNullOrWhiteSpace(_selectedLicense))
                count++;
            if (!string.IsNullOrWhiteSpace(_filteredEmail))
                count++;
            if (!string.IsNullOrWhiteSpace(_filteredFullName))
                count++;
            if (!string.IsNullOrWhiteSpace(_selectedStatus) && _selectedStatus != "All")
                count++;
            return count;
        }
    }

    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalRecords;
    private int _totalPages;
    private int _startRecord;
    private int _endRecord;
    private bool _isLoading = true;
    private bool _gridInitialized;

    private CancellationTokenSource? _debounceTokenSource;

    protected override async Task OnInitializedAsync()
    {
        _allUsers = await UserService.LoadUsersAsync();

        // Extract distinct roles from data
        _availableRoles = _allUsers.SelectMany(u => u.Roles).Distinct().OrderBy(r => r).ToList();

        ApplyFiltersAndPagination();
        _isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Initialize grid once (even with empty data)
        if (!_gridInitialized && !_isLoading)
        {
            await InitializeAgGridAsync();
        }
    }

    private async Task InitializeAgGridAsync()
    {
        try
        {
            // Initialize with current data
            var success = await JsRuntime.InvokeAsync<bool>(
                "UsersGridManager.initializeGrid",
                "usersGrid",
                _pagedUsers
            );

            if (success)
            {
                _gridInitialized = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Ag-Grid: {ex.Message}");
        }
    }

    private async Task UpdateGridDataAsync()
    {
        if (_gridInitialized)
        {
            await JsRuntime.InvokeVoidAsync("UsersGridManager.updateData", _pagedUsers);
        }
    }

    // Handle search input with 300ms debounce
    private async Task OnSearchChangedAsync()
    {
        _debounceTokenSource?.Cancel();
        _debounceTokenSource?.Dispose();
        _debounceTokenSource = new CancellationTokenSource();

        var token = _debounceTokenSource.Token;

        try
        {
            await Task.Delay(300, token);

            if (!token.IsCancellationRequested)
            {
                _currentPage = 1;
                await ApplyFiltersAndPaginationAsync();
            }
        }
        catch (TaskCanceledException)
        {
            // Ignored
        }
    }

    // Toggle Dropdowns
    private void ToggleFullNameDropdown()
    {
        if (!_fullNameDropdownOpen)
            CloseDropdowns();
        _fullNameDropdownOpen = !_fullNameDropdownOpen;
    }

    private void ToggleRoleDropdown()
    {
        if (!_roleDropdownOpen)
            CloseDropdowns();
        _roleDropdownOpen = !_roleDropdownOpen;
    }

    private void ToggleLicenseDropdown()
    {
        if (!_licenseDropdownOpen)
            CloseDropdowns();
        _licenseDropdownOpen = !_licenseDropdownOpen;
    }

    private void ToggleEmailDropdown()
    {
        if (!_emailDropdownOpen)
            CloseDropdowns();
        _emailDropdownOpen = !_emailDropdownOpen;
    }

    private void ToggleStatusDropdown()
    {
        if (!_statusDropdownOpen)
            CloseDropdowns();
        _statusDropdownOpen = !_statusDropdownOpen;
    }

    private void CloseDropdowns()
    {
        _fullNameDropdownOpen = false;
        _roleDropdownOpen = false;
        _licenseDropdownOpen = false;
        _emailDropdownOpen = false;
        _statusDropdownOpen = false;
    }

    // Filter Logic Methods
    private async Task OnFullNameChanged(ChangeEventArgs e)
    {
        _filteredFullName = e.Value?.ToString() ?? string.Empty;
        _currentPage = 1;
        await ApplyFiltersAndPaginationAsync();
    }

    private async Task OnEmailChanged(ChangeEventArgs e)
    {
        _filteredEmail = e.Value?.ToString() ?? string.Empty;
        _currentPage = 1;
        await ApplyFiltersAndPaginationAsync();
    }

    private async Task ToggleRole(string role)
    {
        if (_selectedRoles.Contains(role))
            _selectedRoles.Remove(role);
        else
            _selectedRoles.Add(role);

        _currentPage = 1;
        await ApplyFiltersAndPaginationAsync();
    }

    private async Task SelectLicense(string license)
    {
        _selectedLicense = _selectedLicense == license ? string.Empty : license;
        _licenseDropdownOpen = false;
        _currentPage = 1;
        await ApplyFiltersAndPaginationAsync();
    }

    private async Task SelectStatus(string status)
    {
        _selectedStatus = status;
        CloseDropdowns();
        _currentPage = 1;
        await ApplyFiltersAndPaginationAsync();
    }

    private async Task ClearAllFilters()
    {
        _selectedRoles.Clear();
        _selectedLicense = string.Empty;
        _filteredEmail = string.Empty;
        _filteredFullName = string.Empty;
        _selectedStatus = "All";
        _globalSearch = string.Empty;

        CloseDropdowns();
        _currentPage = 1;
        await ApplyFiltersAndPaginationAsync();
    }

    private async Task OnPageSizeChangedAsync()
    {
        _currentPage = 1;
        await ApplyFiltersAndPaginationAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            await ApplyFiltersAndPaginationAsync();
        }
    }

    private async Task NextPageAsync()
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            await ApplyFiltersAndPaginationAsync();
        }
    }

    private async Task ApplyFiltersAndPaginationAsync()
    {
        ApplyFiltersAndPagination();
        await UpdateGridDataAsync();
        StateHasChanged();
    }

    private void ApplyFiltersAndPagination()
    {
        var criteria = new UserFilterCriteria
        {
            GlobalSearch = _globalSearch,
            SelectedRoles = _selectedRoles,
            SelectedLicense = _selectedLicense,
            EmailQuery = _filteredEmail,
            SelectedStatus = _selectedStatus,
            FullNameQuery = _filteredFullName,
        };

        _filteredUsers = UserService.FilterUsers(_allUsers, criteria);

        var result = UserService.GetPagedUsers(_filteredUsers, _currentPage, _pageSize);
        _pagedUsers = result.PagedUsers;
        _totalRecords = result.TotalRecords;

        _totalPages = _totalRecords > 0 ? (int)Math.Ceiling((double)_totalRecords / _pageSize) : 1;
        _startRecord = _totalRecords > 0 ? (_currentPage - 1) * _pageSize + 1 : 0;
        _endRecord = Math.Min(_currentPage * _pageSize, _totalRecords);
    }

    private async Task ExportToCsvAsync()
    {
        if (_filteredUsers == null || _filteredUsers.Count == 0)
        {
            return;
        }

        await JsRuntime.InvokeVoidAsync("exportToCsv", _filteredUsers, "users.csv");
    }

    public async ValueTask DisposeAsync()
    {
        _debounceTokenSource?.Cancel();
        _debounceTokenSource?.Dispose();

        if (_gridInitialized && JsRuntime is not null)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("UsersGridManager.destroyGrid");
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        GC.SuppressFinalize(this);
    }
}
