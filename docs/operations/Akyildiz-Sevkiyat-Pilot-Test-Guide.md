# Akyildiz Sevkiyat: Pilot Test & Operations Guide

This guide consolidates the manual test checklist, the 1-day pilot simulation scenario, and operational metrics for the Akyildiz Sevkiyat ISS warehouse system.

---

## 1. Pilot Simulation Scenario (1-Day)

### 1.1 Objective & Team
**Goal**: Verify end-to-end operation using real staff. Focus on transitions and data accuracy.
- **Accounting**: 1 Person (Shipment creation & planning).
- **Warehouse**: 2 Persons (Picking & Goods Receipt).
- **Admin/Observer**: 1 Person (Monitoring logs and integrity).

### 1.2 Timeline & Workflow
| Phase | Activity | Key Steps |
| :--- | :--- | :--- |
| **I. Planning** | Shipment Creation | Create 2 shipments for `ISS-ANK-TEST` project. |
| **II. Assignment**| Zone Assignment | Assign to `ANKARA-1`. Click "Depoya Ata". |
| **III. Execution**| Micro Picking | **(Concurrency Test)**: Operator 1 & 2 pick different products simultaneously. |
| **IV. Shortage** | Macro Allocation | Enter shortage for 1 item; distribute manually between shipments. |
| **V. Dispatch** | Logistics | Assign driver `Ahmet` (34-ABC-123). Mark `ReadyForDispatch`. |
| **VI. Inbound** | Goods Receipt | Post a partial receipt with 1 rejected item (reason: Hasarlı). |

---

## 2. Manual Test Checklist

### 2.1 Daily Workflow (Core)
- [ ] **Shipment List**: Search accuracy and filtering. (Priority: High)
- [ ] **Warehouse Transfer**: Transition to `AssignedToWarehouse`. (Priority: Critical)
- [ ] **Zone Preparation**: "🚀 TOPLAMAYI BAŞLAT" locks the zone (`Frozen`). (Priority: Critical)
- [ ] **Mobile Picking**: Rapid stepper use (+/-) handles without UI lag or duplicate requests. (Priority: High)
- [ ] **Macro/Micro Completion**: Transitions to `ReadyForDriverAssignment`. (Priority: High)
- [ ] **Goods Receipt**: PO auto-populate and line-by-line saving. (Priority: Critical)

### 2.2 Advanced Safety & Integrity
- [ ] **Data Integrity**: `Sum(Allocation)` MUST equal `PickedQty`. Block if mismatch. (Priority: Critical)
- [ ] **Persistence**: Saved picking data survives page refresh/re-login. (Priority: High)
- [ ] **Dirty State**: System blocks transitions if unsaved (Dirty) lines exist. (Priority: High)
- [ ] **Stock Impact**: "Posted" GR increases warehouse stock levels in DB. (Priority: Critical)
- [ ] **Concurrency**: Multi-user editing on same zone results in no data corruption. (Priority: Critical)

---

## 3. Pilot Metrics (Measurement Targets)
| Metric | Target | Result / Observer Note |
| :--- | :--- | :--- |
| **Shipment Creation** | < 30 sec | |
| **Picking Save Latency** | < 2 sec | |
| **Macro Modal Load** | < 3 sec | |
| **GR Posting Time** | < 3 sec | |
| **User Error Rate** | < 5% | |

---

## 4. Incident & Fail-State Procedures
In case of a **500 Error**, **Data Mismatch**, or **System Freeze**:
1. **Evidence**: Take a screenshot of the error modal/screen.
2. **Context**: Record the Shipment ID and User Role.
3. **Logs**: Use browser tools (F12) to export console logs.
4. **Recovery**: Refresh page; if data is corrupted, notify Admin to reset the specific Shipment/Zone state.

---

## 5. System Stability Monitoring
The **Admin/Observer** must monitor the following during the simulation:
- **Browser Console**: Watch for Red (Error) or Orange (Warning) logs.
- **Network Tab**: Ensure no failed API requests (4xx/5xx).
- **Backend Logs**: (If available) Monitor server-side exception logs in real-time.

---

## 6. Post-Pilot Cleanup
After the simulation is finalized:
- [ ] **Archive/Delete**: Delete test shipments created during the pilot.
- [ ] **Reset States**: Revert any test `ZonePreparation` records to `Draft`.
- [ ] **Stock Correction**: If the test database is linked to real inventory, reset stock adjustments performed via Goods Receipt.

---

## 7. Must Pass Before Pilot (Final Check)
- [ ] Shipment cycle (`Created` -> `Ready`) completes without crash.
- [ ] Audit logs (`ShipmentHistory`) are generated for every state change.
- [ ] Mobile buttons are responsive and disable during processing.
