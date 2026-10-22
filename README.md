# Ewidencja członków spółki (MKS.DeepWater)

Aplikacja webowa do zbierania i gromadzenia informacji o członkach spółki:
dane osobowe i adresowe, wpłaty składek członkowskich oraz raporty z zebranych danych.

## Technologie

| Obszar | Rozwiązanie |
|---|---|
| Framework | ASP.NET Core 10 (MVC) |
| Baza danych | SQLite – plik `App_Data/deepwater.db` (brak zewnętrznego serwera) |
| Dostęp do danych | Entity Framework Core 10 + migracje |
| Uwierzytelnianie | ASP.NET Core Identity (konta lokalne, logowanie login + hasło, role) |
| UI | Razor Views + Bootstrap 5 |

## Uruchomienie

```bash
dotnet run --project src/MKS.DeepWater.Web
```

Przy pierwszym starcie aplikacja sama:
1. tworzy plik bazy `src/MKS.DeepWater.Web/App_Data/deepwater.db` i wykonuje migracje,
2. zakłada role `Administrator` i `Pracownik`,
3. zakłada konto startowe administratora na podstawie konfiguracji `SeedAdmin`,
4. wpisuje 3 przykładowych członków z adresami i jedną wpłatą składki.

### Konto startowe (SeedAdmin)

Dane konta startowego **nie są trzymane w repozytorium**. Hasło trzeba podać przed pierwszym
uruchomieniem – gdy konta jeszcze nie ma, a `SeedAdmin:Password` nie jest ustawione, aplikacja
przerywa start z komunikatem, jak je ustawić. Kolejne starty hasła już nie wymagają (konto
istnieje), więc usunięcie sekretu nie wywraca działającej instancji.

Lokalnie – user-secrets (ładowane tylko w środowisku `Development`):

```bash
cd src/MKS.DeepWater.Web
dotnet user-secrets set "SeedAdmin:UserName" "admin"
dotnet user-secrets set "SeedAdmin:Password" "<haslo>"
dotnet user-secrets set "SeedAdmin:Email" "admin@localhost"
```

Na serwerze – zmienne środowiskowe: `SeedAdmin__UserName`, `SeedAdmin__Password`,
`SeedAdmin__Email`.

Hasło musi spełniać politykę Identity z `Program.cs`: min. 8 znaków, wielka i mała litera,
cyfra, znak specjalny, min. 4 różne znaki. Polityka jest sprawdzana przy zakładaniu konta
i przy zmianie hasła – jej zaostrzenie **nie unieważnia haseł już zapisanych w bazie**.

Konto zakładane jest tylko wtedy, gdy nie istnieje, więc zmiana `SeedAdmin:Password` nie
podmienia hasła istniejącemu kontu. Aplikacja nie ma jeszcze ekranu zmiany hasła
(patrz „Kolejne kroki”), dlatego jedyny sposób wymiany hasła konta startowego to usunięcie
plików `App_Data/deepwater.db*` i odtworzenie bazy od nowa – razem z danymi. Bazę założoną
starym, słabym hasłem trzeba odtworzyć ręcznie.

## Struktura projektu

```
MKS.DeepWater.slnx
src/MKS.DeepWater.Web/
  Program.cs                 # konfiguracja: SQLite, Identity, globalne [Authorize], routing
  appsettings.json           # connection string (SeedAdmin poza repozytorium)
  Data/
    ApplicationDbContext.cs  # DbContext (Identity + domena), indeksy, mapowanie decimal->double
    ApplicationUser.cs       # konto użytkownika obsługi
    DbInitializer.cs         # migracje, role, konto admina, dane demo
    Migrations/              # migracje EF Core (InitialCreate)
  Models/
    Domain/                  # model danych: Member, Address, FeePayment, FeeRate, Enums
    ViewModels/              # modele widoków: LoginViewModel, MemberListViewModel
    ErrorViewModel.cs
  Controllers/
    AccountController.cs     # logowanie / wylogowanie / brak dostępu
    MembersController.cs     # lista + CRUD członków, miękkie usuwanie i przywracanie
    HomeController.cs        # przekierowanie na listę + strona błędu
  Views/
    Shared/_Layout.cshtml    # szablon z zakładką "Lista członków"
    Account/Login.cshtml     # strona logowania
    Members/Index.cshtml     # lista członków z menu akcji w wierszu
    Members/_MemberForm.cshtml  # wspólny formularz dla dodawania i edycji
    Members/Create.cshtml, Edit.cshtml, Delete.cshtml
  wwwroot/                   # Bootstrap, jQuery, CSS
```

## Model danych

- **Member** – numer członkowski (unikalny), imię, nazwisko, PESEL, e-mail, telefon,
  data urodzenia, data przyjęcia/ustania członkostwa, status (aktywny / zawieszony / były),
  liczba udziałów, uwagi oraz znaczniki `DeletedAt` / `DeletedBy` (miękkie usuwanie).
- **Address** – adres przypisany do członka; rodzaj: zameldowania / korespondencyjny / inny
  (ulica, nr domu, nr lokalu, kod pocztowy, miejscowość, kraj).
- **FeePayment** – wpłata składki: rok składkowy, data wpłaty, kwota, forma (przelew/gotówka/karta),
  numer dokumentu, uwagi.
- **FeeRate** – stawka składki na udział obowiązująca w danym roku (podstawa do raportu zaległości).

Relacje: `Member 1—* Address`, `Member 1—* FeePayment` (kaskadowe usuwanie).

### Miękkie usuwanie

Usunięcie członka ustawia `DeletedAt` i `DeletedBy` – wiersz, jego adresy i wpłaty zostają
w bazie. Globalny filtr zapytań (`HasQueryFilter`) na `Member`, `Address` i `FeePayment`
sprawia, że usunięte dane nie pojawiają się w żadnym zwykłym zapytaniu; lista pokazuje je
dopiero z filtrem „pokaż usunięte" (`IgnoreQueryFilters()`), skąd można je przywrócić.

`DeletedAt` to wycofanie rekordu z ewidencji i nie ma nic wspólnego ze statusem
`MemberStatus.Former` („były członek"), który opisuje stan członkostwa.

Numer członkowski jest unikalny także wśród usuniętych rekordów – przy dodawaniu zajęty
numer zwraca błąd walidacji zamiast wyjątku z indeksu bazy.

## Stan realizacji

Zrealizowane (definition of done):
- projekt się buduje (`dotnet build` – 0 błędów, 0 ostrzeżeń),
- cała aplikacja wymaga zalogowania, anonim jest przekierowany na `/Account/Login`,
- logowanie kontem startowym (`SeedAdmin`) działa,
- po zalogowaniu widoczny szablon z jedną zakładką **Lista członków** (dane z bazy,
  filtr: szukaj / status / rok składkowy / pokaż usunięte, kolumna wpłat za wybrany rok),
- CRUD członków: dodawanie i edycja (dane członka wraz z adresem zameldowania),
  miękkie usuwanie ze strony potwierdzenia i przywracanie – akcje dostępne z menu
  „trzykropek" w wierszu listy.

Kolejne kroki (model danych i baza są już przygotowane):
- karta członka z wieloma adresami (korespondencyjny i inne),
- rejestracja wpłat składek i zestawienie wpłat dla członka,
- raporty: zaległości składkowe za rok (na podstawie `FeeRate` i `SharesCount`),
  lista adresowa do korespondencji, eksport do CSV/XLSX,
- zarządzanie kontami użytkowników, zmiana własnego hasła i podział uprawnień między role
  `Administrator` / `Pracownik`.

## Migracje

```bash
dotnet ef migrations add <Nazwa> -p src/MKS.DeepWater.Web -o Data/Migrations
```

Migracje wykonują się automatycznie przy starcie aplikacji (`DbInitializer`).
