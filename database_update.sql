-- 2025-2026 Sezonu İlk Hafta Maçları
INSERT INTO Maclar (EvSahibi, Deplasman, MacTarihi, Skor, Liga, Hafta) VALUES
('Gaziantep FK', 'Galatasaray', '2025-08-08 21:30:00', '0-3', 'Süper Lig', 1),
('Eyüpspor', 'Konyaspor', '2025-08-10 19:00:00', '1-4', 'Süper Lig', 1),
('Çaykur Rizespor', 'Göztepe', '2025-08-10 21:30:00', '0-3', 'Süper Lig', 1);

-- Gaziantep FK - Galatasaray maçının tartışmalı pozisyonları
DECLARE @MacId1 INT = (SELECT TOP 1 Id FROM Maclar WHERE EvSahibi = 'Gaziantep FK' AND Deplasman = 'Galatasaray' AND Hafta = 1 AND Liga = 'Süper Lig' ORDER BY Id DESC);

INSERT INTO Pozisyonlar (MacId, Aciklama, Dakika, PozisyonTuru, VideoUrl) VALUES
(@MacId1, 'Torreira''nin Lungoyi''ye müdahalesi - Gaziantep penaltı beklentisi', 1, 'Tartışmalı Pozisyon', 'https://www.youtube.com/watch?v=dQw4w9WgXcQ'),
(@MacId1, 'Sallai''nin çekilmesi - VAR müdahalesi ile penaltı', 8, 'Penaltı', 'https://www.youtube.com/watch?v=abc123def456'),
(@MacId1, 'Barış Alper Yılmaz''ın ikinci penaltısı', 45, 'Penaltı', 'https://www.youtube.com/watch?v=xyz789uvw012'),
(@MacId1, 'Kaleci Burak Bozan''ın kırmızı kartı - Elle oynama', 30, 'Kırmızı Kart', 'https://www.youtube.com/watch?v=mno345pqr678');

-- Pozisyon ID'lerini al
DECLARE @Pozisyon1 INT = (SELECT Id FROM Pozisyonlar WHERE MacId = @MacId1 AND Dakika = 1);
DECLARE @Pozisyon8 INT = (SELECT Id FROM Pozisyonlar WHERE MacId = @MacId1 AND Dakika = 8);
DECLARE @Pozisyon45 INT = (SELECT Id FROM Pozisyonlar WHERE MacId = @MacId1 AND Dakika = 45);
DECLARE @Pozisyon30 INT = (SELECT Id FROM Pozisyonlar WHERE MacId = @MacId1 AND Dakika = 30);

-- Hakem yorumları (Tüm hakemler için tüm pozisyonlar)
INSERT INTO HakemYorumlari (PozisyonId, YorumcuAdi, Yorum, DogruKarar, YorumTarihi, Kanal) VALUES
-- 1. dakika Torreira pozisyonu için tüm hakem yorumları
(@Pozisyon1, 'Fırat Aydınus', 'Penaltı beklentisi de hakemin oyunu devam ettirmesi de hatalı. Burada faul var ama faul Torreira''ya yapılıyor.', 0, GETDATE(), 'Hürriyet'),
(@Pozisyon1, 'Deniz Çoban', 'Penaltı beklentisi de hakemin oyunu devam ettirmesi de hatalı. Burada faul var ama faul Torreira''ya yapılıyor. Şiddet yoğunluk olmadığı için kırmızı karttan bahsedemeyiz ama çok klasik bir sarı kart.', 0, GETDATE(), 'beIN Sports'),
(@Pozisyon1, 'Bahattin Duran', 'Hakemin devam kararı doğru mu? Penaltı yok. Galatasaray lehine faul verilmeli.', 0, GETDATE(), 'beIN Sports'),
(@Pozisyon1, 'Bülent Yıldırım', 'Riskli bir müdahale ama zamanlama doğru. Torreira ilk topa müdahaleyi yapıyor. Benim fikrim de penaltı yok. Burada bir hücum faul var.', 0, GETDATE(), 'beIN Sports'),

-- 8. dakika VAR penaltısı için tüm hakem yorumları
(@Pozisyon8, 'Deniz Çoban', 'Açık, net bir şekilde penaltı var. VAR müdahalesi doğru. Hakem yanlış yerde durduğu için pozisyonu görmüyor.', 1, GETDATE(), 'beIN Sports'),
(@Pozisyon8, 'Bahattin Duran', 'Ben pozisyonun başında Sallai''nin rakibi Rodrigues''e şiddetli bir çekme yaptığı ve pozisyonun faul olduğu düşüncesindeyim ancak o sırada top oyunda değil. Top oyuna girdikten sonra Rodrigues şiddetli şekilde Sallai''yi çektiği için hem VAR müdahalesinin hem de penaltı kararının doğru olduğunu düşünüyorum.', 1, GETDATE(), 'beIN Sports'),
(@Pozisyon8, 'Bülent Yıldırım', 'Hiçbir şüphe yok net bir penaltı. Top oyunda olmadığı zamana karışılamadığı için VAR müdahalesi de doğruydu.', 1, GETDATE(), 'beIN Sports'),

-- 45. dakika ikinci penaltı için tüm hakem yorumları
(@Pozisyon45, 'Deniz Çoban', 'Burada dikkatsiz bir müdahale olduğunu düşünüyorum. Bunun kararını verecek olan hakem ve kararını penaltı olarak gösterdi. Bu penaltıda hiçbir sıkıntı yok.', 1, GETDATE(), 'beIN Sports'),
(@Pozisyon45, 'Bahattin Duran', 'Çok doğru bir karar. Dikkatsiz müdahale.', 1, GETDATE(), 'beIN Sports'),
(@Pozisyon45, 'Bülent Yıldırım', 'Çok dikkatsiz bir müdahale. Hakem gördü ve çaldı. Karar doğru.', 1, GETDATE(), 'beIN Sports'),

-- 30. dakika kırmızı kart için tüm hakem yorumları
(@Pozisyon30, 'Deniz Çoban', 'Tespit doğru, kırmızı kart doğru ve müdahale doğru ancak hakem daha önceden hareketlense pozisyonu görebilir ve VAR müdahalesine gerek kalmazdı.', 1, GETDATE(), 'beIN Sports'),
(@Pozisyon30, 'Bahattin Duran', 'Doğru bir VAR müdahalesi ve doğru bir karar.', 1, GETDATE(), 'beIN Sports'),
(@Pozisyon30, 'Bülent Yıldırım', 'Kaleci ilk çıkışında doğru geliyor ancak sonrasında ihlal gerçekleştiriyor. Karar doğru.', 1, GETDATE(), 'beIN Sports');