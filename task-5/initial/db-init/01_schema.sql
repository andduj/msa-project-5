-- Create application tables
CREATE TABLE IF NOT EXISTS products  (
    productId BIGINT NOT NULL PRIMARY KEY,
    productSku BIGINT NOT NULL,
    productName VARCHAR(20),
    productAmount BIGINT,
    productData VARCHAR(120)
);

CREATE TABLE IF NOT EXISTS loyality_data  (
    productSku BIGINT NOT NULL PRIMARY KEY,
    loyalityData VARCHAR(120)
);


