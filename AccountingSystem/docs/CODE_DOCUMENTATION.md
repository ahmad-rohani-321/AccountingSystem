# AccountingSystem Code Documentation

This document describes the current codebase structure, responsibilities, and main data flows. It is intended for developers who need to maintain or refactor the project.

Domain entity classes under `Models/` and the EF Core context under `Data/ApplicationDbContext.cs` are intentionally documented here instead of edited in place.

## Application Shape

AccountingSystem is an ASP.NET Core MVC application targeting .NET 10. It combines server-rendered MVC views, controller-based JSON APIs, EF Core with SQLite, ASP.NET Core Identity, DevExtreme grids/forms, and DevExpress Reporting.

The application entry point is `Program.cs`. It configures:

- SQLite EF Core access through `ApplicationDbContext`.
- ASP.NET Core Identity using the custom `User` entity.
- DevExpress controls and reporting infrastructure.
- MVC controllers and views with JSON property names preserved.
- Cookie login path at `/auth`.
- Scoped inventory repositories.
- Static files, routing, authentication, authorization, DevExpress controls, API controllers, and the default MVC route.

## Project Folders

`Controllers/`
: Server-rendered MVC controllers and API controllers. MVC controllers usually return views. API controllers return JSON, DevExtreme data-source results, or report partials.

`Controllers/APIs/`
: Authenticated API surface used by DevExtreme UI pages and form posts. These controllers orchestrate HTTP requests, validation, EF Core queries, persistence, and response shaping.

`Data/`
: EF Core database context. Do not refactor or document in place unless the data model is intentionally being changed.

`Models/`
: Domain entities for accounting, accounts, inventory, identity, purchase, and settings. These are persistence models and should remain separate from request/response view models.

`Repository/Inventory/`
: Repository abstractions and implementations for inventory lookup entities and items.

`ViewModels/`
: Request, response, and grid row contracts. These are safe to evolve independently from persistence models when the UI/API contract changes.

`Views/`
: Razor pages for the MVC screens. Most screens rely on DevExtreme widgets and call the API controllers.

`Reports/`
: DevExpress XtraReport classes, designer files, and resources. Designer-generated files should not be hand-refactored unless the report designer requires it.

`wwwroot/`
: Static assets, images, DevExtreme scripts/styles, CSS, and localization files.

`Migrations/`
: EF Core migrations and model snapshot. Treat these as database history.

## Shared API Infrastructure

### `ApiControllerBase`

Base class for API controllers that need the authenticated Identity user id.

Responsibility:
- Centralize `ClaimTypes.NameIdentifier` lookup.
- Remove repeated `IHttpContextAccessor` dependencies from API controllers.

Use it when:
- An API controller needs `CurrentUserId`.
- The controller already inherits from `ControllerBase`.

Avoid using it for MVC view controllers that inherit from `Controller` unless they are converted carefully.

### `DevExtremeFormValueMapper`

Utility for parsing DevExtreme form `values` payloads, which arrive as JSON strings in `[FromForm] string values`.

Responsibility:
- Deserialize the DevExtreme JSON payload once.
- Apply property-specific setter delegates.
- Keep controllers from repeating `JsonSerializer.Deserialize<Dictionary<string, JsonElement>>`.

Supported setter helpers:
- `FormValueSetter.String(...)`
- `FormValueSetter.Boolean(...)`

Extend it only when repeated DevExtreme mapping logic appears in multiple controllers.

## MVC Controllers

### `AuthController`

Handles login/logout UI flow.

Responsibilities:
- Render the login page.
- Sign users in through ASP.NET Core Identity.
- Sign users out.

Notes:
- Authenticated users are redirected away from the login page.
- Login posts use Identity's `PasswordSignInAsync`.

### `HomeController`

Serves the default home page and error page.

### `AccountController`

Returns account-related MVC views:
- Normal accounts.
- Person/contributor account pages.

The data operations for these screens live primarily in `Controllers/APIs/AccountsController.cs`.

### `ActionsController`

Returns operational action pages, including the journal entry screen.

The journal entry page gets its data from `JournalEntriesController` and posts manual journal entries through `AccountsController`.

### `PurchaseController`

Returns purchase pages:
- Purchase index/list.
- New purchase page.

The purchase save workflow lives in `Controllers/APIs/PurchasesController.cs`.

### `PurchaseOrderController`

Returns purchase order pages:
- Purchase order index/list.
- New/edit purchase order page.

The purchase order API workflow lives in `Controllers/APIs/PurchaseOrdersController.cs`.

### `PurchaseReturnController`

Reserved for purchase return pages. Current implementation is view-navigation focused.

### `WarehouseController`

Returns inventory/warehouse pages:
- Warehouse index.
- Warehouse settings.
- Remaining stock.
- Least items.

The data APIs for these pages live in inventory API controllers.

### `SettingsController`

Returns the settings page. Settings API work is split across currencies, exchange rates, categories, units, and warehouses.

### `ReportsController`

Creates DevExpress report instances from serialized form data and returns the shared `_Report` partial.

Report endpoints:
- `PersonAccounts`
- `NormalAccount`
- `ContributorAccounts`
- `ItemsList`
- `StockItemsList`
- `LeastItemReport`
- `JournalEntry`

Notes:
- Each action deserializes a JSON data source from form data.
- Report classes are in the `Reports/` folder.
- The nested row classes define report-only shapes for journal entries and least-item reports.

### `CustomWebDocumentController`

DevExpress Web Document Viewer controller. It forwards the DevExpress MVC controller service to the base `WebDocumentViewerController`.

## API Controllers

All API controllers are authenticated with `[Authorize]` unless noted otherwise.

### `AccountsController`

Main API for accounts, contacts, balances, account images, and manual journal entries.

Key endpoints:
- `GET /api/Accounts`
- `GET /api/Accounts/next-code`
- `GET /api/Accounts/balance`
- `POST /api/Accounts`
- `POST /api/Accounts/create`
- `POST /api/Accounts/journal-entry`
- `PUT /api/Accounts`

Responsibilities:
- Load account grids by allowed account type groups.
- Generate account codes.
- Return account balance for an account/currency pair.
- Create or update accounts and account contacts.
- Save manual journal entries with debit/credit balance updates.
- Validate journal entry amount, exchange rate, selected accounts, transaction types, and optional cheque image.

Refactor notes:
- This controller still contains business logic and file handling. A future service extraction should move journal-entry posting and account creation into application services.

### `CategoriesController`

DevExtreme CRUD API for inventory categories.

Key endpoints:
- `GET /api/Categories`
- `GET /api/Categories/Active`
- `POST /api/Categories`
- `PUT /api/Categories`

Responsibilities:
- Read all categories through `ICategoryRepository`.
- Return active categories for dropdowns.
- Create/update categories from DevExtreme form values.

### `UnitsController`

DevExtreme CRUD API for inventory units.

Key endpoints:
- `GET /api/Units`
- `GET /api/Units/Active`
- `POST /api/Units`
- `PUT /api/Units`

Responsibilities:
- Read all units through `IUnitRepository`.
- Return active units for dropdowns.
- Create/update units from DevExtreme form values.

### `WarehousesController`

DevExtreme CRUD API for warehouses.

Key endpoints:
- `GET /api/Warehouses`
- `GET /api/Warehouses/Active`
- `POST /api/Warehouses`
- `PUT /api/Warehouses`

Responsibilities:
- Read warehouses through `IWarehouseRepository`.
- Return active warehouses for dropdowns.
- Create/update warehouses from DevExtreme form values.

### `CurrenciesController`

DevExtreme CRUD API for currencies.

Key endpoints:
- `GET /api/Currencies`
- `POST /api/Currencies`
- `PUT /api/Currencies`

Responsibilities:
- Load currencies with creator info.
- Create/update currencies from DevExtreme form values.
- Ensure only one currency is marked as main currency.

### `CurrencyExchangeController`

API for exchange-rate maintenance.

Key endpoints:
- `GET /api/CurrencyExchange`
- `POST /api/CurrencyExchange`

Responsibilities:
- Return active currencies with latest exchange-rate data.
- Save new exchange-rate rows against the configured main currency.

Notes:
- The GET endpoint returns zero values when no exchange history exists.
- POST requires a main currency to exist.

### `ItemsController`

API for item management, item images, unit conversions, SKU validation, and initial stock setup.

Key endpoints:
- `GET /api/Items`
- `GET /api/Items/{id}/UnitConversions`
- `GET /api/Items/{id}/InitialStock/Allowed`
- `GET /api/Items/{id}/InitialStock/UnitOptions`
- `PUT /api/Items/{id}/InitialStock`
- `PUT /api/Items/{id}/UnitConversions`
- `PUT /api/Items`
- `POST /api/Items/CreateWithConversions`
- `POST /api/Items/UploadImage`
- `PUT /api/Items/{id}/Image`
- `GET /api/Items/Unique`
- `GET /api/Items/NextSku`

Responsibilities:
- Shape item grid rows with related category, unit, creator, and stock-operation state.
- Manage unit conversion rows per item.
- Prevent initial stock entry when an item already has stock operations.
- Create stock balances and initial-stock transactions.
- Create items with conversions in one request.
- Upload and assign item images.
- Check unique fields and generate the next SKU.

Refactor notes:
- This controller is a strong candidate for service extraction because it mixes validation, image persistence, unit-conversion rules, and stock-balance mutations.

### `JournalEntriesController`

Read API for journal-entry UI data.

Key endpoints:
- `GET /api/JournalEntries`
- `GET /api/JournalEntries/page-data`

Responsibilities:
- Return today's journal entries.
- Return active currencies, account options, and account balances needed by the journal-entry page.

### `PurchaseOrdersController`

API for purchase order listing, loading, saving, and deletion.

Key endpoints:
- `GET /api/PurchaseOrders/GetToday`
- `GET /api/PurchaseOrders/GetCreatedToday`
- `GET /api/PurchaseOrders/GetPending`
- `GET /api/PurchaseOrders/{id}`
- `POST /api/PurchaseOrders`
- `DELETE /api/PurchaseOrders/{id}`

Responsibilities:
- Build purchase-order grid rows.
- Filter by date range and account.
- Load order details.
- Validate supplier account and detail rows.
- Resolve item unit conversions.
- Create or update order headers and detail rows.
- Delete order details and order header.

Important implementation detail:
- Detail rows are linked to an order by matching `CreatedByUserId` and `CreationDate`, not by a purchase-order id property.

### `PurchasesController`

API for final purchase creation.

Key endpoints:
- `GET /api/Purchases/next-no`
- `POST /api/Purchases`

Responsibilities:
- Generate next purchase number.
- Validate purchase header, currency, account, warehouse, item, and payment inputs.
- Create purchase and purchase details.
- Increase stock balances and create stock transactions.
- Update supplier and treasure account balances.
- Create journal entries for purchase amount and received amount.
- Wrap the complete save in a database transaction.

Refactor notes:
- This is one of the highest-value service-extraction targets. Purchase save should eventually move to a purchase application service that coordinates stock and journal-entry services.

### `StockBalancesController`

Read API for stock-balance reporting and least-item reporting.

Key endpoints:
- `GET /api/StockBalances`
- `GET /api/StockBalances/LeastItems`

Responsibilities:
- Return remaining stock rows with item, unit, and warehouse information.
- Return stock rows where quantity is below item minimum quantity.

### `StockOperationsController`

API for manual stock operations.

Key endpoints:
- `GET /api/StockOperations/TransactionTypes`
- `GET /api/StockOperations/Items`
- `GET /api/StockOperations/StockBalanceItems`
- `POST /api/StockOperations/RemainingStock`

Responsibilities:
- Provide operation type options.
- Provide active items and stock-balance item options.
- Save stock adjustments, stock-out operations, transfers, and related stock transactions.
- Validate unit conversions and available quantity.
- Wrap stock mutations in database transactions.

Refactor notes:
- This controller is another strong candidate for service extraction. Stock mutation rules should be centralized so purchases, initial stock, and manual operations share one stock service.

## Repositories

Repositories currently exist for inventory lookup and item data.

### `ICategoryRepository` / `CategoryRepository`

Responsibilities:
- Load categories with creator info.
- Load category by id.
- Add/update category.
- Check existence.

### `IUnitRepository` / `UnitRepository`

Responsibilities:
- Load units with creator info.
- Load unit by id.
- Add/update unit.
- Check existence.

### `IWarehouseRepository` / `WarehouseRepository`

Responsibilities:
- Load warehouses with creator info.
- Load warehouse by id.
- Add/update warehouse.
- Add/update a mixed range through `UpdateRangeAsync`.
- Check existence.

### `IItemsRepository` / `ItemsRepository`

Responsibilities:
- Add item.
- Load item by id with category, unit, and creator info.
- Update item.
- Load all items with category, unit, and creator info.

Refactor notes:
- Some controllers still query `ApplicationDbContext` directly for inventory data. If repository usage continues, keep repository boundaries consistent. If services are introduced, prefer services for business workflows and keep repositories focused on persistence.

## View Models

### Account view models

`AccountTypeOption`
: Dropdown/list option for account type selection.

`AccountListItem`
: Grid row for account lists. Includes contact fields when accounts are person/contributor-style accounts.

`AccountBalancePayload`
: Balance input payload for account creation/update.

`AccountCreateRequest`
: Main request contract for account creation/update, including account metadata, contact fields, balances, and optional image data.

### Item view models

`CreateItemRequest`
: Request for creating an item with unit conversions.

`UnitConversionRow`
: Request/response row for an item's unit conversion setup.

`UpdateItemImageRequest`
: Request for assigning an uploaded image to an item.

`InitialStockRowVm`
: Request row for initial stock balances.

### Purchase order view models

`PurchaseOrderGridRow`
: Grid row for order list pages.

`PurchaseOrderSaveRequest`
: Request for creating/updating a purchase order.

`PurchaseOrderSaveDetailRequest`
: Detail row inside a purchase order save request.

`PurchaseOrderResponse`
: Header and details returned when loading an order for edit/view.

`PurchaseOrderDetailResponse`
: Detail row returned in `PurchaseOrderResponse`.

### Purchase view models

`PurchaseSaveRequest`
: Request for creating a completed purchase.

`PurchaseSaveDetailRequest`
: Detail row for purchase save, including item, unit conversion, quantity, price, warehouse, batch, and remarks.

### Currency exchange view model

`CurrencyExchangeVM`
: Row used by the exchange-rate UI and API. Holds sub-currency, main/sub amounts, and exchange rate.

### Journal entry view model

`JournalEntryCreateRequest`
: Multipart form request for manual journal entries, including debit/credit accounts and currencies, amount, exchange rate, remarks, and optional cheque photo.

### Stock operation view model

`RemainingStockOperationVm`
: Request for manual remaining-stock operations, including operation type, item/warehouse/stock-balance references, quantity, unit conversion, batch, and notes.

## Views

`Views/Shared/_Layout.cshtml`
: Main application shell and shared static asset references.

`Views/Shared/Error.cshtml`
: Error page.

`Views/_ViewImports.cshtml`
: Razor namespace and tag-helper imports.

`Views/_ViewStart.cshtml`
: Shared layout configuration.

`Views/Auth/Index.cshtml`
: Login page.

`Views/Home/Index.cshtml`
: Home page.

`Views/Account/*.cshtml`
: Account screens backed by `AccountController` and `AccountsController`.

`Views/Actions/JournalEntry.cshtml`
: Manual journal-entry screen backed by `JournalEntriesController` and the account journal-entry API.

`Views/Purchase/*.cshtml`
: Purchase list and new purchase screens backed by `PurchasesController`.

`Views/PurchaseOrder/*.cshtml`
: Purchase order list and edit/create screens backed by `PurchaseOrdersController`.

`Views/Warehouse/*.cshtml`
: Warehouse, inventory settings, remaining stock, and least-item pages backed by inventory APIs.

`Views/Settings/Index.cshtml`
: Settings screen backed by lookup and currency APIs.

`Views/Reports/_Report.cshtml`
: Shared report viewer partial used by `ReportsController`.

## Reports

Report classes under `Reports/` are DevExpress XtraReport definitions.

Account reports:
- `ContributorAccounts`
- `NormalAccounts`
- `PersonAccounts`

Inventory reports:
- `ItemsList`
- `StockItemsList`
- `LeastItemsReport`

Journal reports:
- `JournalEntryReport`

Files ending in `.Designer.cs` and `.resx` are generated/designer-managed support files.

## Important Business Flows

### Manual journal entry

1. The journal-entry page loads currencies, account options, and balances from `JournalEntriesController`.
2. The form posts to `AccountsController.CreateJournalEntry`.
3. The controller validates accounts, currencies, amount, exchange rate, remarks, transaction types, and optional cheque image.
4. Account balances are created when missing.
5. Debit and credit balances are adjusted.
6. Journal entry rows are inserted.

### Purchase order save

1. The UI posts `PurchaseOrderSaveRequest`.
2. `PurchaseOrdersController` validates supplier account and detail rows.
3. Each item/unit conversion is resolved.
4. New orders are inserted, or existing orders are updated.
5. Existing detail rows are removed on update.
6. New detail rows are inserted.

### Purchase save

1. The UI asks `PurchasesController` for the next purchase number.
2. The UI posts `PurchaseSaveRequest`.
3. The controller validates purchase number, supplier account, currency, optional treasure account, item ids, warehouse ids, and totals.
4. The purchase, purchase details, stock balances, stock transactions, account balances, and journal entries are saved inside one transaction.

### Initial stock save

1. The UI checks if initial stock is allowed for an item.
2. Unit options are loaded from item unit conversions.
3. The UI posts initial stock rows.
4. `ItemsController` validates item, warehouse, quantity, and unit conversion.
5. Stock balances and stock transactions are created.

### Manual stock operation

1. The UI loads operation types, active items, and existing stock balance items.
2. The UI posts `RemainingStockOperationVm`.
3. `StockOperationsController` validates the selected operation and unit conversion.
4. The controller creates or updates stock balances and inserts stock transactions in a transaction.

## Refactoring Rules For This Codebase

- Do not edit `Models/` entity classes unless the database model is intentionally changing.
- Do not edit `Data/ApplicationDbContext.cs` unless the EF model mapping is intentionally changing.
- Keep generated DevExpress designer files out of manual refactors.
- Keep controllers focused on HTTP orchestration.
- Move business workflows into services before they grow further.
- Keep view models in `ViewModels/`; do not expose EF entities as new API contracts.
- Use `ApiControllerBase.CurrentUserId` in API controllers instead of injecting `IHttpContextAccessor` for user lookup.
- Use `DevExtremeFormValueMapper` when multiple controllers need to parse DevExtreme `[FromForm] values`.
- Prefer a single database transaction around multi-table business operations.
- Preserve existing Pashto user-facing messages when refactoring behavior.

## Suggested Next Documentation Improvements

- Add XML comments to public service interfaces after service extraction.
- Add endpoint examples for the highest-use APIs.
- Add screenshots or workflow notes for each Razor page.
- Add report data-source examples for each DevExpress report.
- Add a database glossary generated from the entity model when model edits are allowed.
