# Blazor User List

## How to Run
1. **Navigate** to the project directory:
   ```bash
   cd BlazorUserList
   ```
2. Run the application:
   ```bash
   dotnet run
   ```
3. Open your browser to the URL shown (typically `http://localhost:5251`).

## Technical Implementation details
- **JS Interop Strategy**: I chose a **Vanilla JavaScript Interop wrapper** (`users-interop.js`) instead of a third-party Blazor library.
  - I think it's just going to save us with any dependency issues and I think Vanilla.js will give us ultimate control over AG grid's APIs for any custom functionality 

## Design Decisions & Trade-offs
- **Three-File Component Structure**: Each major component (like `Users.razor`) follows a clean, modular structure, we generally do this with Angular and it keeps everything really clean and modular:
  - `.razor`: Handles the HTML markup and UI structure.
  - `.razor.cs`: Contains the C# logic using `partial` classes to keep the markup file lean and maintainable.
  - `.razor.css`: Uses **Scoped CSS** to ensure styles are isolated and do not leak to other parts of the application.
- **CSS Strategy**: 
  - **Scoped CSS**: Employed for component-specific styling (Ag-Grid overrides, filter pills) to maintain cleanliness and modularity.
  - **Global CSS**: Reserved for common, app-wide styles (found in `app.css`), ensuring consistency across the entire application while minimizing redundancy.

## Assumptions & Omissions
- **Multi-Selection**: In the attached image in the assignment, I did see that each row was selectable. But because I do not see any relevant bulk operation in the assignment like bulk delete etc., I do not implement multi-row selection 

## Future Enhancements
With more time, I would refactor the following:
1.  **Reusable Data Grid Component**: Abstract the specific Ag-Grid implementation into a generic `<AgDataGrid Columns="..." Data="..." />` component to be used across the entire app. This way I would have injected it into the user component and that would have made stuff even more clean. 
2.  **Componentized Filter Pills**: Extract the filter pill logic (dropdown handling, active states) into a reusable `<FilterPill>` component to declutter the main page logic.
