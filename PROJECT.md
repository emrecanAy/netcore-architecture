# JetZen — Mimari Referans & Öğretici (PROJECT.md)

> Bu doküman JetZen backend + frontend çözümünün **mimari yapısını, katmanlarını,
> teknolojilerini, DDD/CQRS/Domain Events desenlerini ve best-practice'lerini** belgeler.
> İş alanı (charter/aviation) önemli değil — amaç **yeni bir projede bu yapıyı çoğaltabilmek**.
>
> Anlatım Türkçe, mimari terimler İngilizce (parantezli). Her bölümde *ne / neden / nasıl +
> kod örneği + bu projede nerede* verilir. Sonda **iyileştirme önerileri** ayrı bölümde.
>
> ⚠️ Bu doküman **hiçbir gerçek secret/connection-string/anahtar** içermez; yalnızca yapısal
> referanslar (örn. "değer KeyVault'tan gelir") kullanır.

---

## İçindekiler

1. [Giriş & nasıl okunur](#1-giriş--nasıl-okunur)
2. [Üst seviye mimari](#2-üst-seviye-mimari)
3. [Solution & proje sorumlulukları](#3-solution--proje-sorumlulukları)
4. [Teknoloji yığını (tech stack)](#4-teknoloji-yığını-tech-stack)
5. [DDD katmanı (derin)](#5-ddd-katmanı-derin)
6. [Domain Events (derin)](#6-domain-events-derin)
7. [CQRS + MediatR pipeline (derin)](#7-cqrs--mediatr-pipeline-derin)
8. [Persistence (kalıcılık katmanı)](#8-persistence-kalıcılık-katmanı)
9. [Composition root & Dependency Injection](#9-composition-root--dependency-injection)
10. [Servisler & dış entegrasyonlar](#10-servisler--dış-entegrasyonlar)
11. [API katmanı (Azure Functions)](#11-api-katmanı-azure-functions)
12. [Frontend topolojisi](#12-frontend-topolojisi)
13. [DevOps & runtime](#13-devops--runtime)
14. [Best-practice envanteri (bu projenin doğru yaptıkları)](#14-best-practice-envanteri)
15. [İyileştirme önerileri (öğretici)](#15-iyileştirme-önerileri-öğretici)
16. [Yeni projede uygulama reçetesi](#16-yeni-projede-uygulama-reçetesi)

---

## 1. Giriş & nasıl okunur

JetZen; **katmanlı mimari (layered)** + **CQRS** + **taktiksel DDD (Domain-Driven Design)**
desenlerini bir arada kullanan, **Azure Functions (isolated worker)** üzerinde koşan bir .NET 8
backend'i ile 4 ayrı frontend uygulamasından (2 Angular web + 2 React Native mobil) oluşur.

Çekirdek fikir tek cümlede:

> **İş mantığı (business logic) domain entity'lerin içinde yaşar; uygulama katmanı (Commands/Queries)
> sadece bir senaryoyu orkestre eder; yan etkiler (side-effect) domain event'ler üzerinden tetiklenir;
> kalıcılık ve event yayını tek bir pipeline behavior'da merkezîleştirilir.**

Dokümanı okurken her bölümde şu üçlüyü arayın: **ne** (kavram), **neden** (hangi problemi çözüyor),
**nasıl** (bu repoda hangi dosyada, hangi kodla). Yeni projeye taşımak için en kritik bölümler:
[5](#5-ddd-katmanı-derin), [6](#6-domain-events-derin), [7](#7-cqrs--mediatr-pipeline-derin),
[9](#9-composition-root--dependency-injection) ve [16](#16-yeni-projede-uygulama-reçetesi).

---

## 2. Üst seviye mimari

### Katman modeli

JetZen, **Onion / Clean Architecture** ruhuna yakın bir katmanlamayla çalışır ama klasik
"Domain / Application / Infrastructure / API" isimleri yerine fiziksel projelerle ayrışmıştır:

```
            ┌─────────────────────────────────────────────┐
            │              JetZen.Functions                │  ← Giriş noktası (API / host)
            │      HTTP · Queue · Timer · Durable · SignalR │
            └───────────────────────┬─────────────────────┘
                                    │ DI çağrısı (AddCompositionSetup)
            ┌───────────────────────▼─────────────────────┐
            │             JetZen.Composition               │  ← Composition Root (DI birleştirme)
            │        MediatR · AutoMapper · pipeline        │
            └───────────┬───────────────────┬─────────────┘
                        │                   │
          ┌─────────────▼──────┐   ┌────────▼────────────┐
          │  JetZen.Commands   │   │   JetZen.Queries    │  ← Application (CQRS)
          │  (write + events)  │   │   (read-only)       │
          └─────┬───────┬──────┘   └──────┬───────┬──────┘
                │       │                 │       │
        ┌───────▼──┐ ┌──▼───────────┐ ┌───▼────┐ │
        │ JetZen.  │ │ JetZen.      │ │ JetZen.│ │
        │  Dto     │ │  Services    │ │  Dto   │ │
        └────┬─────┘ └──┬───────┬───┘ └────────┘ │
             │          │       │                │
   ┌─────────▼──┐  ┌────▼────┐ ┌▼───────────────▼─┐
   │ JetZen.    │  │ JetZen. │ │ JetZen.          │   ← Domain + Infrastructure
   │  Models    │  │ Repos.  │ │  Common          │
   │ (domain)   │  │ (EF/DB) │ │ (VO, util)       │
   └────────────┘  └─────────┘ └──────────────────┘
```

### Bağımlılık yön kuralı (dependency direction)

**Oklar hep içe (domain'e) doğru akar.** `Models` ve `Common` hiçbir üst katmana referans vermez;
en stabil çekirdektirler. `Functions` en dış halkadır ve yalnızca `Composition`'ı tanır.

- **Neden bu yön?** Domain mantığı, altyapı detaylarından (EF, Azure, HTTP) habersiz kalır.
  Böylece domain'i unit test etmek, altyapıyı değiştirmek (örn. Functions yerine ASP.NET) ucuzlar.
- **Bu projede nasıl görünür?** `JetZen.Models` sadece `JetZen.Common` + `MediatR`'a (event sözleşmesi
  için) bağımlıdır; EF Core'a bile bağımlı değildir. EF konfigürasyonu `JetZen.Repositories`'de
  ayrı `IEntityTypeConfiguration` (Fluent API) dosyalarında durur — domain temiz kalır.

### Üç desenin birleşimi

| Desen | JetZen'de karşılığı | Kazanç |
|------|---------------------|--------|
| **Layered / Clean** | Proje bazlı katman + içe bağımlılık | İzolasyon, test edilebilirlik |
| **CQRS** | `JetZen.Commands` (yazma) ↔ `JetZen.Queries` (okuma) ayrı | Okuma/yazma yollarını bağımsız optimize etmek |
| **Tactical DDD** | Rich entity, Value Object, Domain Event, Aggregate | İş kurallarını tek yerde toplamak, anemic model'den kaçınmak |

---

## 3. Solution & proje sorumlulukları

`JetZen.sln` → **11 proje** (10 kütüphane + 1 test).

| Proje | TFM | Sorumluluk | Bağımlı olduğu |
|-------|-----|------------|----------------|
| **JetZen.Common** | net6.0 | Çekirdek yardımcılar: `ValueObject` base, extension'lar, util'ler, `AppSettings`, sabitler, geo/timezone | (yok) |
| **JetZen.Models** | net8.0 | Domain: entity/aggregate + `DomainEvents/` (160+ event), `Abstract/` arabirimler | Common, MediatR |
| **JetZen.Dto** | net8.0 | DTO'lar + `BaseResponseDto` (timezone dönüşümü, reflection ile update) | Common, Models |
| **JetZen.Repositories** | net8.0 | DAL: `SqlContext`, generic `SqlRepository<T>`, `SQLUnitOfWork`, `ModelBuilders/` (77 Fluent config), `Migrations/`, Dapper + stored proc | Models |
| **JetZen.Services** | net8.0 | Dış entegrasyonlar (email, blob, payment, signing, SMS, SignalR, Graph), `Behaviors/` (pipeline), Identity/JWT | Common, Dto, Repositories, özel SQL cache |
| **JetZen.Commands** | net8.0 | CQRS yazma tarafı: command + validator + handler; `DomainEventHandlers/` (31 dosya) | Dto, Services |
| **JetZen.Queries** | net8.0 | CQRS okuma tarafı: query + handler (read-only, `AsNoTracking`) | Dto, Models, Services |
| **JetZen.Composition** | net8.0 | Composition Root: `AddCompositionSetup`, `MapperProfiles/` (52 AutoMapper profili) | Commands, Queries |
| **JetZen.Functions** | net8.0 (Functions v4) | Host/API: trigger'lar, middleware, `Program.cs` | Composition |
| **Microsoft.Extensions.Caching.SqlServer** | net8.0 | SQL Server tabanlı özel distributed cache sağlayıcısı | (Azure.Identity, SqlClient) |
| **JetZen.UnitTests** | net8.0 | xUnit + Moq + MockQueryable | Commands, Composition, Queries |

Bağımlılık zinciri (dıştan içe):
`Functions → Composition → {Commands, Queries} → {Dto, Services} → {Models, Repositories, Common}`.

---

## 4. Teknoloji yığını (tech stack)

| Kategori | Paket / Teknoloji | Sürüm | Ne işe yarar |
|----------|-------------------|-------|--------------|
| **Runtime** | .NET | 8.0 (Common 6.0) | Ana hedef framework |
| **Host** | Azure Functions Worker | v4 isolated (Worker 1.22) | Serverless host; HTTP/Queue/Timer/Durable/SignalR trigger |
| **CQRS / mesajlaşma** | MediatR | 12.4.0 | In-process request/notification dispatch + pipeline behavior |
| **ORM (yazma)** | EF Core + SqlServer | 8.0.7 | Change tracking, migration, Fluent config |
| **Mikro-ORM (okuma/perf)** | Dapper | 2.1.35 | Stored proc / ham SQL ile hızlı okuma |
| **Mapping** | AutoMapper | 13.0.1 | Entity ↔ DTO dönüşümü (52 profil) |
| **Validation** | FluentValidation | 11.9.2 | Bildirimsel (declarative) command/query doğrulama |
| **Auth** | JwtBearer + Identity + Microsoft.Identity.Web | 8.0.x / 3.0.1 | JWT doğrulama, `IdentityUser<Guid>`, Azure AD |
| **Distributed lock** | DistributedLock.SqlServer | 1.0.5 | Çok-instance senaryolarda kilit |
| **PDF / Office** | QuestPDF, EPPlus, ExcelDataReader | 2024.7 / 7.4 / 3.7 | Belge & rapor üretimi |
| **Görsel** | SixLabors.ImageSharp, SkiaSharp | 3.1.5 / 2.88 | Görsel işleme |
| **Azure SDK** | Blobs, Queues, KeyVault, SignalR.Management, Identity | — | Storage, secret, realtime |
| **3. parti** | Microsoft.Graph, Twilio, Google.Apis.Auth, RestSharp, HtmlAgilityPack | — | Mail/takvim, SMS, OAuth, HTTP, scraping |
| **Geo** | GeoCoordinate.NetCore, GeoTimeZone, TimeZoneConverter | — | Mesafe & zaman dilimi hesapları |
| **Test** | xUnit, Moq, MockQueryable.Moq, coverlet | 2.9 / 4.20 / 7.0 | Birim test + mock `IQueryable` |
| **Serileştirme** | Newtonsoft.Json | 13.0.3 | camelCase + özel ValueObject converter |

**Hibrit ORM notu:** EF Core *yazma* ve change-tracking için, Dapper/stored-proc ise
*okuma/performans-kritik* sorgular için kullanılır (örn. coğrafi yakınlık sorgusu — bkz. bölüm 8).

---

## 5. DDD katmanı (derin)

### 5.1 Entity base sınıfı

**Ne:** Tüm domain entity'leri ortak bir tabandan türer; kimlik (Id), audit alanları ve soft-delete
burada toplanır.

**Bu projede:** `JetZen.Models/Concrete/Entity.cs`

```csharp
public abstract class Entity : EntityDomainEvent   // ← event taşıma yeteneğini miras alır
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    [DatabaseGenerated(DatabaseGeneratedOption.Identity), DataMember]
    public DateTime CreatedDate { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public Guid? IsDeletedByUserId { get; protected set; }
    public DateTime? DeletedDate { get; protected set; }

    public void Delete(Guid? isDeletedByUserId)
    {
        IsDeleted = true;
        DeletedDate = DateTime.UtcNow;
        IsDeletedByUserId = isDeletedByUserId;
    }
}
```

**Neden böyle:**
- **`protected set`** → entity dışından alan ataması yasak. Durum (state) yalnızca davranış
  metotlarıyla değişir → **encapsulation**. Bu, "anemic domain model" (sadece getter/setter'lı,
  mantığı serviste duran) anti-pattern'inden korunmanın temel taşıdır.
- **Soft delete** → veriyi fiziksel silmek yerine işaretler; audit/geri-alma için kritik.
- **Id constructor'da `Guid.NewGuid()`** → kimlik veritabanına gitmeden bellekte oluşur; bu sayede
  yeni entity'ler kaydedilmeden önce ilişkilendirilebilir/event'lere konabilir.

### 5.2 Rich domain model (davranış zengin entity)

**Ne:** İş kuralları entity metotlarında yaşar; servisler bu metotları *çağırır*, kuralı
*yeniden yazmaz*.

**Bu projede:** `Trip`, `Proposal`, `Operator` (hepsi `JetZen.Models/Concrete/`). Örnek davranışlar:

```csharp
// Trip.cs — durum geçişleri + event yayını tek satırda
public void CompleteTrip() => AddDomainEvent(new CompleteTripDomainEvent(this));
public void FlownTrip()    => AddDomainEvent(new FlownTripDomainEvent(this));
public void SetStatus(TripStatus status) { Status = status; UpdatedDate = DateTime.UtcNow; }

// Proposal.cs — iş anlamı taşıyan metotlar (CRUD değil, ubiquitous language)
public void SendProposal(string emailSubject, string emailBody) { /* status + event */ }
public void CloseProposal(ProposalClosingReason reason) { /* idempotent kapama */ }
public Proposal CopyBackupProposal(...) { /* factory benzeri kopyalama */ }
```

**Neden böyle:** Kurallar tek yerde toplanır → tutarlılık. Metot adları **ubiquitous language**
(ortak iş dili) taşır: `CancelTrip`, `PurchaseProposal`, `SendBackupProposal` — kod, iş insanının
konuştuğu dile yakın okunur.

**Kural:** Yeni projende her durum değişimi için public setter değil, **anlamlı bir metot** yaz.
"Set" eki yerine fiil kullan (`Activate`, `Cancel`, `Approve`).

### 5.3 Value Object (VO)

**Ne:** Kimliği olmayan, **değerine göre eşitlenen** (value equality), değişmez (immutable) küçük
tipler. Örn. para birimi, durum kodu, adres.

**Bu projede:** `JetZen.Common/ValueObjects/ValueObject.cs` — equality alt yapısı:

```csharp
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType()) return false;
        var valueObject = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(1, (cur, o) => { unchecked { return cur * 23 + (o?.GetHashCode() ?? 0); } });

    public static bool operator ==(ValueObject a, ValueObject b) { /* null-safe */ }
    public static bool operator !=(ValueObject a, ValueObject b) => !(a == b);
}
```

İki tür VO var:
- **`StringValueObject`** tabanlı *enum benzeri* tipler: `TripStatus`, `ProposalStatus`,
  `OperatorStatus`, `CurrencyCode`, `PaymentMethod` … (60+ adet). Sabit string değer + tip güvenliği.
- **Bileşik VO**: `Address` (sokak/şehir/ülke alanları `GetEqualityComponents` ile karşılaştırılır).

**Neden böyle:** `string Status` yerine `TripStatus Status` kullanmak → geçersiz değer (typo)
derleme zamanında engellenir, davranış (örn. `IsCancellable`) VO'ya taşınabilir. `==` ile iki adres
referansa değil **içeriğe** göre karşılaştırılır.

**DB'ye nasıl yazılır:** EF `HasConversion` ile VO ↔ ilkel tip dönüşümü (bkz. bölüm 8).

### 5.4 Aggregate'ler & rol arabirimleri

- **Aggregate root** örnekleri: `Client`, `Trip`, `Proposal`, `Operator`, `ProposalItinerary`.
  Her biri kendi child koleksiyonunu yönetir (örn. `Trip` → `TripLeg[]`, `TripSheet[]`,
  `TripCancellationTerm[]`).
- **`IBrokerUpdatable`** (`JetZen.Models/Abstract/IBrokerUpdatable.cs`) — birden çok aggregate'e
  polimorfik "broker ata" davranışı kazandırır; `Client`, `Trip`, `Proposal` uygular. Atama sırasında
  ilgili domain event tetiklenir.

**Kural:** Bir transaction'da kural olarak **tek aggregate** değiştirilir; diğer aggregate'ler
domain event ile eventual-consistency mantığında güncellenir (bkz. bölüm 6).

---

## 6. Domain Events (derin)

### 6.1 Olay sözleşmesi

**Ne:** "Domain'de anlamlı bir şey oldu" bildirimi. JetZen'de event = MediatR `INotification`.

**Bu projede:** `JetZen.Models/DomainEvents/` (160+ event). Tipik event saf veri taşıyıcısıdır:

```csharp
public class TripDomainEvent : INotification
{
    public TripDomainEvent(Trip trip) => Trip = trip;
    public Trip Trip { get; protected set; }
}
```

### 6.2 Event'in entity'de toplanması

Entity'ler event'i hemen yayınlamaz; **biriktirir**. Taban:

`JetZen.Models/DomainEvents/EntityDomainEvent.cs`

```csharp
public abstract class EntityDomainEvent
{
    private List<INotification> _domainEvents;
    public List<INotification> DomainEvents => _domainEvents;

    public void AddDomainEvent(INotification domainEvent)
    {
        _domainEvents ??= new List<INotification>();
        _domainEvents.Add(domainEvent);
    }
    public void RemoveDomainEvent(INotification domainEvent) => _domainEvents?.Remove(domainEvent);
}
```

**Neden biriktir, hemen yayma?** Çünkü event handler'lar yan etki üretir (mail, log, başka aggregate
güncelleme). Bunları **handler bittikten sonra, kalıcılaştırmadan hemen önce** topluca işlemek
tutarlılık sağlar.

### 6.3 Yayın + kalıcılık orkestrasyonu (kalbi)

Tüm event yayını ve `SaveChanges` **tek bir pipeline behavior'da** merkezîleşir:

`JetZen.Services/Behaviors/UnitOfWorkBehavior.cs`

```csharp
public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
    var response = await next();              // 1) Command handler çalışır → entity'ler event biriktirir

    await PublishDomainEvents(ct);            // 2) Event'leri yay (cascade döngü)

    foreach (var ctx in _unitOfWorks.Where(x => x.GetChanges().Any()))
        await ctx.SaveChangesAsync();         // 3) DB'ye yaz

    await _eventNotification.NotifyAsync(ct); // 4) Dış bildirim (SignalR/queue)
    return response;
}

private async Task PublishDomainEvents(CancellationToken ct)
{
    while (AnyDomainEvents())                                   // cascade: handler yeni event üretebilir
    {
        var entities = _unitOfWorks.SelectMany(x => x.GetChanges<EntityDomainEvent>())
                                   .Where(x => x.DomainEvents != null).ToList();
        var domainEvents = entities.SelectMany(x => x.DomainEvents).ToList();
        entities.ForEach(x => x.DomainEvents.Clear());          // tekrar yayını önle

        foreach (var @event in domainEvents)
            await _mediator.Publish(@event, ct);                // tüm INotificationHandler'lar tetiklenir

        foreach (var ctx in _unitOfWorks.Where(x => x.GetChanges().Any()))
            await ctx.SaveChangesAsync();                       // handler yan etkilerini kaydet
    }
}
```

Akış diyagramı:

```
Command Handler
     │  entity.AddDomainEvent(...)
     ▼
ChangeTracker'da biriken event'ler
     │
     ▼
while (event var?) ──► MediatR.Publish ──► [Handler A][Handler B]... ──► SaveChanges
     ▲                                            │ (yeni event üretebilir)
     └────────────────────────────────────────────┘
     │ (event kalmadı)
     ▼
SaveChanges (ana) ──► EventNotification.NotifyAsync (SignalR/Queue)
```

**Neden cascade `while` döngüsü?** Bir handler başka bir entity'yi değiştirip yeni event üretebilir
(örn. "Trip oluşturuldu" → "Client doğrulandı" → "ActivityLog eklendi"). Döngü, event seti
boşalana kadar (quiescent) devam eder.

### 6.4 Handler deseni

**Bu projede:** `JetZen.Commands/DomainEventHandlers/` (31 dosya). Adlandırma: `On[Tetik][Aksiyon]DomainEventHandler`.

```csharp
public class OnTripCreatedUpdateClientVerifiedDomainEventHandler
    : INotificationHandler<TripDomainEvent>
{
    private readonly ISQLRepository<Client> _clients;
    public async Task Handle(TripDomainEvent notification, CancellationToken ct)
    {
        var client = await _clients.FirstAsync(x => x.Id == notification.Trip.ClientId, ct);
        client.SetEmailConfirmed(true);
        client.SetPhoneNumberConfirmed(true);
    }
}
```

Bir handler **birden çok event tipini** de dinleyebilir (örn. `AddActivityLogDomainEventHandler`
8 farklı `INotificationHandler<T>` uygular → her domain olayı için aktivite logu yazar).

**Kayıt:** Otomatik. `config.RegisterServicesFromAssemblies(...)` MediatR'a handler assembly'lerini
tarar (bkz. bölüm 9).

---

## 7. CQRS + MediatR pipeline (derin)

### 7.1 Command (yazma) tam örneği

`JetZen.Commands/Aircrafts/AddAircraft/` altında 3 parça bir arada durur:

```csharp
// 1) Request — record, IRequest<TResponse>
public record AddAircraftCommand(Guid OperatorId, string TailNumber, int Seats /* ... */)
    : IRequest<AircraftDto>;

// 2) Validator — FluentValidation; DB'ye async erişebilir
public class AddAircraftCommandValidator : AbstractValidator<AddAircraftCommand>
{
    public AddAircraftCommandValidator(ISQLRepository<Aircraft> aircrafts)
    {
        RuleFor(x => x.TailNumber).NotEmpty().WithMessage("Tail Number is required.");
        RuleFor(x => x).MustAsync(async (root, _, _, ct) =>
            await aircrafts.AsNoTracking().FirstOrDefaultAsync(a => a.TailNumber == root.TailNumber, ct) is null
        ).WithMessage("The tail number you are trying to add already exists.");
    }
}

// 3) Handler — sadece orkestrasyon; iş kuralı entity'de
public class AddAircraftCommandHandler : IRequestHandler<AddAircraftCommand, AircraftDto>
{
    private readonly ISQLRepository<Aircraft> _aircrafts;
    private readonly IMapper _mapper;
    public async Task<AircraftDto> Handle(AddAircraftCommand request, CancellationToken ct)
    {
        var aircraft = new Aircraft(/* ... */);     // domain kuralları ctor/metotta
        await _aircrafts.AddAsync(aircraft, ct);
        return _mapper.Map<AircraftDto>(aircraft);
    }
}
```

> Dikkat: Handler **`SaveChanges` çağırmaz**. Kalıcılık `UnitOfWorkBehavior`'a bırakılır → tek
> commit noktası, transaction tutarlılığı.

### 7.2 Query (okuma) tam örneği

```csharp
public record GetAircraftQuery(Guid Id) : IRequest<AircraftDto>;

public class GetAircraftQueryHandler : IRequestHandler<GetAircraftQuery, AircraftDto>
{
    readonly ISQLRepository<Aircraft> _aircrafts;
    readonly IMapper _mapper;
    public async Task<AircraftDto> Handle(GetAircraftQuery request, CancellationToken ct)
    {
        var aircraft = await _aircrafts.AsNoTracking()      // ← okuma: tracking yok
            .Include(x => x.Registration).Include(x => x.Operator)
            .AsSplitQuery()                                 // ← N+1/kartezyen patlamayı azalt
            .FirstAsync(x => x.Id == request.Id, ct);
        return _mapper.Map<AircraftDto>(aircraft);
    }
}
```

**Command vs Query farkı:** Query'ler `AsNoTracking` (salt-okunur, hızlı), change-tracking yok,
event üretmez. Command'ler entity'i izler, event üretir, `UnitOfWorkBehavior` üzerinden yazar.

### 7.3 Pipeline behavior'lar (cross-cutting)

MediatR pipeline'ı her request'i **soğan zarı gibi sarar**. Kayıt sırası (DI'da) sarmalama sırasını
belirler — `JetZen.Composition/DependencyInjection.cs`:

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>)); // en dış
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(QueueBehaviour<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));  // en iç (handler'a en yakın)
```

Sarmalama ve yürütme (request içeri, response dışarı):

```
Request →  Validation.before  →  Queue.before  →  UnitOfWork.before  →  [HANDLER]
                                                                            │
Response ← Validation.after   ←  Queue.after   ←  UnitOfWork.after   ←──────┘
```

| Behavior | Dosya | Görev (ne zaman) |
|----------|-------|------------------|
| **ValidationBehaviour** | `Services/Behaviors/ValidationBehaviour.cs` | Handler'dan **önce** tüm `IValidator<TRequest>`'ları çalıştırır; hata varsa `ValidationException` fırlatır (handler hiç çalışmaz). |
| **QueueBehaviour** | `Services/Behaviors/QueueBehaviour.cs` | Handler **sonrası** biriken queue mesajlarını flush eder (güvenilir async mesajlaşma). |
| **UnitOfWorkBehavior** | `Services/Behaviors/UnitOfWorkBehavior.cs` | Handler **sonrası** domain event'leri yayınlar → `SaveChanges` → dış bildirim. Tek commit noktası. |

**Neden pipeline?** Validation, transaction, mesaj flush gibi her komutta tekrarlanan endişeler
handler'lardan çıkarılır → handler'lar sadece iş senaryosuna odaklanır (DRY + Single Responsibility).

---

## 8. Persistence (kalıcılık katmanı)

### 8.1 Generic repository

`JetZen.Repositories/Abstract/ISQLRepository.cs` — generic, `IQueryable<TEntity>` türevli:

```csharp
public interface ISQLRepository<TEntity> : IQueryable<TEntity>, IEnumerable<TEntity>, IQueryable
{
    Task AddAsync(TEntity entity, CancellationToken ct);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task RemoveAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct);
    Task<IEnumerable<TEntity>> GetByIds(IEnumerable<Guid> ids, CancellationToken ct);
    // Performans-kritik: ham SQL / stored proc
    Task<IEnumerable<Guid>> GetAirportsWithStoredProcedure(double lat, double lon, int distance);
    Task<IEnumerable<Guid>> GetFilteredOperators(string searchQuery, int skip, int take);
    // ...
}
```

`ISQLRepository<T>` `IQueryable` türettiği için handler'larda doğrudan LINQ (`Include`, `Where`,
`AsNoTracking`, `FirstAsync`) yazılabilir. *(Bunun bir tasarım tartışması var — bkz. bölüm 15.)*

### 8.2 Unit of Work + ChangeTracker

`JetZen.Repositories/Concrete/SqlUnitOfWork.cs` — EF'in `ChangeTracker`'ı üstüne ince bir kabuk:

```csharp
public class SQLUnitOfWork : IUnitOfWork
{
    private SqlContext _context;
    public IEnumerable<TEntity> GetChanges<TEntity>() where TEntity : class
        => _context.ChangeTracker.Entries<TEntity>().Select(e => e.Entity);
    public IEnumerable<dynamic> GetChanges()
        => _context.ChangeTracker.Entries().Select(e => e.Entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
```

`UnitOfWorkBehavior`, `GetChanges<EntityDomainEvent>()` ile **değişen tüm entity'lerden** event'leri
toplar — bu yüzden handler'ın event'i nereye eklediğinin önemi yoktur; ChangeTracker hepsini görür.

### 8.3 Fluent konfigürasyon + VO dönüşümü

Her entity için ayrı `IEntityTypeConfiguration<T>` → `JetZen.Repositories/ModelBuilders/` (77 dosya).
Value Object'ler `HasConversion` ile ilkel tipe inip çıkar:

```csharp
public class TripModelBuilder : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Status)
               .HasConversion(v => v.Value, v => new TripStatus(v))   // VO ↔ string
               .IsRequired();
        builder.HasMany(t => t.Legs).WithOne().HasForeignKey(l => l.TripId);
        builder.HasIndex(t => t.Status);
    }
}
```

**Neden ayrı dosyalar?** Domain (`Models`) EF'den habersiz kalır; tüm persistence detayı
`Repositories`'de izole edilir. 77 küçük dosya, tek dev `OnModelCreating`'den okunabilir.

### 8.4 Diğer persistence detayları

- **Migration-on-startup:** `Program.cs` → `await context.Database.MigrateAsync()` (uygulama açılışında
  şema güncellenir). *(Risk tartışması: bölüm 15.)*
- **Azure AD bağlantı kimliği:** `AzureAdAuthenticationDbConnectionInterceptor` — DB'ye managed
  identity / Entra token ile bağlanır (parolasız).
- **Hibrit okuma:** Coğrafi/karmaşık sorgular stored proc + Dapper ile (EF LINQ yerine performans).
- **Distributed cache:** Özel `Microsoft.Extensions.Caching.SqlServer` projesi → session/cache SQL'de.

---

## 9. Composition root & Dependency Injection

**Tek giriş:** `JetZen.Composition/DependencyInjection.cs` → `AddCompositionSetup(appSettings, assemblies)`.
Tüm DI burada birleşir (Composition Root deseni — kayıtlar tek yerde toplanır).

```csharp
public static void AddCompositionSetup(this IServiceCollection services, AppSettings appSettings, params Assembly[] assemblies)
{
    services.AddScoped<UserInfo>();              // istek başına kullanıcı bağlamı
    services.AddHttpContextAccessor();

    services.Configure<IdentityOptions>(o => { /* parola politikası */ });

    services.AddMediatR(c => c.RegisterServicesFromAssemblies(assemblies.Distinct().ToArray()));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(QueueBehaviour<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

    services.AddServices(appSettings);           // JetZen.Services dış entegrasyonları
    services.AddValidatorsFromAssemblies(assemblies.Distinct(), ServiceLifetime.Scoped);
    services.AddAutoMapper(typeof(AirportProfile).Assembly);   // 52 profil assembly tarama

    services.AddDbContext<SqlContext>(x =>
    {
        x.UseSqlServer(appSettings.SQLConnectionString);
        x.AddInterceptors(new AzureAdAuthenticationDbConnectionInterceptor());
    }).AddTransient(typeof(IUnitOfWork), typeof(SQLUnitOfWork));

    services.AddScoped(typeof(ISQLRepository<>), typeof(SqlRepository<>));
    services.AddScoped<ISQLQueryService, SQLQueryService>();
}
```

### Yaşam süreleri (lifetimes) ve *neden*

| Servis | Lifetime | Neden |
|--------|----------|-------|
| `SqlContext` (DbContext) | Scoped | İstek başına bir bağlam; EF tracking'in beklediği desen |
| `ISQLRepository<>` | Scoped | DbContext ile aynı ömür (aynı transaction) |
| `IUnitOfWork` | Transient | Hafif kabuk; her enjeksiyonda taze |
| Pipeline behavior'lar | Transient | Stateless; her request'te yeni |
| Validator'lar | Scoped | DB'ye erişen async kurallar DbContext ömrüyle uyumlu |
| AutoMapper | Singleton | Profil konfigürasyonu değişmez, paylaşılabilir |
| `AppSettings` | Singleton | Salt-okunur global konfigürasyon |
| `UserInfo` | Scoped | İstek başına kullanıcı kimliği |

**Assembly scanning:** MediatR, FluentValidation ve AutoMapper, handler/validator/profile sınıflarını
**otomatik bulur**. Yeni bir command eklemek için DI'a tek satır bile yazmazsın — sözleşmeyi
(`IRequest`, `AbstractValidator`, `Profile`) uygulaman yeterli. Bu, "open/closed" prensibini
operasyonel kılar.

---

## 10. Servisler & dış entegrasyonlar

`JetZen.Services` — `Abstract/` (arabirimler) + `Concrete/` (implementasyonlar) + `Behaviors/`.
Tüm dış dünya (3rd-party) **arabirim arkasına** alınır → handler'lar somut SDK'ya bağımlı değildir,
test'te mock'lanabilir.

| Endişe | Arabirim | Sağlayıcı / teknoloji |
|--------|----------|-----------------------|
| E-posta | `IEmailProvider`, `IGraphService` | Microsoft Graph |
| Blob depolama | `IBlobService` | Azure Blob (+ KeyVault sertifikası ile client-side encryption) |
| Ödeme | `IPaymentService` | Clover (charge/capture/refund/customer) |
| Belge imzalama | `ISigningService` | **PandaDoc + SignNow** (strateji deseni) |
| SMS | `ISmsService` | Twilio |
| Realtime | `ISignalRService` | Azure SignalR |
| PDF/Excel | `IBuilderService`, `IExcelGenerateService` | QuestPDF, EPPlus |
| Push bildirim | `IPushNotificationService` | iOS/Android |
| Hava/uçuş verisi | `IWeatherForecastService`, `IAviapagesService` | OpenMeteo, Aviapages |

### Strateji deseni örneği (imzalama)

`ISigningService` aynı sözleşmeyi iki sağlayıcı uygular:

```csharp
public interface ISigningService
{
    string ProviderName { get; }                  // "Pandadoc" / "SignNow"
    Task<SignLinkResponseDto> GenerateSignLink(byte[] doc, string ext, string redirect, Guid cbParam, CancellationToken ct);
    Task<byte[]> GetDocument(string documentId, CancellationToken ct);
    Task<BaseHttpResponse> SubscribeDocument(string documentId, string @event, Guid cbParam, CancellationToken ct);
}
```

DI'a iki implementasyon da kaydedilir; `ISigningProvider` doğru olanı `ProviderName`'e göre seçer.
**Neden:** Sağlayıcı değiştirmek/yedeklemek (failover) tek satır konfigürasyonla mümkün; iş mantığı
hangi sağlayıcının kullanıldığını bilmez (Open/Closed + Strategy).

> Bu projede tüm secret/token değerleri `AppSettings` üzerinden (üretimde KeyVault'tan) gelir —
> **kodda sabit gömülü değer yoktur**.

---

## 11. API katmanı (Azure Functions)

`JetZen.Functions` — domaine göre klasörlenmiş **ince** trigger sınıfları. Endpoint'in tek işi:
request'i bir command/query'e çevirip `_mediator.Send` demek.

```csharp
public class AircraftsFunctions
{
    readonly IMediator _mediator;
    public AircraftsFunctions(IMediator mediator) => _mediator = mediator;

    [Function("GetAircraft")]
    [Authorize]
    public async Task<AircraftDto> GetAircraft(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "aircrafts/{id:guid}")] HttpRequestData req,
        Guid id)
        => await _mediator.Send(new GetAircraftQuery(id));

    [Function("AddAircraft")]
    [Authorize]
    public async Task<AircraftDto> AddAircraft(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "aircrafts")] HttpRequestData request)
    {
        var command = await request.GetValueFromBody<AddAircraftCommand>();
        return await _mediator.Send(command);   // pipeline: validation → handler → uow
    }
}
```

**Trigger çeşitleri:** HTTP (REST), Queue (Azure Storage Queue), Timer (zamanlanmış iş),
Durable Task (uzun süreli orkestrasyon), SignalR.

**Host (`Program.cs`) sırası ve sorumlulukları:**

```csharp
var host = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddJsonFile("appsettings.json", false, true))
    .ConfigureServices(async (builder, service) =>
    {
        service.AddCompositionSetup(SetupConfig(builder, service),
            typeof(CreateClientCommand).Assembly,        // Commands
            typeof(GetClientQuery).Assembly,             // Queries
            typeof(AddProposalDomainEvent).Assembly);    // DomainEvents
        var context = service.BuildServiceProvider().GetService<SqlContext>();
        await context.Database.MigrateAsync();           // açılışta migration
    })
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        // Newtonsoft: camelCase + null atla + ValueObject converter
        worker.UseMiddleware<ExceptionMiddleware>();         // 1) hata → uygun HTTP yanıt
        worker.UseMiddleware<AuthenticationMiddleware>();    // 2) JWT/identity
        worker.UseMiddleware<ResponseManipulateMiddleware>();// 3) yanıt son işleme (timezone vb.)
    })
    .ConfigureOpenApi()
    .Build();
```

**Konfigürasyon kaynağı:** `IS_PRODUCTION=true` ise `AppSettings(true)` (üretim → KeyVault),
değilse `appsettings.json`'daki `App` section. **Hiçbir secret repoda değildir.**

---

## 12. Frontend topolojisi

`JetZen.Apps/` — 4 ayrı uygulama. Hepsinde ortak: **SignalR** ile realtime, **Axios/HttpClient**
ile aynı Functions API'sine REST, **MSAL/OAuth** ile kimlik.

| Uygulama | Framework | State | Render | Amaç |
|----------|-----------|-------|--------|------|
| `jetzen-web-site` | Angular 17 | NgRx 17 | SSR (Express) | Halka açık site + müşteri portalı |
| `jetzen-broker-app` | Angular 14 | NgRx 13 | SSR (Universal) | Broker paneli / admin |
| `jetzen-mobile` | React Native 0.73 | Redux + redux-persist | — | Müşteri mobil (Google/Apple sign-in, Firebase analytics, biometrics) |
| `jetzen-broker-mobile` | React Native 0.74 | Redux + redux-persist | — | Broker mobil (push notification) |

**Gözlem:** Web tarafı Angular+NgRx, mobil taraf RN+Redux. İki web app farklı Angular major'da
(14 vs 17) — uzun yaşayan repoda kademeli yükseltme işareti.

---

## 13. DevOps & runtime

- **CI/CD:** Azure DevOps pipeline'ları (kökte `azure-pipelines*.yml`). Backend (`api`), web-client,
  web-broker, front-end için ayrı pipeline. Docker image → Azure Container Registry.
- **Konteyner:** Linux tabanlı; Functions worker `Exe` olarak paketlenir.
- **Kubernetes:** `manifests/` (9 dosya) → 3 ortam (dev / uat / prod) × 3 katman (backend / broker /
  client). `Deployment` + `ClusterIP Service`; config **K8s secret** olarak enjekte edilir
  (`envFrom: secretRef`). Secret değerleri repoda yok.
- **Secret yönetimi:** Üretimde Azure KeyVault; geliştirmede Azurite (local Azure Storage emulator,
  `127.0.0.1:10000`).

---

## 14. Best-practice envanteri

Bu projenin **doğru** yaptıkları (yeni projede koru):

1. **Rich domain model** — iş kuralı entity'de, serviste değil. `protected/private set` ile katı
   encapsulation.
2. **Tek commit noktası** — handler'lar `SaveChanges` çağırmaz; `UnitOfWorkBehavior` merkezîleştirir →
   transaction tutarlılığı, "yarım kaydetme" yok.
3. **Event-driven yan etki** — mail/log/çapraz-aggregate güncelleme domain event handler'larında →
   handler'lar ince, gevşek bağlı (loosely coupled).
4. **İnce API katmanı** — Functions sadece `_mediator.Send`; iş mantığı sızmıyor.
5. **CQRS okuma/yazma ayrımı** — query'ler `AsNoTracking` + `AsSplitQuery`, event üretmez.
6. **Bildirimsel validation** — FluentValidation pipeline'da; handler doğrulamaya boğulmuyor.
7. **Value Object'lerle tip güvenliği** — geçersiz durum/para birimi derleme zamanında yakalanır.
8. **Arabirim arkasına alınmış entegrasyonlar** — Clover/Twilio/Graph mock'lanabilir, sağlayıcı
   değiştirilebilir (strateji deseni).
9. **Assembly scanning** — yeni handler/validator/profil otomatik bulunur; DI'da elle kayıt yok.
10. **Parolasız DB** — Azure AD interceptor ile managed identity; connection string'de parola yok.
11. **Config ayrımı** — secret'lar KeyVault/K8s secret; repoda gerçek değer yok.

---

## 15. İyileştirme önerileri (öğretici)

Aşağıdakiler **gözlemlenen riskler**; her biri *neden sorun / nasıl düzeltilir* ile. Yeni projende
baştan kaçınabilirsin.

### 15.1 `Program.cs` içinde `BuildServiceProvider()` (anti-pattern)
```csharp
var context = service.BuildServiceProvider().GetService<SqlContext>();   // ⚠
```
**Sorun:** `ConfigureServices` içinde container'ı erken inşa etmek **ikinci bir provider** yaratır;
singleton'lar iki kez kurulabilir, kaynak sızıntısı/çift instance riski. Aynı kalıp
`AddCompositionSetup` içindeki `appSettings` çözümünde de var.
**Çözüm:** Migration'ı host **build edildikten sonra** bir scope'ta çalıştır:
```csharp
using var scope = host.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<SqlContext>().Database.MigrateAsync();
```
Konfigürasyonu `IOptions<T>` / `builder.Configuration.Bind` ile provider kurmadan çöz.

### 15.2 `async void` benzeri: `ConfigureServices(async (...) => ...)`
**Sorun:** `ConfigureServices` lambda'sı `async` yapılmış; bu fiilen `async void` gibi davranır —
exception yutulabilir, sıralama garanti edilmez.
**Çözüm:** DI kaydı senkron olmalı; migration gibi async işi açılış (startup) adımına (yukarıdaki
scope) taşı.

### 15.3 Repository `IQueryable` sızdırıyor
`ISQLRepository<T> : IQueryable<T>` → handler'lar `.Include().Where().FirstAsync()` yazabiliyor.
**Sorun:** Persistence detayı (EF LINQ) uygulama katmanına sızar; repository'nin "soyutlama" değeri
azalır, sorgu mantığı dağılır, EF'siz test zorlaşır.
**Değerlendirme:** Bu pragmatik bir tercih (hız). Ama büyük ekipte sorguları **anlamlı repo
metotları** (`GetActiveByOperator(...)`) veya **specification pattern** ardına almak okunurluğu artırır.

### 15.4 Event publish, `SaveChanges`'ten **önce** çalışıyor
`UnitOfWorkBehavior` önce `PublishDomainEvents` (ki içinde ara `SaveChanges` var) yapar.
**Sorun:** Bir handler henüz kalıcılaşmamış veriye dayalı yan etki üretebilir; ya da event
işlenir ama nihai `SaveChanges` patlarsa **tutarsızlık** olur (mail gitti, kayıt yok).
**Çözüm seçenekleri:**
- **Outbox pattern:** event'leri aynı transaction'da bir `OutboxMessage` tablosuna yaz, ayrı bir
  worker güvenilir biçimde yayınlasın → "exactly-once benzeri" teslim, atomicity.
- Ya da event'leri `SaveChanges` **sonrası** yay (kalıcılık kesin), dış bildirimleri idempotent yap.

### 15.5 Transaction sınırı belirsiz
Cascade `while` döngüsünde her tur kendi `SaveChangesAsync`'ini çağırıyor; explicit
`BeginTransaction` yok.
**Sorun:** Bir handler 3. turda hata verirse, önceki turların yazımı **geri alınmaz** (EF her
`SaveChanges`'i ayrı commit'ler).
**Çözüm:** Tüm pipeline'ı tek `IDbContextTransaction` veya `TransactionScope` içine al; sonda tek
commit. (Functions + çoklu kaynak varsa idempotency + outbox tercih et.)

### 15.6 Karışık TFM (net6.0 vs net8.0)
`JetZen.Common` net6.0, gerisi net8.0.
**Sorun:** Bakım yükü, çift runtime, güvenlik yaması ikiliği; net6 EOL.
**Çözüm:** `Common`'ı net8.0'a (veya çoklu hedefe) terfi et; tek TFM hedefle.

### 15.7 Migration-on-startup (üretimde riskli)
`Program.cs` her açılışta `MigrateAsync` çağırıyor.
**Sorun:** Çok-instance/K8s'te birden çok pod aynı anda migrate etmeye çalışabilir (yarış); başarısız
migration tüm uygulamayı açılışta düşürür.
**Çözüm:** Migration'ı ayrı bir **deploy adımı / init container / tek seferlik job** olarak çalıştır;
uygulama açılışı yalnız "şema hazır mı?" kontrol etsin.

### 15.8 Diğer küçük notlar
- `services.AddAutoMapper(typeof(AirportProfile).Assembly)` tek assembly tarıyor — profiller başka
  assembly'e taşınırsa sessizce kaçabilir; assembly listesi parametreleştirilebilir.
- AutoMapper'ın `ReverseMap` + `IncludeMembers` yoğun kullanımı; runtime mapping hatalarını erken
  görmek için `configuration.AssertConfigurationIsValid()` testi ekle.
- Identity parola politikası gevşek (`RequireDigit=false`, uzunluk 6) — ürün gereği değilse sıkılaştır.

---

## 16. Yeni projede uygulama reçetesi

Bu mimariyi sıfırdan kurmak için minimum iskelet ve sıra:

### 16.1 Proje iskeleti
```
YourApp.sln
├── YourApp.Common          (net8.0)  → ValueObject base, extensions, AppSettings
├── YourApp.Models          (net8.0)  → Entity/EntityDomainEvent base, aggregate'ler, DomainEvents/
├── YourApp.Dto             (net8.0)  → DTO + BaseResponseDto
├── YourApp.Repositories    (net8.0)  → DbContext, SqlRepository<T>, UnitOfWork, ModelBuilders/, Migrations/
├── YourApp.Services        (net8.0)  → Abstract/ + Concrete/ entegrasyonlar, Behaviors/ (pipeline)
├── YourApp.Commands        (net8.0)  → command + validator + handler, DomainEventHandlers/
├── YourApp.Queries         (net8.0)  → query + handler (AsNoTracking)
├── YourApp.Composition     (net8.0)  → AddCompositionSetup, MapperProfiles/
├── YourApp.Api/Functions   (net8.0)  → trigger/controller (ince), Program.cs, Middlewares/
└── YourApp.UnitTests       (net8.0)  → xUnit + Moq
```

### 16.2 Kurulum sırası (önerilen)
1. **Common + Models**: `Entity` (protected setter, soft delete) + `EntityDomainEvent`
   (`List<INotification>` biriktirme) + `ValueObject` base'i kur.
2. **İlk aggregate**: bir entity'yi rich olarak yaz (davranış metotları + `AddDomainEvent`).
3. **Repositories**: `DbContext`, generic `SqlRepository<T>`, `IUnitOfWork`/`SQLUnitOfWork`
   (ChangeTracker üstüne), entity başına `IEntityTypeConfiguration` + VO `HasConversion`.
4. **Services/Behaviors**: `ValidationBehaviour`, `UnitOfWorkBehavior` (event publish → SaveChanges).
   *(Öneri: 15.4/15.5'i baştan uygula — outbox + tek transaction.)*
5. **Commands/Queries**: `IRequest<T>` + handler + (gerekirse) `AbstractValidator<T>`.
6. **Composition**: `AddMediatR(RegisterServicesFromAssemblies)` + pipeline kayıtları (sıra önemli!) +
   `AddValidatorsFromAssemblies` + `AddAutoMapper` + DbContext/repo kayıtları.
7. **Api/Host**: ince endpoint (`_mediator.Send`) + middleware (exception/auth) + config (secret →
   KeyVault/env, repoda değil). Migration'ı **ayrı adım** olarak çalıştır (15.7).
8. **Test**: MockQueryable ile repository mock'la; handler + validator + domain davranışlarını test et;
   `Mapper.AssertConfigurationIsValid()`.

### 16.3 Altın kurallar (özet)
- Durum değişimi = **anlamlı metot**, public setter değil.
- Handler **`SaveChanges` çağırmaz**; tek commit pipeline'da.
- Yan etki = **domain event handler**, handler'ın gövdesinde değil.
- Dış servis = **arabirim arkasında**, somut SDK handler'a sızmaz.
- Secret = **config/KeyVault**, kodda/repoda asla.
- Yeni handler/validator/profil eklerken **DI'a elle kayıt yazma** (assembly scan halleder).

---

*Doküman repo durumuna göre (develop branch) hazırlanmıştır. Dosya yolları doğrulanmıştır; sürümler
ilgili `.csproj` dosyalarından alınmıştır. Hiçbir credential/secret değeri içermez.*
