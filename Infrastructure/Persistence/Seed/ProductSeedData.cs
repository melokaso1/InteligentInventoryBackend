using Domain.Constants;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Persistence.Seed;

// Catálogo ficticio humorístico de El Plonsazo (taller de programación, no comercio real).
// Precios COP por SaleUnit, calibrados para sentirse creíbles en el juego (no millones absurdos):
//   gramo — cannabis 15k–65k, hongos 25k–38k, bazuco ~12k, cocaína ~180k, heroína ~220k, DMT ~280k, tussi/MDMA 95k–135k
//   unidad — pastillas/blotters 18k–55k, pre-rolls/poppers/gomitas 18k–42k, frascos CBD/keta 85k–180k
//   mililitro — LSD líquido ~85k/ml
// Para re-sembrar catálogo completo desde cero: docker compose down -v && docker compose up -d
// Para DB existente: reiniciar la API (upsert añade PLZ-* faltantes y sincroniza nombre/descripción/categoría).
internal static class ProductSeedData
{
    internal const decimal DefaultSeedStock = 10m;

    internal static List<Product> Create(
        Dictionary<string, Category> categoryByName,
        Dictionary<string, Warehouse> warehouseByName) =>
    [
        // —— Alucinógenos ——
        P(categoryByName, warehouseByName, "PLZ-MJ-001", "Marihuana Sativa Indoor Premium", "Alucinógenos", 45_000, 200, "eco", WarehouseNames.AlmacenNorte, "Cogollo sativa indoor — ficción de catálogo El Plonsazo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MJ-002", "Marihuana Índica", "Alucinógenos", 28_000, 150, "eco", WarehouseNames.AlmacenNorte, "Índica clásica — inventario ficticio (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MJ-003", "Marihuana Híbrida Blue Dream", "Alucinógenos", 42_000, 120, "eco", WarehouseNames.AlmacenNorte, "Híbrido Blue Dream — solo datos de demo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MJ-010", "Flores Premium Reserva", "Alucinógenos", 65_000, 80, "local_florist", WarehouseNames.BodegaSur, "Flores premium de reserva — catálogo ficticio.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MJ-011", "Pre-rolled Sativa x6", "Alucinógenos", 18_000, 200, "smoking_rooms", WarehouseNames.CentralBogota, "Pack pre-rolled sativa x6 — taller (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-MJ-012", "Aceite CBD 10%", "Alucinógenos", 85_000, 60, "water_drop", WarehouseNames.CentralBogota, "Aceite CBD 10% — frasco fijo de 30 ml (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-MJ-013", "Hash Marroquí Premium", "Alucinógenos", 55_000, 50, "grain", WarehouseNames.BodegaSur, "Hash premium — catálogo ficticio El Plonsazo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MJ-014", "Marihuana Gorilla Glue #4", "Alucinógenos", 48_000, 100, "eco", WarehouseNames.AlmacenNorte, "Híbrido Gorilla Glue #4 — cogollo indoor ficticio.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MJ-015", "Marihuana OG Kush Indoor", "Alucinógenos", 38_000, 120, "eco", WarehouseNames.AlmacenNorte, "OG Kush indoor — variante de catálogo El Plonsazo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-LSD-042", "LSD-25 Blotter 200µg", "Alucinógenos", 45_000, 50, "science", WarehouseNames.BodegaSur, "Blotter 200µg — ficción para el taller.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-LSD-043", "Microdosis LSD 10µg", "Alucinógenos", 18_000, 100, "science", WarehouseNames.BodegaSur, "Microdosis 10µg — demo de stock (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-LSD-044", "Gel Tabs LSD 150µg", "Alucinógenos", 55_000, 80, "science", WarehouseNames.BodegaSur, "Gel tabs 150µg — catálogo ficticio.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-LSD-045", "LSD Líquido 100µg/ml", "Alucinógenos", 85_000, 30, "biotech", WarehouseNames.BodegaSur, "LSD líquido — venta por mililitro (ficción).", SaleMeasureUnit.Milliliter),
        P(categoryByName, warehouseByName, "PLZ-HNG-033", "Hongos Psilocybe Cubensis", "Alucinógenos", 25_000, 60, "spa", WarehouseNames.BodegaSur, "Hongos cubensis — datos ficticios.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-HNG-034", "Psilocybe Trufas Holandesas", "Alucinógenos", 38_000, 40, "spa", WarehouseNames.BodegaSur, "Trufas psilocybe holandesas — taller El Plonsazo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-HNG-035", "Hongos Penis Envy", "Alucinógenos", 42_000, 50, "spa", WarehouseNames.BodegaSur, "Psilocybe Penis Envy — variante de hongos ficticios.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-HNG-036", "Hongos Golden Teacher", "Alucinógenos", 28_000, 60, "spa", WarehouseNames.BodegaSur, "Golden Teacher — cepa clásica de catálogo demo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-DMT-012", "DMT Cristalizado", "Alucinógenos", 280_000, 25, "biotech", WarehouseNames.BodegaSur, "DMT cristalizado — catálogo humorístico (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-DMT-013", "DMT Freebase Cristal", "Alucinógenos", 295_000, 20, "biotech", WarehouseNames.BodegaSur, "DMT freebase — variante cristalina de ficción.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-DMT-014", "DMT Changa Blend 50/50", "Alucinógenos", 120_000, 35, "spa", WarehouseNames.BodegaSur, "Changa DMT + ayahuasca — mezcla por gramo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-THC-073", "Gomitas THC 10mg x5", "Alucinógenos", 42_000, 120, "medication", WarehouseNames.AlmacenNorte, "Pack de 5 gomitas THC — 50 mg total por pack (ficción).", SaleMeasureUnit.Unit, unitContentAmount: 50m, unitContentMeasure: SaleMeasureUnit.Milligram),
        P(categoryByName, warehouseByName, "PLZ-THC-074", "Gomitas CBD Relax", "Alucinógenos", 28_000, 100, "medication", WarehouseNames.AlmacenNorte, "Gomitas CBD relax — pack de 10 unidades (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-AYA-080", "Ayahuasca", "Alucinógenos", 95_000, 30, "water_drop", WarehouseNames.BodegaSur, "Brebaje ayahuasca — frasco fijo de 250 ml (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-MES-081", "Mescalina San Pedro Cactus", "Alucinógenos", 72_000, 40, "spa", WarehouseNames.BodegaSur, "Extracto de cactus San Pedro — demo por gramo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MES-082", "Mescalina Peyote Extracto", "Alucinógenos", 68_000, 35, "spa", WarehouseNames.BodegaSur, "Extracto de peyote — variante mescalina ficticia.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-IBO-082", "Ibogaina", "Alucinógenos", 165_000, 20, "science", WarehouseNames.BodegaSur, "Ibogaina en polvo — catálogo ficticio.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-NBO-083", "NBOMes Blotter 25I", "Alucinógenos", 38_000, 60, "science", WarehouseNames.BodegaSur, "Blotter NBOMe — solo para el taller (ficción).", SaleMeasureUnit.Unit),

        // —— Estimulantes ——
        P(categoryByName, warehouseByName, "PLZ-TUS-015", "Tussi Rosa 2C-B", "Estimulantes", 120_000, 80, "medication", WarehouseNames.CentralBogota, "Tussi rosa (2C-B) — ficción humorística.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-TUS-016", "Tussi Champagne", "Estimulantes", 135_000, 80, "medication", WarehouseNames.CentralBogota, "Tussi champagne — demo de catálogo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MDM-088", "MDMA Cristal Europa", "Estimulantes", 95_000, 200, "diamond", WarehouseNames.CentralBogota, "MDMA cristal — seed ficticio.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MDM-089", "MDMA Pastillas Red Bull", "Estimulantes", 55_000, 150, "medication", WarehouseNames.CentralBogota, "Pastillas MDMA — catálogo demo (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-COC-099", "Cocaína Perlada — Polvo", "Estimulantes", 180_000, 50, "grain", WarehouseNames.CentralBogota, "Cocaína perlada — precio por gramo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-COC-100", "Crack (Cocaína Base)", "Estimulantes", 95_000, 25, "grain", WarehouseNames.CentralBogota, "Crack — cocaína base, solo para el workshop (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-COC-101", "Cocaína HCl — Polvo Fino", "Estimulantes", 174_000, 45, "grain", WarehouseNames.CentralBogota, "Clorhidrato de cocaína — polvo fino por gramo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-COC-102", "Pasta Base Cocaína", "Estimulantes", 15_000, 80, "grain", WarehouseNames.CentralBogota, "Pasta base — precursor ficticio vendido por gramo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-EXT-056", "Éxtasis Tesla 300mg", "Estimulantes", 48_000, 120, "medication", WarehouseNames.CentralBogota, "Éxtasis (MDMA) Tesla — catálogo ficticio El Plonsazo.", SaleMeasureUnit.Unit, unitContentAmount: 300m, unitContentMeasure: SaleMeasureUnit.Milligram),
        P(categoryByName, warehouseByName, "PLZ-EXT-057", "Éxtasis Punisher 280mg", "Estimulantes", 52_000, 100, "medication", WarehouseNames.CentralBogota, "Éxtasis Punisher — pastilla MDMA ficticia.", SaleMeasureUnit.Unit, unitContentAmount: 280m, unitContentMeasure: SaleMeasureUnit.Milligram),
        P(categoryByName, warehouseByName, "PLZ-EXT-058", "Éxtasis Pink Porsche 250mg", "Estimulantes", 46_000, 110, "medication", WarehouseNames.CentralBogota, "Pink Porsche — variante éxtasis de demo.", SaleMeasureUnit.Unit, unitContentAmount: 250m, unitContentMeasure: SaleMeasureUnit.Milligram),
        P(categoryByName, warehouseByName, "PLZ-BAZ-070", "Bazuco", "Estimulantes", 12_000, 100, "grain", WarehouseNames.CentralBogota, "Bazuco — polvo vendido por gramo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-SAL-072", "Sales de baño Arctic Blast", "Estimulantes", 65_000, 80, "science", WarehouseNames.CentralBogota, "Sales de baño (catinonas) — sobre de 250 g (ficción).", SaleMeasureUnit.Unit, unitContentAmount: 250m, unitContentMeasure: SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MET-090", "Metanfetamina Cristalina Ice", "Estimulantes", 145_000, 40, "diamond", WarehouseNames.CentralBogota, "Metanfetamina cristal ice — ficción de catálogo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-MET-092", "Metanfetamina Pastillas Yaba", "Estimulantes", 38_000, 90, "medication", WarehouseNames.CentralBogota, "Yaba TH — pastillas de metanfetamina ficticias.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-MET-093", "Metanfetamina Pink Ice Cristal", "Estimulantes", 155_000, 35, "diamond", WarehouseNames.CentralBogota, "Pink ice — variante cristalina de demo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-AMP-091", "Anfetamina Speed Pastillas", "Estimulantes", 32_000, 100, "medication", WarehouseNames.CentralBogota, "Anfetamina (speed) en pastillas — demo de filtros (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-KHT-092", "Khat Hojas Frescas", "Estimulantes", 22_000, 60, "eco", WarehouseNames.CentralBogota, "Hojas de khat — inventario ficticio.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-COC-093", "Hoja de Coca Tradicional", "Estimulantes", 18_000, 80, "eco", WarehouseNames.CentralBogota, "Hoja de coca seca — referencia de catálogo (ficción).", SaleMeasureUnit.Gram),

        // —— Inhalantes ——
        P(categoryByName, warehouseByName, "PLZ-POP-007", "Popper Rush XL (Amil)", "Inhalantes", 35_000, 100, "air", WarehouseNames.CentralBogota, "Popper amil nitrito — frasco fijo (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-POP-008", "Popper Nitrito Isopropílico", "Inhalantes", 28_000, 80, "air", WarehouseNames.CentralBogota, "Popper isopropílico — inventario de taller (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-AER-060", "Aerosol Tolueno", "Inhalantes", 15_000, 120, "air", WarehouseNames.AlmacenNorte, "Aerosol de solvente tolueno — ficción para filtros.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-SOL-061", "Disolvente Volátil Tolueno", "Inhalantes", 12_000, 100, "science", WarehouseNames.AlmacenNorte, "Disolvente volátil — solo demo El Plonsazo (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-GAS-062", "Gas Hilarante N2O x10", "Inhalantes", 48_000, 80, "air", WarehouseNames.CentralBogota, "Óxido nitroso (N2O) x10 — catálogo humorístico (ficción).", SaleMeasureUnit.Unit),

        // —— Disociativos ——
        P(categoryByName, warehouseByName, "PLZ-KET-021", "Ketamina Líquida 50ml", "Disociativos", 180_000, 40, "vaccines", WarehouseNames.BodegaSur, "Ketamina líquida — frasco fijo de 50 ml (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-KET-022", "Ketamina Polvo Cristalino S", "Disociativos", 125_000, 50, "science", WarehouseNames.BodegaSur, "Ketamina en polvo cristalino — venta por gramo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-KET-023", "Ketamina Spray Nasal Esketamina", "Disociativos", 95_000, 60, "medication", WarehouseNames.BodegaSur, "Spray nasal esketamina — frasco unitario demo.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-PCP-022", "PCP (Fenciclidina)", "Disociativos", 88_000, 30, "science", WarehouseNames.BodegaSur, "PCP en polvo — droga disociativa de demo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-PCP-024", "PCP Líquido Angel Dust", "Disociativos", 72_000, 25, "water_drop", WarehouseNames.BodegaSur, "PCP líquido — variante disociativa ficticia.", SaleMeasureUnit.Unit),

        // —— Depresores ——
        P(categoryByName, warehouseByName, "PLZ-HER-071", "Heroína", "Depresores", 220_000, 40, "medication", WarehouseNames.BodegaSur, "Heroína — precio por gramo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-HER-072", "Heroína Black Tar", "Depresores", 195_000, 35, "medication", WarehouseNames.BodegaSur, "Black tar — variante heroína ficticia por gramo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-HER-073", "Heroína #4 Polvo Blanco", "Depresores", 235_000, 30, "medication", WarehouseNames.BodegaSur, "Heroína #4 — polvo blanco de catálogo demo.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-GHB-075", "GHB", "Depresores", 52_000, 50, "water_drop", WarehouseNames.BodegaSur, "GHB líquido — frasco fijo de 30 ml (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-BAR-076", "Secobarbital", "Depresores", 28_000, 60, "medication", WarehouseNames.BodegaSur, "Secobarbital — barbitúrico sedante ficticio.", SaleMeasureUnit.Unit),

        // —— Opioides ——
        P(categoryByName, warehouseByName, "PLZ-OPI-001", "Fentanilo Parche Transdérmico", "Opioides", 185_000, 30, "medication", WarehouseNames.BodegaSur, "Parche fentanilo — ficción de catálogo.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-OPI-002", "Codeína Jarabe", "Opioides", 38_000, 80, "medication", WarehouseNames.BodegaSur, "Jarabe con codeína — demo de filtros (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-OPI-003", "Morfina Ampolla", "Opioides", 95_000, 40, "vaccines", WarehouseNames.BodegaSur, "Ampolla de morfina — inventario ficticio.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-OPI-004", "Metadona Jarabe", "Opioides", 42_000, 50, "medication", WarehouseNames.BodegaSur, "Jarabe de metadona — taller El Plonsazo (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-OPI-005", "Hidromorfona Comprimidos", "Opioides", 68_000, 40, "medication", WarehouseNames.BodegaSur, "Hidromorfona — catálogo de prescripción ficticia.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-OPI-006", "Meperidina Ampollas", "Opioides", 55_000, 35, "medication", WarehouseNames.BodegaSur, "Meperidina en ampollas — solo demo (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-OPI-007", "Oxycodona OxyContin 40mg", "Opioides", 125_000, 25, "medication", WarehouseNames.BodegaSur, "Oxycodona OxyContin — comprimido ficticio.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-OPI-008", "Tramadol 50mg Caja x20", "Opioides", 32_000, 70, "medication", WarehouseNames.BodegaSur, "Tramadol 50 mg — caja de 20 comprimidos demo.", SaleMeasureUnit.Unit),

        // —— Cannabis ——
        P(categoryByName, warehouseByName, "PLZ-CAN-001", "Delta-8 THC Gummies x10", "Cannabis", 35_000, 100, "medication", WarehouseNames.AlmacenNorte, "Gomitas delta-8 — pack de 10 unidades (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-CAN-002", "Flores Cannabis Outdoor", "Cannabis", 32_000, 150, "eco", WarehouseNames.AlmacenNorte, "Cannabis outdoor — cogollo por gramo (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-CAN-003", "Cannabis Autoflower Northern Lights", "Cannabis", 36_000, 120, "eco", WarehouseNames.AlmacenNorte, "Northern Lights autoflower — variante outdoor ficticia.", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-CAN-004", "Resina Live Rosin THC", "Cannabis", 75_000, 40, "grain", WarehouseNames.AlmacenNorte, "Live rosin — concentrado de cannabis por gramo.", SaleMeasureUnit.Gram),

        // —— Cannabinoides sintéticos ——
        P(categoryByName, warehouseByName, "PLZ-K2-001", "K2 Spice Incense", "Cannabinoides sintéticos", 25_000, 90, "air", WarehouseNames.CentralBogota, "Incienso K2 — cannabinoide sintético ficticio.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-K2-002", "K2 Liquid Vape Cartucho", "Cannabinoides sintéticos", 48_000, 70, "smoking_rooms", WarehouseNames.CentralBogota, "Cartucho K2 líquido — demo de filtros (ficción).", SaleMeasureUnit.Unit),

        // —— Drogas sintéticas ——
        P(categoryByName, warehouseByName, "PLZ-SYN-001", "Flakka (Alpha-PVP)", "Drogas sintéticas", 78_000, 45, "science", WarehouseNames.CentralBogota, "Flakka (alpha-PVP) — catálogo humorístico (ficción).", SaleMeasureUnit.Gram),
        P(categoryByName, warehouseByName, "PLZ-SYN-002", "Catinonas (Mephedrone)", "Drogas sintéticas", 58_000, 55, "science", WarehouseNames.CentralBogota, "Catinonas sintéticas — sobre de 200 g (ficción).", SaleMeasureUnit.Unit, unitContentAmount: 200m, unitContentMeasure: SaleMeasureUnit.Gram),

        // —— Nicotina ——
        P(categoryByName, warehouseByName, "PLZ-NIC-001", "Vape Desechable Mango Ice", "Nicotina", 22_000, 200, "smoking_rooms", WarehouseNames.CentralBogota, "Cigarrillo electrónico desechable — ficción.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-NIC-002", "Pod Nicotina 5% Salt", "Nicotina", 38_000, 150, "smoking_rooms", WarehouseNames.CentralBogota, "Pod recargable nicotina 5% — demo (ficción).", SaleMeasureUnit.Unit),

        // —— Esteroides anabólicos ——
        P(categoryByName, warehouseByName, "PLZ-STR-001", "Testosterona Enantato 250mg/ml", "Esteroides anabólicos", 120_000, 40, "fitness_center", WarehouseNames.AlmacenNorte, "Testosterona enantato — frasco 10 ml (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-STR-002", "Nandrolona Decanoato (Deca)", "Esteroides anabólicos", 145_000, 30, "fitness_center", WarehouseNames.AlmacenNorte, "Nandrolona decanoato — catálogo ficticio.", SaleMeasureUnit.Unit),

        // —— Medicamentos con prescripción ——
        P(categoryByName, warehouseByName, "PLZ-RX-001", "Metilfenidato 10mg Caja", "Medicamentos con prescripción", 48_000, 80, "medication", WarehouseNames.CentralBogota, "Metilfenidato (Ritalin) — caja x30 (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-RX-002", "Hidrocodona Compuesto", "Medicamentos con prescripción", 72_000, 40, "medication", WarehouseNames.CentralBogota, "Hidrocodona con APAP — prescripción demo (ficción).", SaleMeasureUnit.Unit),

        // —— Benzodiacepinas ——
        P(categoryByName, warehouseByName, "PLZ-BNZ-001", "Flunitrazepam (Rohypnol)", "Benzodiacepinas", 35_000, 50, "medication", WarehouseNames.BodegaSur, "Flunitrazepam — medicamento Z ficticio.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-BNZ-002", "Clonazepam 2mg", "Benzodiacepinas", 22_000, 70, "medication", WarehouseNames.BodegaSur, "Clonazepam — medicamento para dormir demo (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-BNZ-003", "Diazepam 10mg Valium", "Benzodiacepinas", 18_000, 80, "medication", WarehouseNames.BodegaSur, "Diazepam Valium — variante benzodiacepina ficticia.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-BNZ-004", "Alprazolam 2mg Xanax", "Benzodiacepinas", 25_000, 75, "medication", WarehouseNames.BodegaSur, "Alprazolam Xanax — comprimido de catálogo demo.", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-BNZ-005", "Lorazepam Ativan 2mg", "Benzodiacepinas", 20_000, 65, "medication", WarehouseNames.BodegaSur, "Lorazepam Ativan — variante benzo de taller.", SaleMeasureUnit.Unit),

        // —— Antitusivos (DXM, loperamida) ——
        P(categoryByName, warehouseByName, "PLZ-OTC-001", "Dextrometorfano (DXM) Jarabe", "Antitusivos", 18_000, 100, "medication", WarehouseNames.AlmacenNorte, "Dextrometorfano jarabe — antitusivo OTC demo (ficción).", SaleMeasureUnit.Unit),
        P(categoryByName, warehouseByName, "PLZ-OTC-002", "Loperamida Imodium Pack x12", "Antitusivos", 15_000, 120, "medication", WarehouseNames.AlmacenNorte, "Loperamida — pack de 12 cápsulas ficticias.", SaleMeasureUnit.Unit),
    ];

    private static Product P(
        Dictionary<string, Category> categoryByName,
        Dictionary<string, Warehouse> warehouseByName,
        string code, string name, string categoryName, decimal price, decimal maxStock,
        string icon, string warehouseName, string description,
        SaleMeasureUnit saleUnit,
        ProductStatus status = ProductStatus.Active,
        decimal? unitContentAmount = null,
        SaleMeasureUnit? unitContentMeasure = null)
    {
        var category = categoryByName[categoryName];
        var warehouse = warehouseByName[warehouseName];
        var now = DateTime.UtcNow;
        var currentStock = status == ProductStatus.OutOfStock ? 0m : DefaultSeedStock;

        return new Product
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            CategoryId = category.Id,
            Category = category,
            Price = price,
            SaleUnit = saleUnit,
            UnitContentAmount = unitContentAmount,
            UnitContentMeasure = unitContentMeasure,
            Status = status,
            Icon = icon,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now,
            Inventories =
            [
                new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouse.Id,
                    Warehouse = warehouse,
                    CurrentStock = currentStock,
                    MinStock = Math.Max(1m, maxStock / 4m),
                    MaxStock = maxStock,
                    UpdatedAt = now,
                },
            ],
        };
    }
}
