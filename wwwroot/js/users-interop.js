// Ag-Grid initialization and management for Blazor Users List
window.UsersGridManager = {
    gridApi: null,

    initializeGrid: function (elementId, rowData) {
        const gridElement = document.getElementById(elementId);

        if (!gridElement) {
            console.error('Grid element not found:', elementId);
            return false;
        }

        if (typeof agGrid === 'undefined') {
            console.error('agGrid library not loaded');
            return false;
        }

        // Define column definitions
        const columnDefs = [
            {
                headerName: 'Full Name',
                field: 'fullName',
                minWidth: 220,
                pinned: 'left', // Sticky column
                lockPinned: true, // Prevent user from unpinning
                cellClass: 'pinned-left-border', // Custom class for the border divider
                sortable: true,
                comparator: (valueA, valueB) => valueA.toLowerCase().localeCompare(valueB.toLowerCase()),
                cellRenderer: params => {
                    if (!params.data) return '';
                    return `
                        <div style="display: flex; align-items: center;">
                            <div class="avatar-circle">${params.data.initials}</div>
                            <div style="display: flex; flex-direction: column;">
                                <span style="color: #111827; font-size: 0.875rem;">${params.data.fullName}</span>
                            </div>
                        </div>
                    `;
                }
            },
            {
                headerName: 'Assigned Roles',
                field: 'rolesDisplay',
                minWidth: 150,
                sortable: true,
                comparator: (valueA, valueB, nodeA, nodeB) => {
                    // Sort by the first role
                    const rolesA = nodeA.data.roles && nodeA.data.roles.length > 0 ? nodeA.data.roles[0] : '';
                    const rolesB = nodeB.data.roles && nodeB.data.roles.length > 0 ? nodeB.data.roles[0] : '';
                    return rolesA.localeCompare(rolesB);
                },
                cellRenderer: params => {
                    if (!params.data || !params.data.roles) return 'No roles';
                    const roles = params.data.roles;
                    if (roles.length === 0) return 'No roles';
                    if (roles.length === 1) return roles[0];
                    const remaining = roles.slice(1);
                    const tooltip = remaining.join('\n');
                    return `${roles[0]} <u><span style="font-weight: 700; cursor: help;" title="${tooltip}">+${remaining.length} more</span></u>`;
                }
            },
            {
                headerName: 'License',
                field: 'license',
                minWidth: 120,
                sortable: true,
                cellRenderer: params => {
                    if (!params.value) return '';
                    return `<span style="background-color: #f3f4f6; padding: 2px 8px; border-radius: 4px; color: #374151; font-weight: 500; font-size: 0.75rem;">${params.value}</span>`;
                }
            },
            {
                headerName: 'Email',
                field: 'email',
                minWidth: 220,
                sortable: true,
                cellRenderer: params => {
                    if (!params.value) return '';
                    return `<a href="mailto:${params.value}" style="color: #4f46e5; text-decoration: none;">${params.value}</a>`;
                }
            },
            {
                headerName: 'Last Active',
                field: 'formattedLastActive',
                minWidth: 50,
                sortable: true,
                comparator: (valueA, valueB, nodeA, nodeB) => {
                    const dateA = new Date(nodeA.data.lastActive).getTime();
                    const dateB = new Date(nodeB.data.lastActive).getTime();
                    return dateA - dateB;
                }
            },
            {
                headerName: 'Status',
                field: 'status',
                minWidth: 50,
                sortable: true,
                cellRenderer: params => {
                    if (!params.value) return '';
                    return `<span class="status-pill status-${params.value.toLowerCase()}">${params.value}</span>`;
                }
            },
            {
                headerName: 'Invite?',
                field: 'wasInvited',
                minWidth: 5,
                sortable: true,
                cellRenderer: params => {
                    return params.value ? 'Yes' : 'No';
                }
            },
            {
                headerName: 'Actions',
                minWidth: 80,
                width: 80,
                pinned: 'right', // Pinned right for better UX as well
                sortable: false, // No sorting for actions
                cellRenderer: params => {
                    if (!params.data) return '';
                    const userId = params.data.id;
                    return `
                        <div class="action-menu-container" style="display: flex; justify-content: center;">
                            <button onclick="toggleActionMenu('${userId}', event)" class="action-menu-btn" style="color: #6b7280; background: none; border: none; cursor: pointer; padding: 4px; border-radius: 4px;" onmouseover="this.style.backgroundColor='#f3f4f6'" onmouseout="this.style.backgroundColor='transparent'">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <circle cx="12" cy="12" r="1"></circle>
                                    <circle cx="12" cy="5" r="1"></circle>
                                    <circle cx="12" cy="19" r="1"></circle>
                                </svg>
                            </button>
                        </div>
                    `;
                }
            }
        ];

        // Grid options
        const gridOptions = {
            columnDefs: columnDefs,
            rowData: rowData,
            defaultColDef: {
                sortable: false,
                filter: false,
                resizable: true
            },
            rowHeight: 52, // Compact height
            headerHeight: 40,
            domLayout: 'normal',
            suppressCellFocus: true,
            suppressNoRowsOverlay: true, // Correctly disable "No Rows" overlay to prevent crash
            onGridReady: params => {
                this.gridApi = params.api;
                params.api.sizeColumnsToFit();
            }
        };

        try {
            agGrid.createGrid(gridElement, gridOptions);

            // Add global click listener to close menus
            if (!window.hasActionMenuListener) {
                document.addEventListener('click', function (event) {
                    // If click is NOT inside a dropdown menu, close all menus
                    if (!event.target.closest('.action-dropdown-portal')) {
                        closeAllActionMenus();
                    }
                });

                // Close on scroll too
                document.addEventListener('scroll', function () {
                    closeAllActionMenus();
                }, true);

                window.hasActionMenuListener = true;
            }

            return true;
        } catch (error) {
            console.error("Error creating grid:", error);
            return false;
        }
    },

    updateData: function (rowData) {
        if (this.gridApi) {
            this.gridApi.setGridOption('rowData', rowData);
        }
    },

    destroyGrid: function () {
        if (this.gridApi) {
            this.gridApi.destroy();
            this.gridApi = null;
        }
    },

    // Get current row data for copy functionality
    getAllRowData: function () {
        const rowData = [];
        if (this.gridApi) {
            this.gridApi.forEachNode(node => rowData.push(node.data));
        }
        return rowData;
    }
};

// Close all open action menus
function closeAllActionMenus() {
    document.querySelectorAll('.action-dropdown-portal').forEach(el => el.remove());
}

// Toggle Action Menu
window.toggleActionMenu = function (userId, event) {
    if (event) {
        event.stopPropagation();
        event.preventDefault();
    }

    // Close any existing menus first
    closeAllActionMenus();

    // Get the button element
    const button = event.currentTarget;
    const rect = button.getBoundingClientRect();

    // Create the dropdown element dynamically
    const menu = document.createElement('div');
    menu.className = 'action-dropdown-portal';
    menu.style.cssText = `
        position: fixed;
        top: ${rect.bottom + 5}px;
        left: ${rect.right - 140}px; /* Align right edge */
        background: white;
        border: 1px solid #e5e7eb;
        border-radius: 6px;
        box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
        z-index: 999999; /* Super high z-index */
        min-width: 140px;
        display: block;
        animation: fadeIn 0.1s ease-out;
    `;

    menu.innerHTML = `
        <button onclick="handleAction('View', '${userId}')" style="display: block; width: 100%; text-align: left; padding: 8px 12px; border: none; background: none; cursor: pointer; color: #374151; font-size: 0.875rem;" onmouseover="this.style.backgroundColor='#f3f4f6'" onmouseout="this.style.backgroundColor='transparent'">View</button>
        <button onclick="handleAction('Edit', '${userId}')" style="display: block; width: 100%; text-align: left; padding: 8px 12px; border: none; background: none; cursor: pointer; color: #374151; font-size: 0.875rem;" onmouseover="this.style.backgroundColor='#f3f4f6'" onmouseout="this.style.backgroundColor='transparent'">Edit</button>
        <button onclick="handleAction('Archive', '${userId}')" style="display: block; width: 100%; text-align: left; padding: 8px 12px; border: none; background: none; cursor: pointer; color: #dc2626; font-size: 0.875rem;" onmouseover="this.style.backgroundColor='#fef2f2'" onmouseout="this.style.backgroundColor='transparent'">Archive</button>
        <div style="border-top: 1px solid #e5e7eb; margin: 4px 0;"></div>
        <button onclick="copyUserDetails('${userId}'); closeAllActionMenus();" style="display: block; width: 100%; text-align: left; padding: 8px 12px; border: none; background: none; cursor: pointer; color: #059669; font-size: 0.875rem;" onmouseover="this.style.backgroundColor='#f0fdf4'" onmouseout="this.style.backgroundColor='transparent'">Copy Details</button>
    `;

    // Append to BODY to escape grid clipping/transforms
    document.body.appendChild(menu);
};

window.handleAction = function (action, userId) {
    console.log(`Action triggered: ${action} for user ${userId}`);
    showToast(`${action} action triggered`, 'success');
    closeAllActionMenus();
};

// Copy user details to clipboard
window.copyUserDetails = function (userId) {
    const allUsers = window.UsersGridManager.getAllRowData();
    const user = allUsers.find(u => u.id === userId);

    if (!user) {
        console.error('User not found:', userId);
        return;
    }

    const details = `${user.fullName}
Email: ${user.email}
Roles: ${user.roles ? user.roles.join(', ') : 'None'}
License: ${user.license}
Status: ${user.status}
Last Active: ${user.formattedLastActive || user.lastActive}
Invited: ${user.wasInvited ? 'Yes' : 'No'}`;

    navigator.clipboard.writeText(details).then(() => {
        showToast('✓ User details copied!');
    }).catch(err => {
        console.error('Failed to copy:', err);
        showToast('Failed to copy details', 'error');
    });
};

// Show toast notification
function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    toast.textContent = message;
    toast.style.cssText = `
        position: fixed;
        bottom: 24px;
        right: 24px;
        background: ${type === 'success' ? '#059669' : '#dc2626'};
        color: white;
        padding: 12px 24px;
        border-radius: 8px;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        z-index: 9999;
        font-size: 14px;
        font-weight: 500;
        animation: slideIn 0.3s ease-out;
    `;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease-out';
        setTimeout(() => document.body.removeChild(toast), 300);
    }, 2000);
}

// Add animation styles
if (!document.getElementById('toast-animations')) {
    const style = document.createElement('style');
    style.id = 'toast-animations';
    style.textContent = `
        @keyframes slideIn {
            from { transform: translateX(400px); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
        @keyframes slideOut {
            from { transform: translateX(0); opacity: 1; }
            to { transform: translateX(400px); opacity: 0; }
        }
    `;
    document.head.appendChild(style);
}

// CSV Export function
window.exportToCsv = function (users, filename) {
    if (!users || users.length === 0) {
        alert('No data to export');
        return;
    }

    const headers = ['ID', 'Full Name', 'Roles', 'License', 'Email', 'Last Active', 'Status', 'Invited By'];
    const csvRows = [];
    csvRows.push(headers.join(','));

    users.forEach(user => {
        const row = [
            escapeCSV(user.id),
            escapeCSV(user.fullName),
            escapeCSV(user.roles ? user.roles.join('; ') : ''),
            escapeCSV(user.license),
            escapeCSV(user.email),
            escapeCSV(user.formattedLastActive || user.lastActive),
            escapeCSV(user.status),
            escapeCSV(user.invitedBy || 'N/A')
        ];
        csvRows.push(row.join(','));
    });

    const csvContent = csvRows.join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');

    if (link.download !== undefined) {
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', filename);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
};

function escapeCSV(value) {
    if (value === null || value === undefined) {
        return '';
    }

    const stringValue = String(value);

    if (stringValue.includes(',') || stringValue.includes('"') || stringValue.includes('\n')) {
        return '"' + stringValue.replace(/"/g, '""') + '"';
    }

    return stringValue;
}
