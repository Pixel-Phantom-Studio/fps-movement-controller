# Pixel Phantom — FPS Movement Controller

Unity için hafif, harici paketlere bağımlı olmayan, New Input System tabanlı birinci şahıs (FPS) karakter kontrolcüsü ve adım ses motoru.

Pixel Phantom Studio projeleri (Phantom Prophecy, horo vb.) arasında ortak kullanım için UPM (Unity Package Manager) paketi olarak tasarlanmıştır.

---

## ⚡ Özellikler

* **İvme & Ağırlık Hissi:** Doğrudan sabit hız yerine pürüzsüz ivmelenme (`acceleration: 12 m/s²`) ve yavaşlama (`deceleration: 16 m/s²`) ile karaktere gerçekçi bir fizik/ağırlık hissi verir.
* **Tavan Algılamalı Eğilme (Crouch):** Eğildikten sonra kalkarken başının üstünde engel (tavan, masa vb.) varsa ayağa kalkmayı engeller (`SphereCast` tavan kontrolü).
* **Doğal Kafa Sallantısı (Procedural Head Bob):** Sabit animasyon yerine adım fazına ($\pi$) bağlı trigonometrik kafa sallanması. Durunca sönümlenir, koşunca genliği artar, duvara doğru yürürken çalışmaz.
* **Dinamik Adım Sesleri (Footsteps):** Kafa sallantısının en alt tepe noktasında tetiklenen adım event'i (`Stepped`). Yürürken, koşarken veya eğilince otomatik olarak ses seviyesini (`volume`) ve tonunu (`pitch`) dinamik olarak ayarlar.
* **Sıfır Bağımlılık:** Harici kütüphanelere (DOTween vb.) ihtiyaç duymaz. Sadece saf Unity matematiği ve Unity New Input System kullanır.

---

## 📦 Kurulum (Git URL ile)

1. Unity projenizde üst menüden **Window > Package Manager** penceresini açın.
2. Sol üstteki **`+`** simgesine tıklayın ve **"Add package from git URL..."** seçeneğini seçin.
3. Aşağıdaki repo URL'sini yapıştırıp **Add**'e basın:

```text
https://github.com/Pixel-Phantom-Studio/fps-movement-controller.git
```

> **Önemli Gereksinim:** Projenizde **Input System** kurulu olmalıdır (`com.unity.inputsystem`).  
> *Project Settings > Player > Other Settings > Active Input Handling* seçeneğinin **"Input System Package (New)"** veya **"Both"** olduğundan emin olun.

---

## 🚀 1 Dakikalık Hızlı Kurulum

### Yöntem A: Hazır Prefab ile (En Kolay)
1. Paketi ekledikten sonra `Packages/Pixel Phantom FPS Movement Controller/Runtime/Prefabs/Player_Rig.prefab` dosyasını sahnenize sürükleyin.
2. Sahneyi başlatın.

### Yöntem B: Sıfırdan Boş Obje ile
1. Sahnede bir **Capsule** (veya boş GameObject) oluşturun, adını `Player` yapın.
2. Objenin üzerine şu bileşenleri ekleyin:
   - `CharacterController`
   - `PlayerController`
   - `Footsteps` *(isteğe bağlı)*
   - `AudioSource` *(ayak sesleri için)*
3. `Player` objesinin altına bir **Camera** ekleyin (`Main Camera`).
4. `PlayerController` bileşenindeki **Camera** alanına bu kamerayı sürükleyin.
5. İsterseniz `Footsteps` bileşenindeki **Clips** dizisine paketteki `Runtime/Audio/Footsteps/` seslerini atayın.

---

## 🎮 Varsayılan Tuş Atamaları (PlayerInput)

| Eylem | Tuş |
| :--- | :--- |
| **Yürüme (Move)** | W, A, S, D |
| **Bakış (Look)** | Fare Hareketi |
| **Koşma (Sprint)** | Sol Shift |
| **Eğilme (Crouch)** | Sol Ctrl (veya C) |

---

## ⚙️ Inspector Ayarları Referansı

### `PlayerController`
* `_moveSpeed` (Varsayılan: 2.8): Normal yürüme hızı (m/s).
* `_sprintSpeed` (Varsayılan: 5.0): Koşma hızı (m/s).
* `_crouchMoveSpeed` (Varsayılan: 1.4): Eğilerek yürüme hızı (m/s).
* `_acceleration` (Varsayılan: 12): Tam hıza ulaşma ivmesi.
* `_deceleration` (Varsayılan: 16): Durma yavaşlaması.
* `_sensitivity` (Varsayılan: 0.5): Fare hassasiyeti.
* `_crouchHeight` (Varsayılan: 1.0): Eğilince kapsül yüksekliği.
* `_ceilingMask`: Tavan kontrolünde dikkate alınacak Layer'lar (örn. `Default`).
* `_headBob` (Varsayılan: true): Kafa sallantısını açar/kapatır.

### `Footsteps`
* `_clips`: Rastgele çalınacak ayak sesi `AudioClip` dizisi (üst üste aynı sesi çalmaz).
* `_minPitch` / `_maxPitch`: Ses tonu varyasyonu (doğallık için 0.9 - 1.1 arası rastgele seçilir).
* `_crouchVolume`: Eğilerek yürürken sesin kısılma oranı (daha sessiz ve pes).

---

## 🛠️ Mimari ve Katmanlar

* **`InputService`:** Unity New Input System `PlayerInput` nesnesini lazy-init olarak yönetir. Sahneye koyulmasa bile ilk erişimde kendini otomatik başlatır ve sahne geçişlerinde hayatta kalır (`DontDestroyOnLoad`).
* **`PlayerController`:** Sadece girdiyi (`InputService.Actions`) okur ve `CharacterController`'a uygular. Başka oyun sistemlerini bilmez.
* **`Footsteps`:** `PlayerController.Stepped` C# event'ine abonedir. Karakter hareketinden tamamen bağımsız çalışır; sadece adım tetiklendiğinde ses çalar.
