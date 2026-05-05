# Industrial Processing System

Ovo je mali producer-consumer sistem za obradu industrijskih poslova.
Ideja je jednostavna: poslovi ulaze u sistem, cekaju u redu po prioritetu,
worker niti ih uzimaju i obradjuju asinhrono.

## Sta sistem radi

- `Submit(Job job)` prima posao i vraca `JobHandle`.
- `JobHandle.Result` je `Task<int>` koji predstavlja buduci rezultat posla.
- Manji broj u `Priority` znaci veci prioritet.
- `MaxQueueSize` ogranicava broj aktivnih poslova u sistemu.
- Isti `Job.Id` ne moze da se izvrsi vise puta.
- `JobCompleted` i `JobFailed` se loguju asinhrono u `events.log`.
- Ako posao traje duze od 2 sekunde, pokusava se jos 2 puta.
- Ako i treci pokusaj ne uspe, u log se dodaje `ABORT`.
- Na svakih minut pravi se XML izvestaj sa poslednjih najvise 10 fajlova.

## Vrste poslova

`Prime` posao dobija payload u formatu:

```text
numbers:10_000,threads:3
```

Racuna koliko ima prostih brojeva do zadate granice. Broj niti se pri
parsiranju ogranicava na interval `[1,8]`.

`IO` posao dobija payload u formatu:

```text
delay:1_000
```

Simulira cekanje pomocu `Thread.Sleep` i vraca nasumican broj od 0 do 100.

## Organizacija koda

- `Models` - osnovni modeli: `Job`, `JobHandle`, `JobType`.
- `Configuration` - citanje `SystemConfig.xml`.
- `Processing` - glavni `ProcessingSystem`, dogadjaji i metrika.
- `Services` - konkretna obrada poslova i parsiranje payload-a.
- `Infrastructure` - thread-safe prioritetni red.
- `Reporting` - logovanje dogadjaja i XML izvestaji.

Najbitnije za odbranu: `ProcessingSystem` ne zna detalje kako se broje prosti
brojevi niti kako se pise XML fajl. On samo organizuje posao. Kao sef smene:
prima naloge, pazi na red, daje radnicima zadatke i zapisuje sta se desilo.

## Konfiguracija

Sistem cita `SystemConfig.xml`.

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

`WorkerCount` odredjuje koliko worker niti obradjuje poslove i koliko producer
niti u `Program.cs` nasumicno dodaje nove poslove.

## Pokretanje

```powershell
dotnet run --project IndustrialProcessingSystem.Core
```

Program ucita konfiguraciju, ubaci pocetne poslove, pokrene producere i radi
dok se ne pritisne ENTER.

## Testovi i coverage

```powershell
dotnet test IndustrialProcessingSystem.sln --no-restore -m:1
```

Coverage:

```powershell
dotnet test IndustrialProcessingSystem.sln --no-restore -m:1 --collect:"XPlat Code Coverage" --results-directory TestResults
```

Trenutno izmereno:

- testovi: `11/11` prolaze
- line coverage: `69.81%`
- zahtev iz PDF-a: najmanje `67%`

## Kako objasniti na odbrani

Zamisli jednu traku u fabrici. Poslovi stizu na traku, ali nisu svi jednako
hitni. Zato red uvek drzi najhitnije poslove napred.

Worker nit je radnik koji uzima prvi posao sa trake. Ako posao uspe, javlja se
dogadjaj `JobCompleted`. Ako traje previse dugo, sistem ga pokusava ponovo.
Posle tri neuspeha posao se oznacava kao `ABORT`, jer ne zelimo da zaglavi ceo
sistem.

`Task<int>` je obecanje da ce rezultat stici kasnije. Zato `Submit` ne ceka da
se posao zavrsi; odmah vrati `JobHandle`, a korisnik moze kasnije da saceka
`handle.Result`.

Thread-safe deo je vazan zato sto vise niti istovremeno dodaje i uzima poslove.
Zato koristimo zakljucavanje u prioritetnom redu, `ConcurrentDictionary` za
istoriju poslova i `SemaphoreSlim` da red nikada ne predje dozvoljeni kapacitet.

LINQ izvestaj radi kao fotografija sistema: uzme zavrsene poslove, grupise ih po
tipu, izracuna broj uspesnih, prosecno vreme i broj neuspesnih, pa to upise u
XML.
