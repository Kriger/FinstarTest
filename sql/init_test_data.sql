-- Создание таблицы платежей
CREATE TABLE IF NOT EXISTS ClientPayments (
    Id BIGSERIAL PRIMARY KEY,
    ClientId BIGINT NOT NULL,
    Dt TIMESTAMP(0) NOT NULL,
    Amount money NOT NULL
);

-- Очистка данных (если таблица уже существует)
TRUNCATE TABLE ClientPayments RESTART IDENTITY;

-- Заполнение тестовыми данными
INSERT INTO ClientPayments (ClientId, Dt, Amount) VALUES
(1, '2022-01-03 17:24:00', 100),
(1, '2022-01-05 17:24:14', 200),
(1, '2022-01-05 18:23:34', 250),
(1, '2022-01-07 10:12:38', 50),
(2, '2022-01-05 17:24:14', 278),
(2, '2022-01-10 12:39:29', 300);

-- Проверка данных
SELECT * FROM ClientPayments ORDER BY ClientId, Dt;
