# Industrial Processing System

Industrial Processing System je jednostavan .NET sistem za asinhronu obradu
poslova sa prioritetima. Projekat prikazuje kako vise niti moze bezbedno da
prima, cuva i obradjuje poslove, uz logovanje dogadjaja, retry mehanizam,
konfiguraciju preko XML fajla i automatsko generisanje izvestaja.

Najkraca ideja: posao udje u sistem, smesti se u prioritetni red, worker nit ga
preuzme, obradi i upise rezultat.

## Glavne mogucnosti

- asinhrona obrada poslova pomocu `Task`
- thread-safe pristup iz vise niti
- prioritetni red, gde manji broj znaci veci prioritet
- ogranicenje maksimalnog broja aktivnih poslova preko `MaxQueueSize`
- idempotentnost, tako da isti `Job.Id` ne moze da se izvrsi vise puta
- dogadjaji `JobCompleted` i `JobFailed`
- asinhrono logovanje dogadjaja u `events.log`
- timeout od 2 sekunde po pokusaju
- retry mehanizam sa ukupno 3 pokusaja
- `ABORT` log ako posao ne uspe ni iz treceg pokusaja
- XML izvestaj na svakih minut
- cuvanje poslednjih 10 izvestaja
- unit testovi i code coverage

## Model posla

Svaki posao je predstavljen klasom `Job`.

```csharp
public class Job
{
    public Guid Id { get; set; }
    public JobType Type { get; set; }
    public string Payload { get; set; }
    public int Priority { get; set; }
}
```

Kada se posao posalje u sistem, metoda `Submit` vraca `JobHandle`.

```csharp
public class JobHandle
{
    public Guid Id { get; set; }
    public Task<int> Result { get; set; }
}
```

`Task<int>` znaci da rezultat ne mora biti spreman odmah. Sistem nastavlja da
radi, a rezultat se moze sacekati kada bude potreban.

## Vrste poslova

Sistem podrzava dve vrste poslova.

`Prime` posao racuna koliko ima prostih brojeva do zadate vrednosti. Payload
izgleda ovako:

```text
numbers:10_000,threads:3
```

`numbers` je gornja granica za proveru brojeva, a `threads` je broj niti koje se
koriste za racunanje. Broj niti se automatski ogranicava na interval `[1,8]`.

`IO` posao simulira cekanje, kao da se cita vrednost sa neke spoljne adrese ili
uredjaja. Payload izgleda ovako:

```text
delay:1_000
```

`delay` je trajanje cekanja u milisekundama. Nakon cekanja posao vraca nasumican
broj od 0 do 100.

## Kako sistem radi

`ProcessingSystem` je centralna klasa. Ona prima poslove, cuva ih u
prioritetnom redu i pokrece worker niti koje obradjuju poslove.

Tok jednog posla je:

1. Korisnik pozove `Submit(job)`.
2. Sistem proveri da li je posao vec ranije prihvacen.
3. Sistem proveri da li ima mesta u redu.
4. Posao se ubacuje u prioritetni red.
5. Worker nit uzima posao sa najvecim prioritetom.
6. Posao se izvrsava.
7. Ako uspe, pokrece se `JobCompleted`.
8. Ako ne uspe ili traje predugo, sistem pokusava ponovo.
9. Ako ne uspe ni posle treceg pokusaja, upisuje se `ABORT`.

Ovakav pristup prati producer-consumer obrazac: jedne niti proizvode poslove, a
druge niti ih trose i obradjuju.

## Thread-safe delovi

Posto vise niti moze istovremeno da pristupa sistemu, nekoliko delova je
posebno zasticeno:

- `ConcurrentDictionary` cuva istoriju poslova i sprecava duplo izvrsavanje.
- `ConcurrentPriorityQueue` koristi `lock` da bi prioritetni red bio bezbedan.
- `SemaphoreSlim` kontrolise maksimalan broj aktivnih poslova.
- `ConcurrentBag` cuva metriku zavrsenih poslova za izvestaje.

## Organizacija projekta

```text
IndustrialProcessingSystem.Core
|-- Configuration
|   `-- SystemConfig.cs
|-- Infrastructure
|   `-- ConcurrentPriorityQueue.cs
|-- Models
|   |-- Job.cs
|   |-- JobHandle.cs
|   `-- JobType.cs
|-- Processing
|   |-- ProcessingSystem.cs
|   |-- JobEventsArgs.cs
|   `-- JobInfo.cs
|-- Reporting
|   |-- FileEventLogger.cs
|   |-- IEventLogger.cs
|   |-- IReportWriter.cs
|   `-- RollingXmlReportWriter.cs
|-- Services
|   |-- IJobProcessor.cs
|   |-- JobProcessor.cs
|   `-- PayloadParser.cs
`-- Program.cs
```

Podela je namerno jednostavna:

- `Models` sadrzi osnovne podatke.
- `Configuration` cita XML konfiguraciju.
- `Processing` organizuje tok poslova.
- `Services` zna kako se konkretni poslovi izvrsavaju.
- `Infrastructure` sadrzi pomocne thread-safe strukture.
- `Reporting` zapisuje logove i XML izvestaje.

## Konfiguracija

Sistem se podesava kroz `SystemConfig.xml`.

```xml
<SystemConfig>
  <WorkerCount>5</WorkerCount>
  <MaxQueueSize>100</MaxQueueSize>
  <Jobs>
    <Job Type="Prime" Payload="numbers:10_000,threads:3" Priority="1"/>
    <Job Type="IO" Payload="delay:1_000" Priority="3"/>
  </Jobs>
</SystemConfig>
```

`WorkerCount` odredjuje broj worker niti, a `MaxQueueSize` maksimalan broj
aktivnih poslova u sistemu. Pocetni poslovi se ucitavaju iz `Jobs` sekcije.

## Pokretanje

Za pokretanje aplikacije:

```powershell
dotnet run --project IndustrialProcessingSystem.Core
```

Program ucita konfiguraciju, doda pocetne poslove i pokrene niti koje nasumicno
dodaju nove poslove. Aplikacija radi dok se ne pritisne ENTER.

## Testovi

Za pokretanje testova:

```powershell
dotnet test IndustrialProcessingSystem.sln --no-restore -m:1
```

Testovi pokrivaju:

- uspesno dodavanje posla
- odbijanje posla kada je red pun
- idempotentnost
- izvrsavanje `Prime` i `IO` poslova
- prioritetni red
- timeout i `JobFailed` dogadjaj
- citanje XML konfiguracije

## Code coverage

Coverage se pokrece komandom:

```powershell
dotnet test IndustrialProcessingSystem.sln --no-restore -m:1 --collect:"XPlat Code Coverage" --results-directory TestResults
```

Trenutno stanje:

- testovi: `11/11` prolaze
- line coverage: `69.81%`

## Coverage dashboard

GitHub Actions automatski generise i HTML coverage dashboard pomocu
ReportGenerator-a.

Kako se gleda:

1. Otvori GitHub repo.
2. Udji u tab `Actions`.
3. Otvori poslednji `CI` run.
4. Na dnu stranice, u sekciji `Artifacts`, preuzmi `coverage-report-html`.
5. Raspakuj ZIP fajl.
6. Otvori `index.html` u browseru.

Na istoj stranici se vidi i kratak coverage summary, a HTML dashboard daje
detaljniji prikaz po klasama, fajlovima i linijama koda.

## CI

GitHub Actions workflow se nalazi u:

```text
.github/workflows/ci.yml
```

Workflow na svaki push ili pull request pokrece restore, build, testove i code
coverage. Rezultati testova i coverage fajl se cuvaju kao artifacts.
