-- =============================================================================
-- Indexes for ClientPayments
-- =============================================================================

-- Covering index: ClientId + Dt enables index-only scan.
-- Amount is stored in INCLUDE so the planner can satisfy SUM(Amount)
-- without visiting the heap.
CREATE INDEX IF NOT EXISTS idx_cp_clientid_dt_amount
    ON client.ClientPayments (ClientId, Dt) INCLUDE (Amount);
