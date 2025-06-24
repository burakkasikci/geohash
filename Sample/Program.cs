using Geohash;

namespace Sample;

public class VehicleService
{
    public class Driver
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public string Geohash { get; set; } = "";
    }

    // Basit bir sürücü listesi (Gerçek uygulamada veritabanından gelir)
    private readonly List<Driver> _availableDrivers;
    private readonly Geohasher geoHasher = new();

    public VehicleService()
    {
        // Örnek sürücüler
        _availableDrivers =
        [
            new Driver { Id = 1, Name = "Ahmet", Latitude = 41.0082, Longitude = 28.9784 }, // Ayasofya civarı
            new Driver { Id = 2, Name = "Mehmet", Latitude = 41.0090, Longitude = 28.9800 }, // Yakın
            new Driver { Id = 3, Name = "Ayşe", Latitude = 41.0200, Longitude = 29.0000 }, // Biraz uzak
            new Driver { Id = 4, Name = "Fatma", Latitude = 40.9950, Longitude = 28.9600 }, // Daha uzak
            new Driver { Id = 5, Name = "Can", Latitude = 41.0085, Longitude = 28.9788 }
        ];

        // Sürücülerin GeoHash'lerini hesapla (Genellikle sürücü konumu güncellendiğinde yapılır)
        foreach (var driver in _availableDrivers)
            driver.Geohash = geoHasher.Encode(driver.Latitude, driver.Longitude, 7);
    }

    /// <summary>
    /// Belirtilen konuma yakın sürücüleri bulur.
    /// </summary>
    /// <param name="customerLatitude">Kullanıcınin enlemi.</param>
    /// <param name="customerLongitude">Kullanıcınin boylamı.</param>
    /// <param name="precision">Aranacak GeoHash hassasiyeti (daha yüksek sayı daha küçük alan).</param>
    /// <returns>Yakın sürücülerin listesi.</returns>
    public List<Driver> FindNearbyDrivers(double customerLatitude, double customerLongitude, int precision)
    {
        // 1. Kullanıcınin konumunun GeoHash'ini al
        var customerGeohash = geoHasher.Encode(customerLatitude, customerLongitude, precision);
        Console.WriteLine($"Kullanıcınin GeoHash'i (Precision {precision}): {customerGeohash}");

        // 2. Bu GeoHash'in komşularını al (kendi GeoHash'i dahil)
        // GetNeighbors() metodunu kullanarak 8 ana yöndeki komşuları alabiliriz.
        var nearbyGeohashes = geoHasher.GetNeighbors(customerGeohash).ToList();
        if (nearbyGeohashes.Count == 0) return [];

        // Kendi alanındakileri de kaçırmamak için GeoHash'ini de listeye eklemeyi unutmayın!
        var nearbyGeohashesKeys = nearbyGeohashes.Select(t => t.Value).ToList();
        nearbyGeohashesKeys.Add(customerGeohash);

        Console.WriteLine("Aranacak GeoHash'ler:");
        foreach (var gh in nearbyGeohashes)
            Console.WriteLine($"- {gh}");

        // 3. Veritabanından (veya bu örnekte listemizden) bu GeoHash'lere sahip taksicileri filtrele
        var nearbyDrivers = _availableDrivers
            .Where(driver => nearbyGeohashesKeys.Contains(driver.Geohash[..precision]))
            .ToList();

        // Not: Burada Contains() kullanıyoruz, ancak gerçek bir veritabanı sorgusunda
        // genellikle `WHERE Geohash LIKE 'KullanıcıGeoHash%' OR Geohash LIKE 'komşuGeoHash1%' ...`
        // veya GeoHash önekine göre indekslenmiş sorgular kullanılır.
        // Veya daha iyisi, bir "Bounding Box" sorgusu oluşturup, sadece GeoHash önekiyle
        // başlayanları seçtikten sonra, enlem/boylam üzerinden nihai mesafe kontrolü yapabilirsiniz.

        return nearbyDrivers;
    }

    /// <summary>
    /// İki coğrafi nokta arasındaki mesafeyi (metre cinsinden) Haversine formülü kullanarak hesaplar.
    /// </summary>
    /// <param name="latitude1">Birinci noktanın enlemi (derece).</param>
    /// <param name="longitude1">Birinci noktanın boylamı (derece).</param>
    /// <param name="latitude2">İkinci noktanın enlemi (derece).</param>
    /// <param name="longitude2">İkinci noktanın boylamı (derece).</param>
    /// <returns>İki nokta arasındaki mesafe metre cinsinden.</returns>
    public static double CalculateDistance(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        const double EarthRadiusMeters = 6371e3; // Dünya'nın ortalama yarıçapı metre cinsinden

        // Enlemleri ve boylamları radyana çevir
        var latitude1Rad = ToRadians(latitude1);
        var latitude2Rad = ToRadians(latitude2);
        var deltaLatitudeRad = ToRadians(latitude2 - latitude1);
        var deltaLongitudeRad = ToRadians(longitude2 - longitude1);

        // Haversine formülünün parçaları
        var a = Math.Sin(deltaLatitudeRad / 2) * Math.Sin(deltaLatitudeRad / 2) +
                Math.Cos(latitude1Rad) * Math.Cos(latitude2Rad) *
                Math.Sin(deltaLongitudeRad / 2) * Math.Sin(deltaLongitudeRad / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        // Mesafeyi hesapla
        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Derece cinsinden bir açıyı radyana çevirir.
    /// </summary>
    /// <param name="degrees">Derece cinsinden açı.</param>
    /// <returns>Radyan cinsinden açı.</returns>
    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var vehicleService = new VehicleService();

        // Kullanıcınin konumu (İstanbul, Ayasofya civarı)
        const double customerLat = 41.0083;
        const double customerLon = 28.9780;

        // Yakın sürücüleri 7 hassasiyetinde bulalım. (GeoHash hassasiyeti 7 = yaklaşık 150m x 150m kare bir alan)
        // Daha düşük sayı daha geniş alan, daha yüksek sayı daha küçük alan. Örneğin 6: ~600m x 600m, 5: ~2.4km x 2.4km
        const int precision = 7;

        Console.WriteLine($"Kullanıcı konumu: ({customerLat}, {customerLon})\n");

        var nearbyDrivers = vehicleService.FindNearbyDrivers(customerLat, customerLon, precision);

        VehicleService.Driver? nearestVehicle = null;
        var minDistance = double.MaxValue;
        Console.WriteLine("\nBulunan Yakın Sürücüler:");
        if (nearbyDrivers.Count != 0)
        {
            foreach (var driver in nearbyDrivers)
            {
                var distance = VehicleService.CalculateDistance(customerLat, customerLon, driver.Latitude, driver.Longitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestVehicle = driver;
                }

                Console.WriteLine($"- {driver.Name} (ID: {driver.Id}, Konum: {driver.Latitude}, {driver.Longitude}, GeoHash: {driver.Geohash}, Mesafe: {distance:F2} metre)");
            }

            Console.WriteLine("---------------------------");
            Console.WriteLine($"En yakın sürücü - {nearestVehicle!.Name} (ID: {nearestVehicle.Id}, Konum: {nearestVehicle.Latitude}, {nearestVehicle.Longitude}, GeoHash: {nearestVehicle.Geohash}, Mesafe: {minDistance:F2} metre)");
            Console.WriteLine("---------------------------");
        }
        else
        {
            Console.WriteLine("Yakınlarda uygun sürücü bulunamadı.");
        }
    }
}