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
3. zakłada konto startowe **admin / admin**,
4. wpisuje 3 przykładowych członków z adresami i jedną wpłatą składki.

Konto startowe można zmienić w `appsettings.json` (sekcja `SeedAdmin`) **przed** pierwszym
uruchomieniem. Hasło `admin` wymaga poluzowanej polityki haseł – ustawienia znajdują się
w `Program.cs` i przed wdrożeniem produkcyjnym należy je zaostrzyć.

## Struktura projektu

```
MKS.DeepWater.slnx
src/MKS.DeepWater.Web/
  Program.cs                 # konfiguracja: SQLite, Identity, globalne [Authorize], routing
  appsettings.json           # connection string + konto startowe (SeedAdmin)
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
    MembersController.cs     # lista członków z filtrowaniem
    HomeController.cs        # przekierowanie na listę + strona błędu
  Views/
    Shared/_Layout.cshtml    # szablon z zakładką "Lista członków"
    Account/Login.cshtml     # strona logowania
    Members/Index.cshtml     # lista członków
  wwwroot/                   # Bootstrap, jQuery, CSS
```

## Model danych

- **Member** – numer członkowski (unikalny), imię, nazwisko, PESEL, e-mail, telefon,
  data urodzenia, data przyjęcia/ustania członkostwa, status (aktywny / zawieszony / były),
  liczba udziałów, uwagi.
- **Address** – adres przypisany do członka; rodzaj: zameldowania / korespondencyjny / inny
  (ulica, nr domu, nr lokalu, kod pocztowy, miejscowość, kraj).
- **FeePayment** – wpłata składki: rok składkowy, data wpłaty, kwota, forma (przelew/gotówka/karta),
  numer dokumentu, uwagi.
- **FeeRate** – stawka składki na udział obowiązująca w danym roku (podstawa do raportu zaległości).

Relacje: `Member 1—* Address`, `Member 1—* FeePayment` (kaskadowe usuwanie).

## Stan realizacji

Zrealizowane (definition of done):
- projekt się buduje (`dotnet build` – 0 błędów, 0 ostrzeżeń),
- cała aplikacja wymaga zalogowania, anonim jest przekierowany na `/Account/Login`,
- logowanie `admin` / `admin` działa,
- po zalogowaniu widoczny szablon z jedną zakładką **Lista członków** (dane z bazy,
  filtr: szukaj / status / rok składkowy, kolumna wpłat za wybrany rok).

Kolejne kroki (model danych i baza są już przygotowane):
- CRUD członków (dodawanie, edycja, karta członka z adresami),
- rejestracja wpłat składek i zestawienie wpłat dla członka,
- raporty: zaległości składkowe za rok (na podstawie `FeeRate` i `SharesCount`),
  lista adresowa do korespondencji, eksport do CSV/XLSX,
- zarządzanie kontami użytkowników i podział uprawnień między role
  `Administrator` / `Pracownik`.

## Migracje

```bash
dotnet ef migrations add <Nazwa> -p src/MKS.DeepWater.Web -o Data/Migrations
```

Migracje wykonują się automatycznie przy starcie aplikacji (`DbInitializer`).
