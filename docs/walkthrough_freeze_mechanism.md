# Warehouse Freeze & Batch Mechanism Implementation

I have successfully implemented the "Freeze" mechanism to manage warehouse picking sessions and handle late shipments via a Batch system.

## Changes Implemented

### 1. Domain & Database
*   **ZonePreparation:** Added `BatchNo`, `IsFrozen`, `StartedAt`, `StartedByUserId`.
*   **Shipment:** Added `ZonePreparationId` to link shipments to specific batches.
*   **Database:** Created and applied migration `AddZonePreparationFreezeColumns`.
*   **Constraint:** Added Unique Constraint on `(ZoneId, DeliveryDate, BatchNo)`.

### 2. Logic & workflow
*   **Start Picking (Freeze):** Implemented `StartZonePreparationCommand`.
    *   **Guard:** Can only start if `Status == Draft` and `IsFrozen == False`.
    *   Locks the batch (`IsFrozen = true`).
    *   Sets status to `MicroPicking`.
    *   Links current user.
*   **Dashboard / Batch Creation:** Updated `GetWarehouseDashboardQuery`.
    *   Automatically assigns new shipments to the "Open" batch.
    *   If the current batch is frozen, creates a **New Batch** (Batch 2, Batch 3...) for new shipments.
    *   Returns multiple batches for the same Zone.
    *   **Batch Label:** "Ana Toplama" (Batch 1) vs "Geç Gelenler (Batch N)".
*   **Batch Isolation:** Updated all picking queries and commands to strictly filter by `ZonePreparationId`.
    *   `GetProjectMicroPickListQuery`
    *   `GetZoneMacroPickListQuery`
    *   `MarkProjectMicroReadyCommand`
    *   `MarkZoneMacroReadyCommand`
    *   `SetZoneDriverInfoCommand` (Added Guards: Status >= ReadyForDriverInfo + IsFrozen).

### 3. Status Transition Logic
*   **MicroReady:** When all projects in a Zone Batch are marked Micro Ready, the Zone Status automatically advances to `MacroPicking`.

## Logic Flow Verification

| Scenario | Behavior |
| :--- | :--- |
| **Initial State** | Dashboard creates Batch 1 (Draft, IsFrozen=False). Label: "Ana Toplama". |
| **Start Picking** | User clicks "Start". Batch 1 becomes `MicroPicking` & `IsFrozen=True`. |
| **New Shipment** | New order arrives. Dashboard creates Batch 2 (Draft). Label: "Geç Gelenler (Batch 2)". |
| **Batch Isolation** | Micro Picking List only shows items for the selected Batch. |
| **Completion** | All Micro items done -> Zone moves to `MacroPicking`. |
| **Driver Assignment** | Cannot assign driver unless Picking is done (`ReadyForDriverInfo`) AND Batch is Frozen. |

## Next Steps
*   Frontend: Update the Warehouse Dashboard to display multiple batch cards if present, and add the "Start Picking" (Freeze) button.
