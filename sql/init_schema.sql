-- =============================================================================
-- Schema: ClientPayments
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS client;

CREATE TABLE IF NOT EXISTS client.ClientPayments (
    Id     BIGSERIAL PRIMARY KEY,
    ClientId BIGINT     NOT NULL,
    Dt     TIMESTAMP(0) NOT NULL,
    Amount MONEY        NOT NULL
);

-- =============================================================================
-- Seed data
-- =============================================================================

TRUNCATE TABLE client.ClientPayments RESTART IDENTITY;

INSERT INTO client.ClientPayments (ClientId, Dt, Amount) VALUES
(1, '2022-01-03 17:24:00', 100),
(1, '2022-01-05 17:24:14', 200),
(1, '2022-01-05 18:23:34', 250),
(1, '2022-01-07 10:12:38', 50),
(2, '2022-01-05 17:24:14', 278),
(2, '2022-01-10 12:39:29', 300);

SELECT * FROM client.ClientPayments ORDER BY ClientId, Dt;
