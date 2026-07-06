using Domain.Constants;

using Domain.Entities;

using Domain.Enums;



namespace Infrastructure.Persistence.Seed;



// Catálogo de sustancias para el inventario El Plonsazo.

// Precios COP por SaleUnit, calibrados con referencias públicas de mercado colombiano:

//   barato — bazuco 3.5k/g, pasta base 9k/g, hoja coca 6k/g, inhalantes 6.5k–8k/unidad

//   medio — cannabis 12k–38k/g, éxtasis/LSD 12k–32k/unidad, crack 28k/g, benzos 12k–28k/unidad

//   alto — cocaína HCl 38k–52k/g, tussi 78k–88k/g, MDMA cristal ~78k/g, metanfetamina 92k–102k/g

//   premium — heroína 92k–115k/g, DMT 175k–185k/g, ketamina frasco 120k/unidad, opioides 24k–145k/unidad

// Para re-sembrar catálogo completo desde cero: docker compose down -v && docker compose up -d

// Para DB existente: reiniciar la API (upsert añade PLZ-* faltantes y sincroniza nombre/descripción/categoría).

internal static class ProductSeedData

{

    internal const decimal DefaultSeedStock = 10m;

    internal static int ExpectedProductCount => GetAll(StubCategories(), StubWarehouses()).Count;

    /// <summary>Single source of truth for the PLZ-* catalog (requires seeded categories and warehouses).</summary>
    internal static List<Product> GetAll(

        Dictionary<string, Category> categoryByName,

        Dictionary<string, Warehouse> warehouseByName) =>

    [

        // —— Alucinógenos ——

        P(categoryByName, warehouseByName, "PLZ-MJ-001", "Marihuana Sativa Indoor Premium", "Alucinógenos", 32_000, 200, "eco", WarehouseNames.AlmacenNorte, "Cogollo sativa indoor premium, cultivo controlado.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MJ-002", "Marihuana Índica", "Alucinógenos", 15_000, 150, "eco", WarehouseNames.AlmacenNorte, "Índica clásica, flor seca por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MJ-003", "Marihuana Híbrida Blue Dream", "Alucinógenos", 28_000, 120, "eco", WarehouseNames.AlmacenNorte, "Híbrido Blue Dream, cogollo indoor.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MJ-010", "Flores Premium Reserva", "Alucinógenos", 38_000, 80, "local_florist", WarehouseNames.BodegaSur, "Flores premium de reserva, selección top shelf.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MJ-011", "Pre-rolled Sativa x6", "Alucinógenos", 12_000, 200, "smoking_rooms", WarehouseNames.CentralBogota, "Pack pre-rolled sativa x6 unidades.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-MJ-012", "Aceite CBD 10%", "Alucinógenos", 55_000, 60, "water_drop", WarehouseNames.CentralBogota, "Aceite CBD 10%, frasco 30 ml.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-MJ-013", "Hash Marroquí Premium", "Alucinógenos", 32_000, 50, "grain", WarehouseNames.BodegaSur, "Hash marroquí premium, resina prensada.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MJ-014", "Marihuana Gorilla Glue #4", "Alucinógenos", 30_000, 100, "eco", WarehouseNames.AlmacenNorte, "Gorilla Glue #4, cogollo indoor híbrido.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MJ-015", "Marihuana OG Kush Indoor", "Alucinógenos", 22_000, 120, "eco", WarehouseNames.AlmacenNorte, "OG Kush indoor, flor seca premium.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-LSD-042", "LSD-25 Blotter 200µg", "Alucinógenos", 28_000, 50, "science", WarehouseNames.BodegaSur, "Ácido lisérgico (LSD-25), blotter 200 µg por dosis.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-LSD-043", "Microdosis LSD 10µg", "Alucinógenos", 12_000, 100, "science", WarehouseNames.BodegaSur, "Microdosis LSD 10 µg por blotter.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-LSD-044", "Gel Tabs LSD 150µg", "Alucinógenos", 32_000, 80, "science", WarehouseNames.BodegaSur, "Gel tabs LSD 150 µg por unidad.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-LSD-045", "LSD Líquido 100µg/ml", "Alucinógenos", 45_000, 30, "biotech", WarehouseNames.BodegaSur, "LSD líquido 100 µg/ml, venta por mililitro.", SaleMeasureUnit.Milliliter),

        P(categoryByName, warehouseByName, "PLZ-HNG-033", "Hongos Psilocybe Cubensis", "Alucinógenos", 20_000, 60, "spa", WarehouseNames.BodegaSur, "Psilocybe cubensis, hongos secos por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-HNG-034", "Psilocybe Trufas Holandesas", "Alucinógenos", 26_000, 40, "spa", WarehouseNames.BodegaSur, "Trufas psilocybe holandesas, sclerotia por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-HNG-035", "Hongos Penis Envy", "Alucinógenos", 28_000, 50, "spa", WarehouseNames.BodegaSur, "Psilocybe cubensis Penis Envy, cepa potente.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-HNG-036", "Hongos Golden Teacher", "Alucinógenos", 18_000, 60, "spa", WarehouseNames.BodegaSur, "Golden Teacher, cepa cubensis clásica.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-DMT-012", "DMT Cristalizado", "Alucinógenos", 175_000, 25, "biotech", WarehouseNames.BodegaSur, "DMT (N,N-dimetiltriptamina) cristalizado, polvo por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-DMT-013", "DMT Freebase Cristal", "Alucinógenos", 185_000, 20, "biotech", WarehouseNames.BodegaSur, "DMT freebase cristal, forma alcalina pura.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-DMT-014", "DMT Changa Blend 50/50", "Alucinógenos", 72_000, 35, "spa", WarehouseNames.BodegaSur, "Changa DMT/MAOI 50/50, mezcla fumable por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-THC-073", "Gomitas THC 10mg x5", "Alucinógenos", 26_000, 120, "medication", WarehouseNames.AlmacenNorte, "Pack 5 gomitas THC 10 mg c/u (50 mg total).", SaleMeasureUnit.Unit, unitContentAmount: 50m, unitContentMeasure: SaleMeasureUnit.Milligram),

        P(categoryByName, warehouseByName, "PLZ-THC-074", "Gomitas CBD Relax", "Alucinógenos", 18_000, 100, "medication", WarehouseNames.AlmacenNorte, "Gomitas CBD relax, pack x10 unidades.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-AYA-080", "Ayahuasca", "Alucinógenos", 68_000, 30, "water_drop", WarehouseNames.BodegaSur, "Brebaje ayahuasca (DMT + harmalinas), frasco 250 ml.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-MES-081", "Mescalina San Pedro Cactus", "Alucinógenos", 48_000, 40, "spa", WarehouseNames.BodegaSur, "Mescalina, extracto de cactus San Pedro por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MES-082", "Mescalina Peyote Extracto", "Alucinógenos", 45_000, 35, "spa", WarehouseNames.BodegaSur, "Mescalina, extracto de peyote por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-IBO-082", "Ibogaina", "Alucinógenos", 120_000, 20, "science", WarehouseNames.BodegaSur, "Ibogaina HCl, polvo por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-NBO-083", "NBOMes Blotter 25I", "Alucinógenos", 22_000, 60, "science", WarehouseNames.BodegaSur, "25I-NBOMe, blotter por unidad.", SaleMeasureUnit.Unit),



        // —— Estimulantes ——

        P(categoryByName, warehouseByName, "PLZ-TUS-015", "Tussi Rosa 2C-B", "Estimulantes", 78_000, 80, "medication", WarehouseNames.CentralBogota, "Tussi rosa (2C-B), polvo rosado por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-TUS-016", "Tussi Champagne", "Estimulantes", 88_000, 80, "medication", WarehouseNames.CentralBogota, "Tussi champagne, mezcla 2C-B con MDMA por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MDM-088", "MDMA Cristal Europa", "Estimulantes", 78_000, 200, "diamond", WarehouseNames.CentralBogota, "MDMA cristal, polvo puro por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MDM-089", "MDMA Pastillas Red Bull", "Estimulantes", 30_000, 150, "medication", WarehouseNames.CentralBogota, "Pastillas MDMA selladas Red Bull, por unidad.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-COC-099", "Cocaína Perlada — Polvo", "Estimulantes", 52_000, 50, "grain", WarehouseNames.CentralBogota, "Cocaína perlada HCl, polvo por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-COC-100", "Crack (Cocaína Base)", "Estimulantes", 28_000, 25, "grain", WarehouseNames.CentralBogota, "Crack (cocaína base), piedras por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-COC-101", "Cocaína HCl — Polvo Fino", "Estimulantes", 48_000, 45, "grain", WarehouseNames.CentralBogota, "Cocaína HCl polvo fino, por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-COC-102", "Pasta Base Cocaína", "Estimulantes", 9_000, 80, "grain", WarehouseNames.CentralBogota, "Pasta base de cocaína, precursor por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-EXT-056", "Éxtasis Tesla 300mg", "Estimulantes", 28_000, 120, "medication", WarehouseNames.CentralBogota, "Éxtasis Tesla 300 mg MDMA por pastilla.", SaleMeasureUnit.Unit, unitContentAmount: 300m, unitContentMeasure: SaleMeasureUnit.Milligram),

        P(categoryByName, warehouseByName, "PLZ-EXT-057", "Éxtasis Punisher 280mg", "Estimulantes", 30_000, 100, "medication", WarehouseNames.CentralBogota, "Éxtasis Punisher 280 mg MDMA por pastilla.", SaleMeasureUnit.Unit, unitContentAmount: 280m, unitContentMeasure: SaleMeasureUnit.Milligram),

        P(categoryByName, warehouseByName, "PLZ-EXT-058", "Éxtasis Pink Porsche 250mg", "Estimulantes", 28_000, 110, "medication", WarehouseNames.CentralBogota, "Éxtasis Pink Porsche 250 mg MDMA por pastilla.", SaleMeasureUnit.Unit, unitContentAmount: 250m, unitContentMeasure: SaleMeasureUnit.Milligram),

        P(categoryByName, warehouseByName, "PLZ-BAZ-070", "Bazuco", "Estimulantes", 3_500, 100, "grain", WarehouseNames.CentralBogota, "Bazuco, mezcla de residuos cocaínicos por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-SAL-072", "Sales de baño Arctic Blast", "Estimulantes", 42_000, 80, "science", WarehouseNames.CentralBogota, "Sales de baño (mephedrone/MDPV), sobre 250 g.", SaleMeasureUnit.Unit, unitContentAmount: 250m, unitContentMeasure: SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MET-090", "Metanfetamina Cristalina Ice", "Estimulantes", 92_000, 40, "diamond", WarehouseNames.CentralBogota, "Metanfetamina cristal ice, por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-MET-092", "Metanfetamina Pastillas Yaba", "Estimulantes", 22_000, 90, "medication", WarehouseNames.CentralBogota, "Yaba, pastillas de metanfetamina + cafeína.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-MET-093", "Metanfetamina Pink Ice Cristal", "Estimulantes", 102_000, 35, "diamond", WarehouseNames.CentralBogota, "Metanfetamina pink ice, cristal rosado por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-AMP-091", "Anfetamina Speed Pastillas", "Estimulantes", 18_000, 100, "medication", WarehouseNames.CentralBogota, "Anfetamina (speed), pastillas por unidad.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-KHT-092", "Khat Hojas Frescas", "Estimulantes", 18_000, 60, "eco", WarehouseNames.CentralBogota, "Khat (Catha edulis), hojas frescas por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-COC-093", "Hoja de Coca Tradicional", "Estimulantes", 6_000, 80, "eco", WarehouseNames.CentralBogota, "Hoja de coca seca, masticable por gramo.", SaleMeasureUnit.Gram),



        // —— Inhalantes ——

        P(categoryByName, warehouseByName, "PLZ-POP-007", "Popper Rush XL (Amil)", "Inhalantes", 26_000, 100, "air", WarehouseNames.CentralBogota, "Amil nitrito (poppers), frasco Rush XL.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-POP-008", "Popper Nitrito Isopropílico", "Inhalantes", 22_000, 80, "air", WarehouseNames.CentralBogota, "Isopropil nitrito (poppers), frasco estándar.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-AER-060", "Aerosol Tolueno", "Inhalantes", 8_000, 120, "air", WarehouseNames.AlmacenNorte, "Aerosol solvente tolueno, inhalación.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-SOL-061", "Disolvente Volátil Tolueno", "Inhalantes", 6_500, 100, "science", WarehouseNames.AlmacenNorte, "Disolvente volátil tolueno, frasco.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-GAS-062", "Gas Hilarante N2O x10", "Inhalantes", 35_000, 80, "air", WarehouseNames.CentralBogota, "Óxido nitroso (N2O), pack x10 bulbos.", SaleMeasureUnit.Unit),



        // —— Disociativos ——

        P(categoryByName, warehouseByName, "PLZ-KET-021", "Ketamina Líquida 50ml", "Disociativos", 120_000, 40, "vaccines", WarehouseNames.BodegaSur, "Ketamina HCl líquida, frasco 50 ml.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-KET-022", "Ketamina Polvo Cristalino S", "Disociativos", 68_000, 50, "science", WarehouseNames.BodegaSur, "Ketamina polvo cristalino S(+), por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-KET-023", "Ketamina Spray Nasal Esketamina", "Disociativos", 72_000, 60, "medication", WarehouseNames.BodegaSur, "Esketamina spray nasal, frasco unitario.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-PCP-022", "PCP (Fenciclidina)", "Disociativos", 52_000, 30, "science", WarehouseNames.BodegaSur, "PCP (fenciclidina), polvo por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-PCP-024", "PCP Líquido Angel Dust", "Disociativos", 45_000, 25, "water_drop", WarehouseNames.BodegaSur, "PCP líquido (angel dust), frasco.", SaleMeasureUnit.Unit),



        // —— Depresores ——

        P(categoryByName, warehouseByName, "PLZ-HER-071", "Heroína", "Depresores", 105_000, 40, "medication", WarehouseNames.BodegaSur, "Heroína (diacetilmorfina), polvo por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-HER-072", "Heroína Black Tar", "Depresores", 92_000, 35, "medication", WarehouseNames.BodegaSur, "Heroína black tar, opio negro pegajoso por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-HER-073", "Heroína #4 Polvo Blanco", "Depresores", 115_000, 30, "medication", WarehouseNames.BodegaSur, "Heroína #4 polvo blanco, por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-GHB-075", "GHB", "Depresores", 38_000, 50, "water_drop", WarehouseNames.BodegaSur, "GHB (ácido γ-hidroxibutírico), frasco 30 ml.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-BAR-076", "Secobarbital", "Depresores", 22_000, 60, "medication", WarehouseNames.BodegaSur, "Secobarbital, barbitúrico sedante por comprimido.", SaleMeasureUnit.Unit),



        // —— Opioides ——

        P(categoryByName, warehouseByName, "PLZ-OPI-001", "Fentanilo Parche Transdérmico", "Opioides", 145_000, 30, "medication", WarehouseNames.BodegaSur, "Parche transdérmico de fentanilo.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-OPI-002", "Codeína Jarabe", "Opioides", 28_000, 80, "medication", WarehouseNames.BodegaSur, "Jarabe con codeína fosfato, frasco.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-OPI-003", "Morfina Ampolla", "Opioides", 72_000, 40, "vaccines", WarehouseNames.BodegaSur, "Morfina sulfato, ampolla inyectable.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-OPI-004", "Metadona Jarabe", "Opioides", 32_000, 50, "medication", WarehouseNames.BodegaSur, "Jarabe de metadona, frasco.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-OPI-005", "Hidromorfona Comprimidos", "Opioides", 55_000, 40, "medication", WarehouseNames.BodegaSur, "Hidromorfona, comprimidos por unidad.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-OPI-006", "Meperidina Ampollas", "Opioides", 42_000, 35, "medication", WarehouseNames.BodegaSur, "Meperidina (Demerol), ampollas por unidad.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-OPI-007", "Oxycodona OxyContin 40mg", "Opioides", 95_000, 25, "medication", WarehouseNames.BodegaSur, "Oxycodona OxyContin 40 mg, comprimido.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-OPI-008", "Tramadol 50mg Caja x20", "Opioides", 24_000, 70, "medication", WarehouseNames.BodegaSur, "Tramadol 50 mg, caja x20 comprimidos.", SaleMeasureUnit.Unit),



        // —— Cannabis ——

        P(categoryByName, warehouseByName, "PLZ-CAN-001", "Delta-8 THC Gummies x10", "Cannabis", 28_000, 100, "medication", WarehouseNames.AlmacenNorte, "Gomitas delta-8 THC, pack x10.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-CAN-002", "Flores Cannabis Outdoor", "Cannabis", 12_000, 150, "eco", WarehouseNames.AlmacenNorte, "Flores cannabis outdoor, cogollo por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-CAN-003", "Cannabis Autoflower Northern Lights", "Cannabis", 18_000, 120, "eco", WarehouseNames.AlmacenNorte, "Northern Lights autoflower, flor por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-CAN-004", "Resina Live Rosin THC", "Cannabis", 55_000, 40, "grain", WarehouseNames.AlmacenNorte, "Live rosin THC, concentrado por gramo.", SaleMeasureUnit.Gram),



        // —— Cannabinoides sintéticos ——

        P(categoryByName, warehouseByName, "PLZ-K2-001", "K2 Spice Incense", "Cannabinoides sintéticos", 18_000, 90, "air", WarehouseNames.CentralBogota, "K2/Spice, incienso sintético JWH.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-K2-002", "K2 Liquid Vape Cartucho", "Cannabinoides sintéticos", 32_000, 70, "smoking_rooms", WarehouseNames.CentralBogota, "K2 líquido, cartucho vape sintético.", SaleMeasureUnit.Unit),



        // —— Drogas sintéticas ——

        P(categoryByName, warehouseByName, "PLZ-SYN-001", "Flakka (Alpha-PVP)", "Drogas sintéticas", 52_000, 45, "science", WarehouseNames.CentralBogota, "Flakka (α-PVP), estimulante sintético por gramo.", SaleMeasureUnit.Gram),

        P(categoryByName, warehouseByName, "PLZ-SYN-002", "Catinonas (Mephedrone)", "Drogas sintéticas", 45_000, 55, "science", WarehouseNames.CentralBogota, "Mephedrone (4-MMC), catinona sintética, sobre 200 g.", SaleMeasureUnit.Unit, unitContentAmount: 200m, unitContentMeasure: SaleMeasureUnit.Gram),



        // —— Nicotina ——

        P(categoryByName, warehouseByName, "PLZ-NIC-001", "Vape Desechable Mango Ice", "Nicotina", 18_000, 200, "smoking_rooms", WarehouseNames.CentralBogota, "Vape desechable nicotina mango ice.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-NIC-002", "Pod Nicotina 5% Salt", "Nicotina", 28_000, 150, "smoking_rooms", WarehouseNames.CentralBogota, "Pod nicotina salts 5%, cartucho recargable.", SaleMeasureUnit.Unit),



        // —— Esteroides anabólicos ——

        P(categoryByName, warehouseByName, "PLZ-STR-001", "Testosterona Enantato 250mg/ml", "Esteroides anabólicos", 95_000, 40, "fitness_center", WarehouseNames.AlmacenNorte, "Testosterona enantato 250 mg/ml, frasco 10 ml.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-STR-002", "Nandrolona Decanoato (Deca)", "Esteroides anabólicos", 110_000, 30, "fitness_center", WarehouseNames.AlmacenNorte, "Nandrolona decanoato (Deca), frasco inyectable.", SaleMeasureUnit.Unit),



        // —— Medicamentos con prescripción ——

        P(categoryByName, warehouseByName, "PLZ-RX-001", "Metilfenidato 10mg Caja", "Medicamentos con prescripción", 38_000, 80, "medication", WarehouseNames.CentralBogota, "Metilfenidato (Ritalin) 10 mg, caja x30.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-RX-002", "Hidrocodona Compuesto", "Medicamentos con prescripción", 58_000, 40, "medication", WarehouseNames.CentralBogota, "Hidrocodona + paracetamol, comprimido.", SaleMeasureUnit.Unit),



        // —— Benzodiacepinas ——

        P(categoryByName, warehouseByName, "PLZ-BNZ-001", "Flunitrazepam (Rohypnol)", "Benzodiacepinas", 28_000, 50, "medication", WarehouseNames.BodegaSur, "Flunitrazepam (Rohypnol) 1 mg, comprimido.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-BNZ-002", "Clonazepam 2mg", "Benzodiacepinas", 15_000, 70, "medication", WarehouseNames.BodegaSur, "Clonazepam 2 mg, comprimido.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-BNZ-003", "Diazepam 10mg Valium", "Benzodiacepinas", 12_000, 80, "medication", WarehouseNames.BodegaSur, "Diazepam (Valium) 10 mg, comprimido.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-BNZ-004", "Alprazolam 2mg Xanax", "Benzodiacepinas", 18_000, 75, "medication", WarehouseNames.BodegaSur, "Alprazolam (Xanax) 2 mg, comprimido.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-BNZ-005", "Lorazepam Ativan 2mg", "Benzodiacepinas", 14_000, 65, "medication", WarehouseNames.BodegaSur, "Lorazepam (Ativan) 2 mg, comprimido.", SaleMeasureUnit.Unit),



        // —— Antitusivos (DXM, loperamida) ——

        P(categoryByName, warehouseByName, "PLZ-OTC-001", "Dextrometorfano (DXM) Jarabe", "Antitusivos", 12_000, 100, "medication", WarehouseNames.AlmacenNorte, "Dextrometorfano (DXM) jarabe, antitusivo.", SaleMeasureUnit.Unit),

        P(categoryByName, warehouseByName, "PLZ-OTC-002", "Loperamida Imodium Pack x12", "Antitusivos", 10_000, 120, "medication", WarehouseNames.AlmacenNorte, "Loperamida (Imodium), pack x12 cápsulas.", SaleMeasureUnit.Unit),

    ];



    internal static List<Product> Create(
        Dictionary<string, Category> categoryByName,
        Dictionary<string, Warehouse> warehouseByName) =>
        GetAll(categoryByName, warehouseByName);

    /// <summary>Valid PLZ-* codes from seed definitions (does not require DB categories).</summary>
    internal static HashSet<string> ValidSeedProductCodes()
    {
        var categories = CategorySeedData.Create().ToDictionary(c => c.Name);
        var warehouses = WarehouseSeedData.Create().ToDictionary(w => w.Name);
        return GetAll(categories, warehouses)
            .Select(p => p.Code.ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    private static Dictionary<string, Category> StubCategories() =>
        CategorySeedData.Create().ToDictionary(c => c.Name);

    private static Dictionary<string, Warehouse> StubWarehouses() =>
        WarehouseSeedData.Create().ToDictionary(w => w.Name);

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

        var category = ResolveCategory(categoryByName, categoryName);
        var warehouse = ResolveWarehouse(warehouseByName, warehouseName);

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

    private static Category ResolveCategory(Dictionary<string, Category> categoryByName, string categoryName)
    {
        if (categoryByName.TryGetValue(categoryName, out var category))
        {
            return category;
        }

        foreach (var (name, value) in categoryByName)
        {
            if (string.Equals(name, categoryName, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        throw new InvalidOperationException(
            $"Categoría de seed «{categoryName}» no encontrada. " +
            $"Disponibles en BD: [{string.Join(", ", categoryByName.Keys)}]. " +
            "Reinicia la API; si persiste, ejecuta «docker compose down -v && docker compose up -d» desde Backend/.");
    }

    private static Warehouse ResolveWarehouse(Dictionary<string, Warehouse> warehouseByName, string warehouseName)
    {
        if (warehouseByName.TryGetValue(warehouseName, out var warehouse))
        {
            return warehouse;
        }

        foreach (var (name, value) in warehouseByName)
        {
            if (string.Equals(name, warehouseName, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        throw new InvalidOperationException(
            $"Almacén de seed «{warehouseName}» no encontrado. " +
            $"Disponibles en BD: [{string.Join(", ", warehouseByName.Keys)}].");
    }

}
