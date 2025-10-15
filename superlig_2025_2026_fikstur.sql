-- 2025-2026 Trendyol Süper Lig Tam Fikstürü (18 Takım - 34 Hafta - 306 Maç)
-- Sezon: 8 Ağustos 2025 - 17 Mayıs 2026
-- TFF Resmi Verilerine Göre Güncellenmiş

-- Mevcut test verilerini temizle
DELETE FROM KullaniciAnketleri;
DELETE FROM HakemYorumlari;
DELETE FROM Pozisyonlar;
DELETE FROM Maclar;

-- 1. HAFTA (8-11 Ağustos 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Eyüpspor', 'Konyaspor', '2025-08-10 19:00:00', 1, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Kasımpaşa', '2025-08-09 21:30:00', 1, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Kocaelispor', '2025-08-11 21:30:00', 1, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Gençlerbirliği', '2025-08-09 19:00:00', 1, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'İstanbul Başakşehir', '2025-08-10 21:30:00', 1, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Galatasaray', '2025-08-08 21:30:00', 1, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Alanyaspor', '2025-08-09 21:30:00', 1, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Göztepe', '2025-08-10 19:00:00', 1, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Beşiktaş', '2025-08-10 21:30:00', 1, 'Süper Lig', '-', 0, 1, 0);

-- 2. HAFTA (15-18 Ağustos 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Fatih Karagümrük', '2025-08-15 21:30:00', 2, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Samsunspor', '2025-08-16 19:00:00', 2, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Çaykur Rizespor', '2025-08-16 21:30:00', 2, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Fenerbahçe', '2025-08-16 21:30:00', 2, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Gaziantep FK', '2025-08-17 19:00:00', 2, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Antalyaspor', '2025-08-17 19:00:00', 2, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Eyüpspor', '2025-08-17 21:30:00', 2, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Kayserispor', '2025-08-17 21:30:00', 2, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Trabzonspor', '2025-08-18 21:30:00', 2, 'Süper Lig', '-', 0, 1, 0);

-- 3. HAFTA (22-25 Ağustos 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Fenerbahçe', 'Kocaelispor', '2025-08-24 19:00:00', 3, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Beşiktaş', '2025-08-24 19:00:00', 3, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Kasımpaşa', '2025-08-24 19:00:00', 3, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'İstanbul Başakşehir', '2025-08-24 16:00:00', 3, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Antalyaspor', '2025-08-24 18:30:00', 3, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Galatasaray', '2025-08-25 19:00:00', 3, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Alanyaspor', '2025-08-23 19:00:00', 3, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Gençlerbirliği', '2025-08-23 21:30:00', 3, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Gençlerbirliği', '2025-08-23 21:30:00', 3, 'Süper Lig', '-', 0, 1, 0);

-- 4. HAFTA (29 Ağustos - 1 Eylül 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Çaykur Rizespor', '2025-08-31 20:00:00', 4, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Fatih Karagümrük', '2025-09-01 19:00:00', 4, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Samsunspor', '2025-08-30 19:00:00', 4, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Kayserispor', '2025-08-31 19:00:00', 4, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Göztepe', '2025-08-30 21:30:00', 4, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Trabzonspor', '2025-08-31 21:30:00', 4, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Fenerbahçe', '2025-09-01 21:30:00', 4, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Gaziantep FK', '2025-08-30 19:00:00', 4, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Konyaspor', '2025-08-31 19:00:00', 4, 'Süper Lig', '-', 0, 1, 0);

-- 5. HAFTA (13-15 Eylül 2025) - TFF Resmi Fikstürü (İlk Derbi: Fenerbahçe - Trabzonspor)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Fenerbahçe', 'Trabzonspor', '2025-09-14 20:00:00', 5, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Beşiktaş', '2025-09-13 19:00:00', 5, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Eyüpspor', '2025-09-14 19:00:00', 5, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Alanyaspor', '2025-09-15 19:00:00', 5, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'İstanbul Başakşehir', '2025-09-14 21:30:00', 5, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Gençlerbirliği', '2025-09-15 21:30:00', 5, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Kocaelispor', '2025-09-13 21:30:00', 5, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Galatasaray', '2025-09-15 19:00:00', 5, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Kasımpaşa', '2025-09-14 19:00:00', 5, 'Süper Lig', '-', 0, 1, 0);

-- 6. HAFTA (20-22 Eylül 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Fatih Karagümrük', '2025-09-21 20:00:00', 6, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Konyaspor', '2025-09-22 19:00:00', 6, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Antalyaspor', '2025-09-21 21:30:00', 6, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Çaykur Rizespor', '2025-09-20 19:00:00', 6, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Samsunspor', '2025-09-22 21:30:00', 6, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Kayserispor', '2025-09-21 19:00:00', 6, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Göztepe', '2025-09-20 21:30:00', 6, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Fenerbahçe', '2025-09-22 19:00:00', 6, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Gaziantep FK', '2025-09-21 19:00:00', 6, 'Süper Lig', '-', 0, 1, 0);

-- 7. HAFTA (27-29 Eylül 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Fenerbahçe', 'Eyüpspor', '2025-09-28 20:00:00', 7, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Galatasaray', '2025-09-27 19:00:00', 7, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'İstanbul Başakşehir', '2025-09-28 21:30:00', 7, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Trabzonspor', '2025-09-29 19:00:00', 7, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Beşiktaş', '2025-09-28 19:00:00', 7, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Alanyaspor', '2025-09-29 21:30:00', 7, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Gençlerbirliği', '2025-09-27 21:30:00', 7, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Kasımpaşa', '2025-09-29 19:00:00', 7, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Kocaelispor', '2025-09-28 19:00:00', 7, 'Süper Lig', '-', 0, 1, 0);

-- 8. HAFTA (4-6 Ekim 2025) - TFF Resmi Fikstürü (Galatasaray - Beşiktaş Derbisi)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Beşiktaş', '2025-10-05 20:00:00', 8, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Samsunspor', '2025-10-04 19:00:00', 8, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Antalyaspor', '2025-10-05 21:30:00', 8, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Göztepe', '2025-10-04 21:30:00', 8, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Çaykur Rizespor', '2025-10-06 19:00:00', 8, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Kayserispor', '2025-10-06 21:30:00', 8, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Konyaspor', '2025-10-05 19:00:00', 8, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Fatih Karagümrük', '2025-10-04 19:00:00', 8, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Fenerbahçe', '2025-10-06 19:00:00', 8, 'Süper Lig', '-', 0, 1, 0);

-- ... existing code ...

-- 9. HAFTA (18-20 Ekim 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('İstanbul Başakşehir', 'Galatasaray', '2025-10-18 21:30:00', 9, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Gençlerbirliği', '2025-10-19 19:00:00', 9, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Kasımpaşa', '2025-10-19 21:30:00', 9, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Kocaelispor', '2025-10-20 19:00:00', 9, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Gaziantep FK', '2025-10-20 21:30:00', 9, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Antalyaspor', '2025-10-19 19:00:00', 9, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Çaykur Rizespor', '2025-10-20 19:00:00', 9, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Eyüpspor', '2025-10-19 19:00:00', 9, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Fenerbahçe', '2025-10-20 21:30:00', 9, 'Süper Lig', '-', 0, 1, 0);

-- 10. HAFTA (25-27 Ekim 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Göztepe', '2025-10-25 20:00:00', 10, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Çaykur Rizespor', '2025-10-26 19:00:00', 10, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Beşiktaş', '2025-10-27 19:00:00', 10, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Alanyaspor', '2025-10-26 21:30:00', 10, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Fatih Karagümrük', '2025-10-27 21:30:00', 10, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'İstanbul Başakşehir', '2025-10-26 19:00:00', 10, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Samsunspor', '2025-10-27 19:00:00', 10, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Kayserispor', '2025-10-26 19:00:00', 10, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Konyaspor', '2025-10-27 21:30:00', 10, 'Süper Lig', '-', 0, 1, 0);

-- 11. HAFTA (1-3 Kasım 2025) - TFF Resmi Fikstürü (Galatasaray - Trabzonspor Derbisi)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Trabzonspor', '2025-11-01 20:00:00', 11, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Kasımpaşa', '2025-11-02 19:00:00', 11, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Gençlerbirliği', '2025-11-02 21:30:00', 11, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Alanyaspor', '2025-11-03 19:00:00', 11, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Antalyaspor', '2025-11-03 21:30:00', 11, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Eyüpspor', '2025-11-02 19:00:00', 11, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Gaziantep FK', '2025-11-03 19:00:00', 11, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Konyaspor', '2025-11-02 19:00:00', 11, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Fenerbahçe', '2025-11-03 21:30:00', 11, 'Süper Lig', '-', 0, 1, 0);

-- 12. HAFTA (8-10 Kasım 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Kocaelispor', 'Galatasaray', '2025-11-08 20:00:00', 12, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Kayserispor', '2025-11-09 19:00:00', 12, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'İstanbul Başakşehir', '2025-11-09 21:30:00', 12, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Trabzonspor', '2025-11-10 19:00:00', 12, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Samsunspor', '2025-11-10 21:30:00', 12, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Göztepe', '2025-11-09 19:00:00', 12, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Fatih Karagümrük', '2025-11-10 19:00:00', 12, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Çaykur Rizespor', '2025-11-09 19:00:00', 12, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Beşiktaş', '2025-11-10 21:30:00', 12, 'Süper Lig', '-', 0, 1, 0);

-- 13. HAFTA (22-24 Kasım 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Gençlerbirliği', '2025-11-22 20:00:00', 13, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Kocaelispor', '2025-11-23 19:00:00', 13, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Alanyaspor', '2025-11-23 21:30:00', 13, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Antalyaspor', '2025-11-24 19:00:00', 13, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Gaziantep FK', '2025-11-24 21:30:00', 13, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'İstanbul Başakşehir', '2025-11-23 19:00:00', 13, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Eyüpspor', '2025-11-24 19:00:00', 13, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Kasımpaşa', '2025-11-23 19:00:00', 13, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Konyaspor', '2025-11-24 21:30:00', 13, 'Süper Lig', '-', 0, 1, 0);

-- 14. HAFTA (29 Kasım - 1 Aralık 2025) - TFF Resmi Fikstürü (Fenerbahçe - Galatasaray Derbisi)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Fenerbahçe', 'Galatasaray', '2025-12-01 20:00:00', 14, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Çaykur Rizespor', '2025-11-29 19:00:00', 14, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Samsunspor', '2025-11-30 19:00:00', 14, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Kayserispor', '2025-11-30 21:30:00', 14, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Gaziantep FK', '2025-12-01 19:00:00', 14, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Trabzonspor', '2025-11-30 19:00:00', 14, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Göztepe', '2025-12-01 19:00:00', 14, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Fatih Karagümrük', '2025-11-30 19:00:00', 14, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Beşiktaş', '2025-12-01 21:30:00', 14, 'Süper Lig', '-', 0, 1, 0);

-- 15. HAFTA (6-8 Aralık 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Samsunspor', '2025-12-06 20:00:00', 15, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Gençlerbirliği', '2025-12-07 19:00:00', 15, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Kasımpaşa', '2025-12-07 21:30:00', 15, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Eyüpspor', '2025-12-08 19:00:00', 15, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'İstanbul Başakşehir', '2025-12-08 21:30:00', 15, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Fatih Karagümrük', '2025-12-07 19:00:00', 15, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Alanyaspor', '2025-12-08 19:00:00', 15, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Antalyaspor', '2025-12-07 19:00:00', 15, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Kocaelispor', '2025-12-08 21:30:00', 15, 'Süper Lig', '-', 0, 1, 0);

-- 16. HAFTA (13-15 Aralık 2025) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Antalyaspor', 'Galatasaray', '2025-12-13 20:00:00', 16, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Gaziantep FK', '2025-12-14 19:00:00', 16, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Çaykur Rizespor', '2025-12-14 21:30:00', 16, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Konyaspor', '2025-12-15 19:00:00', 16, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Beşiktaş', '2025-12-15 21:30:00', 16, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Trabzonspor', '2025-12-14 19:00:00', 16, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Samsunspor', '2025-12-15 19:00:00', 16, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Göztepe', '2025-12-14 19:00:00', 16, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Kayserispor', '2025-12-15 21:30:00', 16, 'Süper Lig', '-', 0, 1, 0);

-- 17. HAFTA (20-22 Aralık 2025) - TFF Resmi Fikstürü (İlk Yarı Son Hafta)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Kasımpaşa', '2025-12-20 20:00:00', 17, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Kocaelispor', '2025-12-21 19:00:00', 17, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Alanyaspor', '2025-12-21 21:30:00', 17, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Gençlerbirliği', '2025-12-22 19:00:00', 17, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Gaziantep FK', '2025-12-22 21:30:00', 17, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'İstanbul Başakşehir', '2025-12-21 19:00:00', 17, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Eyüpspor', '2025-12-22 19:00:00', 17, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Fatih Karagümrük', '2025-12-21 19:00:00', 17, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Fenerbahçe', '2025-12-22 21:30:00', 17, 'Süper Lig', '-', 0, 1, 0);

-- ... existing code ...

-- İKİNCİ YARI BAŞLANGIÇ - 18. HAFTA (16-19 Ocak 2026) - TFF Resmi Fikstürü
-- İkinci yarı, ilk yarının tam tersi fikstürle oynanacak
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Gaziantep FK', '2026-01-16 20:00:00', 18, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Fenerbahçe', '2026-01-17 21:30:00', 18, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Çaykur Rizespor', '2026-01-18 19:00:00', 18, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Kayserispor', '2026-01-18 21:30:00', 18, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Galatasaray', '2026-01-19 19:00:00', 18, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Gençlerbirliği', '2026-01-19 19:00:00', 18, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Kasımpaşa', '2026-01-19 21:30:00', 18, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Galatasaray', '2026-01-17 19:00:00', 18, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Kocaelispor', '2026-01-18 19:00:00', 18, 'Süper Lig', '-', 0, 1, 0);

-- 19. HAFTA (23-26 Ocak 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Fatih Karagümrük', 'Galatasaray', '2026-01-23 21:30:00', 19, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Kocaelispor', '2026-01-24 19:00:00', 19, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Alanyaspor', '2026-01-24 21:30:00', 19, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Göztepe', '2026-01-24 21:30:00', 19, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Konyaspor', '2026-01-25 19:00:00', 19, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Gençlerbirliği', '2026-01-25 19:00:00', 19, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Beşiktaş', '2026-01-25 21:30:00', 19, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'İstanbul Başakşehir', '2026-01-25 21:30:00', 19, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Kasımpaşa', '2026-01-26 21:30:00', 19, 'Süper Lig', '-', 0, 1, 0);

-- 20. HAFTA (30 Ocak - 2 Şubat 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Kocaelispor', 'Fenerbahçe', '2026-02-01 19:00:00', 20, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Konyaspor', '2026-02-01 19:00:00', 20, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Samsunspor', '2026-02-01 19:00:00', 20, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Çaykur Rizespor', '2026-01-31 16:00:00', 20, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Trabzonspor', '2026-01-31 18:30:00', 20, 'Süper Lig', '-', 0, 1, 0),
('Galatasaray', 'Kayserispor', '2026-02-02 19:00:00', 20, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Fatih Karagümrük', '2026-01-30 19:00:00', 20, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Gaziantep FK', '2026-01-30 21:30:00', 20, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Göztepe', '2026-01-30 21:30:00', 20, 'Süper Lig', '-', 0, 1, 0);

-- 21. HAFTA (6-9 Şubat 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Çaykur Rizespor', 'Galatasaray', '2026-02-08 20:00:00', 21, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Beşiktaş', '2026-02-09 19:00:00', 21, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Antalyaspor', '2026-02-07 19:00:00', 21, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Eyüpspor', '2026-02-08 19:00:00', 21, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Alanyaspor', '2026-02-07 21:30:00', 21, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'İstanbul Başakşehir', '2026-02-08 21:30:00', 21, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Kasımpaşa', '2026-02-09 21:30:00', 21, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Kocaelispor', '2026-02-07 19:00:00', 21, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Gençlerbirliği', '2026-02-08 19:00:00', 21, 'Süper Lig', '-', 0, 1, 0);

-- 22. HAFTA (13-16 Şubat 2026) - TFF Resmi Fikstürü (Trabzonspor - Fenerbahçe Derbisi)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Trabzonspor', 'Fenerbahçe', '2026-02-15 20:00:00', 22, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Çaykur Rizespor', '2026-02-14 19:00:00', 22, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Fatih Karagümrük', '2026-02-15 19:00:00', 22, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Kayserispor', '2026-02-16 19:00:00', 22, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Samsunspor', '2026-02-15 21:30:00', 22, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Gaziantep FK', '2026-02-16 21:30:00', 22, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Göztepe', '2026-02-14 21:30:00', 22, 'Süper Lig', '-', 0, 1, 0),
('Galatasaray', 'Konyaspor', '2026-02-16 19:00:00', 22, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Antalyaspor', '2026-02-15 19:00:00', 22, 'Süper Lig', '-', 0, 1, 0);

-- 23. HAFTA (20-23 Şubat 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Fatih Karagümrük', 'Galatasaray', '2026-02-22 20:00:00', 23, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Beşiktaş', '2026-02-23 19:00:00', 23, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Trabzonspor', '2026-02-22 21:30:00', 23, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Eyüpspor', '2026-02-21 19:00:00', 23, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Alanyaspor', '2026-02-23 21:30:00', 23, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'İstanbul Başakşehir', '2026-02-22 19:00:00', 23, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Kasımpaşa', '2026-02-21 21:30:00', 23, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Kocaelispor', '2026-02-23 19:00:00', 23, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Gençlerbirliği', '2026-02-22 19:00:00', 23, 'Süper Lig', '-', 0, 1, 0);

-- 24. HAFTA (27 Şubat - 2 Mart 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Eyüpspor', 'Fenerbahçe', '2026-03-01 20:00:00', 24, 'Süper Lig', '-', 0, 1, 0),
('Galatasaray', 'Çaykur Rizespor', '2026-02-28 19:00:00', 24, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Fatih Karagümrük', '2026-03-01 21:30:00', 24, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Kayserispor', '2026-03-02 19:00:00', 24, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Samsunspor', '2026-03-01 19:00:00', 24, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Gaziantep FK', '2026-03-02 21:30:00', 24, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Göztepe', '2026-02-28 21:30:00', 24, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Konyaspor', '2026-03-02 19:00:00', 24, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Antalyaspor', '2026-03-01 19:00:00', 24, 'Süper Lig', '-', 0, 1, 0);

-- 25. HAFTA (6-9 Mart 2026) - TFF Resmi Fikstürü (Beşiktaş - Galatasaray Derbisi)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Beşiktaş', 'Galatasaray', '2026-03-08 20:00:00', 25, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Trabzonspor', '2026-03-07 19:00:00', 25, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Eyüpspor', '2026-03-08 21:30:00', 25, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Alanyaspor', '2026-03-07 21:30:00', 25, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'İstanbul Başakşehir', '2026-03-09 19:00:00', 25, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Kasımpaşa', '2026-03-09 21:30:00', 25, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Kocaelispor', '2026-03-08 19:00:00', 25, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Gençlerbirliği', '2026-03-07 19:00:00', 25, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Beşiktaş', '2026-03-09 19:00:00', 25, 'Süper Lig', '-', 0, 1, 0);

-- 26. HAFTA (13-16 Mart 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'İstanbul Başakşehir', '2026-03-15 21:30:00', 26, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Beşiktaş', '2026-03-14 19:00:00', 26, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Trabzonspor', '2026-03-14 21:30:00', 26, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Samsunspor', '2026-03-15 19:00:00', 26, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Alanyaspor', '2026-03-15 21:30:00', 26, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Göztepe', '2026-03-14 19:00:00', 26, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Kayserispor', '2026-03-15 19:00:00', 26, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Fatih Karagümrük', '2026-03-14 19:00:00', 26, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Konyaspor', '2026-03-15 21:30:00', 26, 'Süper Lig', '-', 0, 1, 0);

-- 27. HAFTA (20-23 Mart 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Göztepe', 'Galatasaray', '2026-03-22 20:00:00', 27, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Antalyaspor', '2026-03-21 19:00:00', 27, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Eyüpspor', '2026-03-22 19:00:00', 27, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Fenerbahçe', '2026-03-21 21:30:00', 27, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Gaziantep FK', '2026-03-22 21:30:00', 27, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Gençlerbirliği', '2026-03-21 19:00:00', 27, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Kasımpaşa', '2026-03-22 19:00:00', 27, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Kocaelispor', '2026-03-21 19:00:00', 27, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Trabzonspor', '2026-03-22 21:30:00', 27, 'Süper Lig', '-', 0, 1, 0);

-- 28. HAFTA (3-6 Nisan 2026) - TFF Resmi Fikstürü (Trabzonspor - Galatasaray Derbisi)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Trabzonspor', 'Galatasaray', '2026-04-05 20:00:00', 28, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Beşiktaş', '2026-04-04 19:00:00', 28, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Çaykur Rizespor', '2026-04-04 21:30:00', 28, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Samsunspor', '2026-04-05 19:00:00', 28, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Fatih Karagümrük', '2026-04-05 21:30:00', 28, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Göztepe', '2026-04-04 19:00:00', 28, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Kayserispor', '2026-04-05 19:00:00', 28, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'İstanbul Başakşehir', '2026-04-04 19:00:00', 28, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Kocaelispor', '2026-04-05 21:30:00', 28, 'Süper Lig', '-', 0, 1, 0);

-- 29. HAFTA (10-13 Nisan 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Kocaelispor', '2026-04-12 20:00:00', 29, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Alanyaspor', '2026-04-11 19:00:00', 29, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Antalyaspor', '2026-04-11 21:30:00', 29, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Eyüpspor', '2026-04-12 19:00:00', 29, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Fenerbahçe', '2026-04-12 21:30:00', 29, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Gaziantep FK', '2026-04-11 19:00:00', 29, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Gençlerbirliği', '2026-04-12 19:00:00', 29, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Kasımpaşa', '2026-04-11 19:00:00', 29, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Konyaspor', '2026-04-12 21:30:00', 29, 'Süper Lig', '-', 0, 1, 0);

-- 30. HAFTA (17-20 Nisan 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Gençlerbirliği', 'Galatasaray', '2026-04-19 20:00:00', 30, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Beşiktaş', '2026-04-18 19:00:00', 30, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Trabzonspor', '2026-04-18 21:30:00', 30, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Samsunspor', '2026-04-19 19:00:00', 30, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Çaykur Rizespor', '2026-04-19 21:30:00', 30, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Göztepe', '2026-04-18 19:00:00', 30, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Kayserispor', '2026-04-19 19:00:00', 30, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Fatih Karagümrük', '2026-04-18 19:00:00', 30, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Fenerbahçe', '2026-04-19 21:30:00', 30, 'Süper Lig', '-', 0, 1, 0);

-- 31. HAFTA (24-27 Nisan 2026) - TFF Resmi Fikstürü (Galatasaray - Fenerbahçe Derbisi)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Fenerbahçe', '2026-04-26 20:00:00', 31, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'Alanyaspor', '2026-04-25 19:00:00', 31, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Antalyaspor', '2026-04-26 19:00:00', 31, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'İstanbul Başakşehir', '2026-04-26 21:30:00', 31, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Eyüpspor', '2026-04-27 19:00:00', 31, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Gençlerbirliği', '2026-04-26 19:00:00', 31, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Kasımpaşa', '2026-04-27 19:00:00', 31, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Kocaelispor', '2026-04-26 19:00:00', 31, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Konyaspor', '2026-04-27 21:30:00', 31, 'Süper Lig', '-', 0, 1, 0);

-- 32. HAFTA (1-4 Mayıs 2026) - TFF Resmi Fikstürü
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Samsunspor', 'Galatasaray', '2026-05-03 20:00:00', 32, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Beşiktaş', '2026-05-02 19:00:00', 32, 'Süper Lig', '-', 0, 1, 0),
('Kasımpaşa', 'Trabzonspor', '2026-05-02 21:30:00', 32, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Çaykur Rizespor', '2026-05-03 19:00:00', 32, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Gaziantep FK', '2026-05-03 21:30:00', 32, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Göztepe', '2026-05-02 19:00:00', 32, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Kayserispor', '2026-05-03 19:00:00', 32, 'Süper Lig', '-', 0, 1, 0),
('Antalyaspor', 'Fenerbahçe', '2026-05-02 19:00:00', 32, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Konyaspor', '2026-05-03 21:30:00', 32, 'Süper Lig', '-', 0, 1, 0);

-- 33. HAFTA (8-11 Mayıs 2026) - TFF Resmi Fikstürü (Beşiktaş - Trabzonspor Derbisi)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Galatasaray', 'Antalyaspor', '2026-05-10 20:00:00', 33, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Alanyaspor', '2026-05-09 19:00:00', 33, 'Süper Lig', '-', 0, 1, 0),
('Çaykur Rizespor', 'İstanbul Başakşehir', '2026-05-09 21:30:00', 33, 'Süper Lig', '-', 0, 1, 0),
('Konyaspor', 'Eyüpspor', '2026-05-10 19:00:00', 33, 'Süper Lig', '-', 0, 1, 0),
('Beşiktaş', 'Fatih Karagümrük', '2026-05-10 21:30:00', 33, 'Süper Lig', '-', 0, 1, 0),
('Trabzonspor', 'Gençlerbirliği', '2026-05-09 19:00:00', 33, 'Süper Lig', '-', 0, 1, 0),
('Samsunspor', 'Kasımpaşa', '2026-05-10 19:00:00', 33, 'Süper Lig', '-', 0, 1, 0),
('Göztepe', 'Kocaelispor', '2026-05-09 19:00:00', 33, 'Süper Lig', '-', 0, 1, 0),
('Kayserispor', 'Fenerbahçe', '2026-05-10 21:30:00', 33, 'Süper Lig', '-', 0, 1, 0);

-- 34. HAFTA (15-17 Mayıs 2026) - TFF Resmi Fikstürü (Sezon Finali)
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Hafta, Liga, Skor, Durum, OtomatikYorumToplamaAktif, YorumlarToplandi) VALUES
('Kasımpaşa', 'Galatasaray', '2026-05-17 20:00:00', 34, 'Süper Lig', '-', 0, 1, 0),
('Kocaelispor', 'Beşiktaş', '2026-05-17 20:00:00', 34, 'Süper Lig', '-', 0, 1, 0),
('Alanyaspor', 'Trabzonspor', '2026-05-17 20:00:00', 34, 'Süper Lig', '-', 0, 1, 0),
('Gençlerbirliği', 'Samsunspor', '2026-05-17 20:00:00', 34, 'Süper Lig', '-', 0, 1, 0),
('Gaziantep FK', 'Çaykur Rizespor', '2026-05-17 20:00:00', 34, 'Süper Lig', '-', 0, 1, 0),
('İstanbul Başakşehir', 'Göztepe', '2026-05-17 20:00:00', 34, 'Süper Lig', '-', 0, 1, 0),
('Eyüpspor', 'Kayserispor', '2026-05-17 20:00:00', 34, 'Süper Lig', '-', 0, 1, 0),
('Fatih Karagümrük', 'Antalyaspor', '2026-05-17 20:00:00', 34, 'Süper Lig', '-', 0, 1, 0),
('Fenerbahçe', 'Konyaspor', '2026-05-17 20:00:00', 34, 'Süper Lig', '-', 0, 1, 0);

-- ... existing code ...