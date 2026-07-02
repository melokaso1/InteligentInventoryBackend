using Domain.Constants;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Persistence.Seed;

internal static class ProductSeedData
{
    internal static List<Product> Create(
        Dictionary<string, Category> categoryByName,
        Dictionary<string, Warehouse> warehouseByName) =>
    [
        P(categoryByName, warehouseByName, "PLZ-LAP-001", "Laptop Dell Latitude 5540", "Electrónica", 4_250_000, 24, 40, "laptop", WarehouseNames.CentralBogota, "Laptop empresarial Intel Core i7, 16 GB RAM, 512 GB SSD."),
        P(categoryByName, warehouseByName, "PLZ-LAP-002", "Laptop Lenovo ThinkPad E14", "Electrónica", 3_890_000, 18, 35, "laptop", WarehouseNames.CentralBogota, "Portátil robusto para oficina con pantalla 14\" FHD."),
        P(categoryByName, warehouseByName, "PLZ-MON-001", "Monitor LG 27\" 4K UltraFine", "Electrónica", 1_450_000, 32, 50, "monitor", WarehouseNames.CentralBogota, "Monitor IPS 4K con soporte ajustable."),
        P(categoryByName, warehouseByName, "PLZ-MON-002", "Monitor Samsung 24\" FHD", "Electrónica", 680_000, 45, 60, "monitor", WarehouseNames.AlmacenNorte, "Monitor LED Full HD para estaciones de trabajo."),
        P(categoryByName, warehouseByName, "PLZ-KBD-001", "Teclado mecánico Logitech MX", "Periféricos", 420_000, 56, 80, "keyboard", WarehouseNames.CentralBogota, "Teclado inalámbrico mecánico con retroiluminación."),
        P(categoryByName, warehouseByName, "PLZ-MSE-001", "Mouse inalámbrico Logitech MX Master", "Periféricos", 380_000, 72, 100, "mouse", WarehouseNames.CentralBogota, "Mouse ergonómico multipuerto para productividad."),
        P(categoryByName, warehouseByName, "PLZ-CBL-001", "Cable HDMI 2.1 — 2 metros", "Accesorios", 45_000, 120, 200, "cable", WarehouseNames.CentralBogota, "Cable HDMI de alta velocidad para monitores 4K."),
        P(categoryByName, warehouseByName, "PLZ-CBL-002", "Cable USB-C a USB-C 1.5 m", "Accesorios", 32_000, 95, 150, "cable", WarehouseNames.AlmacenNorte, "Cable de carga y datos USB-C certificado."),
        P(categoryByName, warehouseByName, "PLZ-HDD-001", "Disco SSD Samsung 1 TB NVMe", "Almacenamiento", 380_000, 40, 60, "sd_card", WarehouseNames.CentralBogota, "Unidad NVMe M.2 de alto rendimiento."),
        P(categoryByName, warehouseByName, "PLZ-HDD-002", "Disco SSD Kingston 500 GB SATA", "Almacenamiento", 185_000, 12, 50, "sd_card", WarehouseNames.AlmacenNorte, "SSD SATA III para equipos de oficina."),
        P(categoryByName, warehouseByName, "PLZ-CHR-001", "Silla ergonómica de oficina", "Mobiliario", 890_000, 15, 25, "chair", WarehouseNames.BodegaSur, "Silla con soporte lumbar y reposabrazos ajustables."),
        P(categoryByName, warehouseByName, "PLZ-CHR-002", "Silla ejecutiva de cuero", "Mobiliario", 1_250_000, 8, 20, "chair", WarehouseNames.BodegaSur, "Silla ejecutiva tapizada en cuero sintético."),
        P(categoryByName, warehouseByName, "PLZ-DSK-001", "Escritorio ajustable 140 cm", "Mobiliario", 1_680_000, 12, 18, "desk", WarehouseNames.BodegaSur, "Escritorio regulable en altura con estructura metálica."),
        P(categoryByName, warehouseByName, "PLZ-PRN-001", "Impresora HP LaserJet Pro", "Oficina", 1_120_000, 10, 20, "print", WarehouseNames.CentralBogota, "Impresora láser monocromática para equipos de trabajo."),
        P(categoryByName, warehouseByName, "PLZ-PRN-002", "Tóner HP 85A negro", "Consumibles", 185_000, 35, 60, "toner", WarehouseNames.CentralBogota, "Cartucho de tóner original compatible con LaserJet Pro."),
        P(categoryByName, warehouseByName, "PLZ-PAP-001", "Resma papel carta 500 hojas", "Consumibles", 28_500, 200, 300, "description", WarehouseNames.AlmacenNorte, "Papel bond tamaño carta, 75 g/m²."),
        P(categoryByName, warehouseByName, "PLZ-WEB-001", "Cámara web Logitech C920", "Periféricos", 320_000, 22, 40, "videocam", WarehouseNames.CentralBogota, "Webcam Full HD con micrófono estéreo integrado."),
        P(categoryByName, warehouseByName, "PLZ-HUB-001", "Hub USB-C 7 puertos", "Accesorios", 125_000, 18, 35, "usb", WarehouseNames.AlmacenNorte, "Concentrador USB-C con HDMI y lector SD."),
        P(categoryByName, warehouseByName, "PLZ-UPS-001", "UPS APC 1500 VA", "Electrónica", 1_580_000, 6, 15, "battery_charging_full", WarehouseNames.BodegaSur, "Sistema de respaldo de energía para servidores y equipos críticos."),
        P(categoryByName, warehouseByName, "PLZ-EXT-001", "Extensor WiFi TP-Link RE450", "Redes", 245_000, 0, 30, "router", WarehouseNames.AlmacenNorte, "Repetidor WiFi AC1750 de doble banda.", ProductStatus.OutOfStock),
    ];

    private static Product P(
        Dictionary<string, Category> categoryByName,
        Dictionary<string, Warehouse> warehouseByName,
        string code, string name, string categoryName, decimal price, int stock, int maxStock,
        string icon, string warehouseName, string description,
        ProductStatus status = ProductStatus.Active)
    {
        var category = categoryByName[categoryName];
        var warehouse = warehouseByName[warehouseName];
        return new Product
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            CategoryId = category.Id,
            Category = category,
            Price = price,
            Status = status,
            Icon = icon,
            Description = description,
            Inventories =
            [
                new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouse.Id,
                    Warehouse = warehouse,
                    CurrentStock = stock,
                    MinStock = Math.Max(1, maxStock / 4),
                    MaxStock = maxStock,
                },
            ],
        };
    }
}
