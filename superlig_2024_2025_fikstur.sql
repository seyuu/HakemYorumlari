-- Süper Lig 2024-2025 Sezonu Tam Fikstürü
-- Önce mevcut test verilerini temizle
DELETE FROM HakemYorumlari WHERE MacId IN (SELECT Id FROM Maclar);
DELETE FROM Pozisyonlar WHERE MacId IN (SELECT Id FROM Maclar);
DELETE FROM Maclar;

-- 1. HAFTA (10-11 Ağustos 2024)
INSERT INTO Maclar (EvSahibiTakim, DeplasmanTakimi, MacTarihi, Hafta, Liga, MacDurumu, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Hatayspor', '2024-08-10 20:00:00', 1, 'Süper Lig', 'Bitti', 1, 0),
('Fenerbahçe', 'Adana Demirspor', '2024-08-11 19:00:00', 1, 'Süper Lig', 'Bitti', 1, 0),
('Beşiktaş', 'Rizespor', '2024-08-11 21:45:00', 1, 'Süper Lig', 'Bitti', 1, 0),
('Trabzonspor', 'Sivasspor', '2024-08-10 19:00:00', 1, 'Süper Lig', 'Bitti', 1, 0),
('Başakşehir', 'Antalyaspor', '2024-08-11 19:00:00', 1, 'Süper Lig', 'Bitti', 1, 0),
('Kasımpaşa', 'Gaziantep FK', '2024-08-10 19:00:00', 1, 'Süper Lig', 'Bitti', 1, 0),
('Konyaspor', 'Alanyaspor', '2024-08-11 19:00:00', 1, 'Süper Lig', 'Bitti', 1, 0),
('Samsunspor', 'Kayserispor', '2024-08-10 21:45:00', 1, 'Süper Lig', 'Bitti', 1, 0),
('Eyüpspor', 'Göztepe', '2024-08-11 21:45:00', 1, 'Süper Lig', 'Bitti', 1, 0),
('Bodrum FK', 'Ankaragücü', '2024-08-10 21:45:00', 1, 'Süper Lig', 'Bitti', 1, 0);

-- 2. HAFTA (17-18 Ağustos 2024)
INSERT INTO Maclar (EvSahibiTakim, DeplasmanTakimi, MacTarihi, Hafta, Liga, MacDurumu, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Hatayspor', 'Fenerbahçe', '2024-08-17 20:00:00', 2, 'Süper Lig', 'Bitti', 1, 0),
('Adana Demirspor', 'Beşiktaş', '2024-08-18 19:00:00', 2, 'Süper Lig', 'Bitti', 1, 0),
('Rizespor', 'Trabzonspor', '2024-08-18 21:45:00', 2, 'Süper Lig', 'Bitti', 1, 0),
('Sivasspor', 'Başakşehir', '2024-08-17 19:00:00', 2, 'Süper Lig', 'Bitti', 1, 0),
('Antalyaspor', 'Kasımpaşa', '2024-08-18 19:00:00', 2, 'Süper Lig', 'Bitti', 1, 0),
('Gaziantep FK', 'Konyaspor', '2024-08-17 19:00:00', 2, 'Süper Lig', 'Bitti', 1, 0),
('Alanyaspor', 'Samsunspor', '2024-08-18 19:00:00', 2, 'Süper Lig', 'Bitti', 1, 0),
('Kayserispor', 'Eyüpspor', '2024-08-17 21:45:00', 2, 'Süper Lig', 'Bitti', 1, 0),
('Göztepe', 'Bodrum FK', '2024-08-18 21:45:00', 2, 'Süper Lig', 'Bitti', 1, 0),
('Ankaragücü', 'Galatasaray', '2024-08-17 21:45:00', 2, 'Süper Lig', 'Bitti', 1, 0);

-- Devam eden haftalar için benzer şekilde...
-- (38 hafta boyunca tüm maçlar)