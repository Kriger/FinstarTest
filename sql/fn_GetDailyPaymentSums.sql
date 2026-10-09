-- Табличная функция: поденные суммы платежей клиента за интервал дат.
-- Если за день платежей нет — возвращает 0.
--
-- Usage:
--   SELECT * FROM fn_GetDailyPaymentSums(1, '2022-01-02'::DATE, '2022-01-07'::DATE);

CREATE OR REPLACE FUNCTION public.fn_GetDailyPaymentSums(
    p_ClientId BIGINT,
    p_StartDate DATE,
    p_EndDate   DATE
)
RETURNS TABLE (
    Dt     DATE,
    Amount NUMERIC(19, 2)
)
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
AS $$
BEGIN
    -- Guard clauses: reject invalid input upfront
    IF p_ClientId IS NULL
       OR p_StartDate IS NULL
       OR p_EndDate   IS NULL
       OR p_StartDate > p_EndDate
    THEN
        RAISE EXCEPTION
            'Invalid arguments: ClientId=%, StartDate=%, EndDate=%',
            p_ClientId, p_StartDate, p_EndDate;
    END IF;

    -- Generate every calendar day in [p_StartDate .. p_EndDate],
    -- then LEFT JOIN against actual payments.
    -- The inclusive end is handled by adding 1 day to the datetime column,
    -- which correctly includes payments on p_EndDate regardless of time part.
    RETURN QUERY
    WITH daily AS (
        SELECT
            cp.Dt::DATE AS pay_date,
            SUM(cp.Amount::NUMERIC(19, 2)) AS pay_sum
        FROM public.ClientPayments cp
        WHERE cp.ClientId = p_ClientId
          AND cp.Dt >= p_StartDate
          AND cp.Dt <  p_EndDate + INTERVAL '1 day'
        GROUP BY cp.Dt::DATE
    )
    SELECT
        gs.dt::DATE AS Dt,
        COALESCE(d.pay_sum, 0::NUMERIC(19, 2)) AS Amount
    FROM GENERATE_SERIES(p_StartDate, p_EndDate, INTERVAL '1 day') AS gs(dt)
    LEFT JOIN daily d
        ON  d.pay_date = gs.dt::DATE
    ORDER BY
        gs.dt::DATE;
END;
$$;

COMMENT ON FUNCTION public.fn_GetDailyPaymentSums IS
    'Returns daily payment sums for a client over a date range. Returns 0 for days without payments.';
