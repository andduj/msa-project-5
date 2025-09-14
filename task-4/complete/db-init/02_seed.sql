-- Seed loyality_data with deterministic inserts (robust to blank lines)
INSERT INTO loyality_data (productSku, loyalityData) VALUES
  (20001, 'Loyality_on')
ON CONFLICT (productSku) DO UPDATE SET loyalityData = EXCLUDED.loyalityData;

INSERT INTO loyality_data (productSku, loyalityData) VALUES
  (30001, 'Loyality_on')
ON CONFLICT (productSku) DO UPDATE SET loyalityData = EXCLUDED.loyalityData;

INSERT INTO loyality_data (productSku, loyalityData) VALUES
  (50001, 'Loyality_on')
ON CONFLICT (productSku) DO UPDATE SET loyalityData = EXCLUDED.loyalityData;

INSERT INTO loyality_data (productSku, loyalityData) VALUES
  (60001, 'Loyality_on')
ON CONFLICT (productSku) DO UPDATE SET loyalityData = EXCLUDED.loyalityData;


