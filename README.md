# Bağlantı Portalı

ASP.NET Core tabanlı oda sinyalleşme sunucusu ve tarayıcılar arasında gerçek
eşler arası görüntülü görüşme sağlayan WebRTC uygulaması.

## Öne çıkanlar

- `RTCPeerConnection` ile doğrudan ses ve görüntü aktarımı
- SDP offer/answer ve ICE candidate sinyalleşmesi
- İki kişilik, adı doğrulanan odalar
- SIPSorcery ile sunucu tarafında SDP ayrıştırma ve doğrulama
- WebSocket mesaj boyutu ve sohbet uzunluğu sınırları
- Kamera ve mikrofon açma/kapatma kontrolleri
- Mobil ekranlara uyumlu arayüz
- `/health` sağlık kontrolü

> Eski WinForms istemcisi `RTCform/` altında eğitim geçmişi için korunur. Bu
> istemci JPEG karelerini WebSocket üzerinden taşıdığı için gerçek WebRTC
> değildir ve varsayılan demo akışına dahil edilmez.

## Teknolojiler

- .NET 8 ve ASP.NET Core
- SIPSorcery 10
- WebSocket sinyalleşmesi
- WebRTC, SDP, ICE ve STUN
- HTML, CSS ve modern JavaScript

## Çalıştırma

Windows'ta `başlat.bat` dosyasını çalıştırın veya terminalde:

```powershell
dotnet restore .\Web\WebRTCSignalingServer.csproj
dotnet run --project .\Web\WebRTCSignalingServer.csproj
```

Ardından [http://localhost:8080](http://localhost:8080) adresini iki farklı
tarayıcıda açın. İki tarafta da aynı oda adını girerek görüşmeye katılın.

## LAN testi

Sunucu `0.0.0.0:8080` üzerinde dinler. Aynı ağdaki başka bir cihazdan
`http://BILGISAYAR_IP:8080` adresi kullanılabilir. Tarayıcıların kamera ve
mikrofon erişimi için güvenli bağlam kuralları uyguladığını unutmayın; mobil
cihazlarda üretim testi için HTTPS tercih edilmelidir.

## Mimari

```text
Tarayıcı A ─── WebSocket sinyalleşmesi ─┐
                                        ├── ASP.NET Core + SIPSorcery
Tarayıcı B ─── WebSocket sinyalleşmesi ─┘

Tarayıcı A ═══════ WebRTC medya ═══════ Tarayıcı B
```

Sunucu medya paketlerini taşımaz. Sadece oda üyeliği, SDP ve ICE mesajlarını
iletir. SDP içeriği SIPSorcery ile ayrıştırılarak hatalı sinyalleşme mesajları
reddedilir.

## Güvenlik notları

- Oda adı tahmin edilemeyecek şekilde seçilmelidir.
- Bu proje eğitim ve portföy demosudur; kullanıcı doğrulaması içermez.
- İnternete açılacak dağıtımda HTTPS/WSS, TURN ve kimlik doğrulaması eklenmelidir.
