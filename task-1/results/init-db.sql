-- Инициализация PostgreSQL базы данных для Batch Processing System

-- Создание расширений
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Создание таблиц
CREATE TABLE IF NOT EXISTS customers (
    customer_id SERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(200) UNIQUE NOT NULL,
    registration_date DATE NOT NULL,
    loyalty_level VARCHAR(20) DEFAULT 'bronze',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS orders (
    order_id SERIAL PRIMARY KEY,
    customer_id INTEGER NOT NULL REFERENCES customers(customer_id),
    order_date DATE NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS processing_results (
    id SERIAL PRIMARY KEY,
    batch_date DATE NOT NULL,
    total_orders INTEGER NOT NULL,
    total_amount DECIMAL(12,2) NOT NULL,
    high_value_orders INTEGER NOT NULL,
    processing_status VARCHAR(50) NOT NULL,
    processing_type VARCHAR(50) NOT NULL,
    processing_time_seconds INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Создание индексов для производительности
CREATE INDEX IF NOT EXISTS idx_customers_email ON customers(email);
CREATE INDEX IF NOT EXISTS idx_orders_customer_id ON orders(customer_id);
CREATE INDEX IF NOT EXISTS idx_orders_date ON orders(order_date);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
CREATE INDEX IF NOT EXISTS idx_processing_results_date ON processing_results(batch_date);

-- Вставка тестовых данных
INSERT INTO customers (first_name, last_name, email, registration_date, loyalty_level) VALUES
('Иван', 'Петров', 'ivan.petrov@example.com', '2023-01-15', 'gold'),
('Мария', 'Сидорова', 'maria.sidorova@example.com', '2023-02-20', 'silver'),
('Алексей', 'Козлов', 'alexey.kozlov@example.com', '2023-03-10', 'bronze'),
('Елена', 'Васильева', 'elena.vasileva@example.com', '2023-04-05', 'platinum'),
('Дмитрий', 'Смирнов', 'dmitry.smirnov@example.com', '2023-05-12', 'bronze'),
('Анна', 'Федорова', 'anna.fedorova@example.com', '2023-06-18', 'gold'),
('Сергей', 'Морозов', 'sergey.morozov@example.com', '2023-07-22', 'silver'),
('Ольга', 'Новикова', 'olga.novikova@example.com', '2023-08-30', 'bronze'),
('Павел', 'Волков', 'pavel.volkov@example.com', '2023-09-14', 'platinum'),
('Татьяна', 'Соколова', 'tatyana.sokolova@example.com', '2023-10-08', 'gold')
ON CONFLICT (email) DO NOTHING;

INSERT INTO orders (customer_id, order_date, total_amount, status) VALUES
(1, '2024-01-10', 15000.00, 'completed'),
(2, '2024-01-11', 8500.50, 'completed'),
(3, '2024-01-12', 3200.75, 'completed'),
(4, '2024-01-13', 25000.00, 'completed'),
(5, '2024-01-14', 1500.25, 'completed'),
(6, '2024-01-15', 12000.00, 'completed'),
(7, '2024-01-16', 6750.30, 'completed'),
(8, '2024-01-17', 2800.90, 'completed'),
(9, '2024-01-18', 35000.00, 'completed'),
(10, '2024-01-19', 9200.45, 'completed'),
(1, '2024-01-20', 18500.75, 'pending'),
(3, '2024-01-21', 4100.20, 'pending'),
(5, '2024-01-22', 2200.00, 'processing'),
(7, '2024-01-23', 7800.65, 'processing'),
(9, '2024-01-24', 42000.00, 'pending')
ON CONFLICT DO NOTHING;

-- Создание пользователя для приложения (опционально)
-- DO $$ 
-- BEGIN
--     IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'batchprocessing_user') THEN
--         CREATE ROLE batchprocessing_user WITH LOGIN PASSWORD 'secure_password';
--         GRANT CONNECT ON DATABASE "BatchProcessingDb" TO batchprocessing_user;
--         GRANT USAGE ON SCHEMA public TO batchprocessing_user;
--         GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO batchprocessing_user;
--         GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO batchprocessing_user;
--     END IF;
-- END
-- $$;
