# WorkerCollector

Service untuk mengumpulkan dan mendistribusikan data GPS secara real-time menggunakan Redis dan SignalR.

## Persyaratan

- .NET 8.0 SDK
- Redis Server (port 6381)
- Visual Studio 2022 atau VS Code

## Konfigurasi

1. Pastikan Redis server berjalan di port 6381
2. Sesuaikan konfigurasi di `appsettings.json` jika diperlukan:
   ```json
   {
     "Redis": {
       "ConnectionString": "localhost:6381",
       "Channel": "gps_update"
     },
     "SignalR": {
       "HubPath": "/gpsHub"
     }
   }
   ```

## Cara Menjalankan

1. Clone repository
2. Buka terminal di folder project
3. Jalankan perintah:
   ```
   dotnet run
   ```

## Cara Menggunakan

### Mengirim Data GPS

Kirim data GPS ke Redis menggunakan format JSON:
```json
{
  "UnitNo": "NOMOR_UNIT",
  "Lat": -6.123456,
  "Lon": 106.123456
}
```

Contoh menggunakan Redis CLI:
```
PUBLISH gps_update '{"UnitNo":"UNIT001","Lat":-6.123456,"Lon":106.123456}'
```

### Menerima Data GPS

Client dapat terhubung ke WebSocket menggunakan endpoint:
```
ws://localhost:5000/gpsHub
```

Data akan dikirim setiap 1 detik dalam format array JSON.

## Monitoring

- Log aplikasi dapat dilihat di console
- Koneksi WebSocket yang aktif akan di-log
- Data yang dikirim ke client akan di-log

## Troubleshooting

1. Jika Redis tidak terkoneksi:
   - Pastikan Redis server berjalan
   - Periksa port Redis di konfigurasi
   - Periksa firewall settings

2. Jika WebSocket tidak terkoneksi:
   - Pastikan service berjalan
   - Periksa endpoint WebSocket
   - Periksa CORS settings jika diperlukan 