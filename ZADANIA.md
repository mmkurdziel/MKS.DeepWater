# Zadania: logowanie numerem telefonu i kodem SMS

Tracker realizacji. Zadania wykonywane są **po kolei**, po jednym na raz.
Pełna analiza techniczna i uzasadnienie decyzji: <https://claude.ai/code/artifact/d96e286f-c209-422f-aa87-0dce55742560>

## Jak korzystać z tego pliku

1. Kolejne zadanie do realizacji = **pierwsze z góry ze statusem `⬜ Oczekuje`**.
2. Zadanie jest ukończone wyłącznie wtedy, gdy spełnia **wszystkie** punkty z „Ukończone, gdy”,
   w tym `dotnet test` na zielono i `dotnet build` bez ostrzeżeń.
3. Po ukończeniu: status na `✅ Zrobione`, data i skrót commita w nawiasie.
4. Testy jednostkowe powstają **razem z kodem zadania**, nie w osobnym etapie na końcu.
5. Zadania 11 i 12 nie mają testów jednostkowych (weryfikacja ręczna / decyzja wdrożeniowa) —
   jest to zaznaczone jawnie, nie jest przeoczeniem.

## Ustalenia obowiązujące we wszystkich zadaniach

- Numer telefonu: **dokładnie 9 cyfr**, wzorzec `^[1-9][0-9]{8}$`, bez spacji i znaków specjalnych.
- `+48` jest stałą w kodzie (`PhoneNumber.CountryCode`), doklejaną **wyłącznie** w `ISmsSender`.
  Baza nie przechowuje kierunkowego.
- Obsługiwane są tylko numery polskie.
- Logowanie członka: numer → kod SMS (6 cyfr). Logowanie admina: login + hasło (zostaje na stałe).
- Bramka na czas developmentu: **SMS Gate** (telefon z Androidem, tryb Local Server).
  Wybór płatnej bramki to zadanie 12, przy wdrożeniu produkcyjnym.
- Role: `Administrator`, `Dostęp rozszerzony`, `Dostęp ograniczony`. Jedna rola na konto.

## Stan realizacji

| # | Zadanie | Status |
|---|---|---|
| 1 | Projekt testowy | ✅ Zrobione |
| 2 | `PhoneNumber` i walidator numeru | ⬜ Oczekuje |
| 3 | Unikalność numeru w bazie | ⬜ Oczekuje |
| 4 | Role i powiązanie konta z członkiem | ⬜ Oczekuje |
| 5 | `ISmsSender` i SMS Gate | ⬜ Oczekuje |
| 6 | `LoginCode` i `LoginCodeService` | ⬜ Oczekuje |
| 7 | Ekrany logowania | ⬜ Oczekuje |
| 8 | Panel administratora | ⬜ Oczekuje |
| 9 | „Moje dane” | ⬜ Oczekuje |
| 10 | Utwardzenie i audyt | ⬜ Oczekuje |
| 11 | SMS Gate na środowisku dev | ⬜ Oczekuje |
| 12 | Wybór płatnej bramki | ⬜ Oczekuje |

---

### Zadanie 1 — Projekt testowy

**Status:** ✅ Zrobione (2026-10-26, gałąź `zadanie-01-projekt-testowy`)

**Cel.** Każde następne zadanie kończy się zielonym `dotnet test`, więc miejsce na testy musi
istnieć przed pierwszą linią kodu produkcyjnego.

**Zakres**

- `tests/MKS.DeepWater.Web.Tests/` — xUnit, `net10.0`, referencja do projektu webowego.
- Pakiety: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`,
  `Microsoft.EntityFrameworkCore.Sqlite`.
- Asercje: wbudowane `Assert` z xUnit. **Bez FluentAssertions** — wersje 8+ mają licencję
  komercyjną, a projekt nie potrzebuje niczego ponad standard.
- Dodanie projektu do `MKS.DeepWater.slnx`.
- Helper `TestDb.CreateContext()` tworzący `ApplicationDbContext` na **SQLite in-memory**
  (`Microsoft.Data.Sqlite` z `Data Source=:memory:`, otwarte połączenie trzymane przez czas testu).
  Nie `EF Core InMemory`: ten provider **nie wymusza unikalnych indeksów** i nie zwraca
  `SqliteException 19`, więc testy z zadania 3 dałyby fałszywą zieloność.

**Pliki**

- `tests/MKS.DeepWater.Web.Tests/MKS.DeepWater.Web.Tests.csproj`
- `tests/MKS.DeepWater.Web.Tests/TestDb.cs`
- `tests/MKS.DeepWater.Web.Tests/SmokeTests.cs`
- `MKS.DeepWater.slnx` (modyfikacja)

**Testy jednostkowe**

- `TestDb_tworzy_baze_z_migracjami` — kontekst wstaje, `Members` i `Users` są puste.
- `TestDb_ma_unikalny_indeks_w_schemacie` — `sqlite_master` zawiera
  `IX_Members_MembershipNumber` ze słowem `UNIQUE`.
- `TestDb_wymusza_unikalny_indeks` — dwa rekordy z tym samym `MembershipNumber` dają
  `DbUpdateException` z `SqliteErrorCode 19`.
- `Bez_indeksu_ten_sam_numer_przechodzi` — kontrola negatywna: po `DROP INDEX` ten sam zapis
  przechodzi i w bazie są dwa rekordy.

**Ukończone, gdy**

- [x] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [x] `dotnet test` — 4/4 zielone
- [x] sonda potrafi dać wynik negatywny — dowiedzione automatycznie przez
      `Bez_indeksu_ten_sam_numer_przechodzi`

**Zrealizowano inaczej, niż planowano.** Pierwotny warunek brzmiał „test unikalności czerwieni się
po usunięciu `.IsUnique()` z `OnModelCreating`, sprawdzone ręcznie raz”. To nie działa: schemat
bazy testowej powstaje z **migracji**, a nie z modelu, więc zmiana w `OnModelCreating` nie zdejmuje
indeksu z już zmigrowanej bazy. Kontrola negatywna robi to poprawnie — zdejmuje indeks z bazy
(`DROP INDEX`) i sprawdza, że zapis wtedy przechodzi. Dodatkowo doszedł test odczytujący definicję
indeksu z `sqlite_master`, który wykryje brak migracji albo utratę indeksu.

**Pakiety faktycznie użyte:** `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3,
`xunit.runner.visualstudio` 3.1.4, `Microsoft.EntityFrameworkCore.Sqlite` 10.0.13.
`coverlet.collector` ze szablonu usunięty — nieużywany.

---

### Zadanie 2 — `PhoneNumber` i walidator numeru

**Status:** ⬜ Oczekuje

**Cel.** Jedno miejsce w aplikacji, które wie, czym jest numer telefonu i jak wygląda `+48`.
Czysta logika, bez bazy — najtańsze do przetestowania i podstawa wszystkiego dalej.

**Zakres**

- `PhoneNumber`: `CountryCode = "+48"`, `Pattern` (`^[1-9][0-9]{8}$`), `IsValid`,
  `ToGatewayFormat`, `ToDisplay`, `LooksLikeMobile`.
- Prefiksy komórkowe UKE w jednej tablicy: `45, 50, 51, 53, 57, 60, 66, 69, 72, 73, 78, 79, 88`,
  z komentarzem, że lista jest ruchoma i służy **tylko** do ostrzeżenia.
- `[PolishMobileNumber]` — atrybut walidacji, komunikat:
  „Numer telefonu to 9 cyfr, bez spacji i znaków specjalnych.”

**Pliki**

- `src/MKS.DeepWater.Web/Services/PhoneNumber.cs`
- `src/MKS.DeepWater.Web/Models/Validation/PolishMobileNumberAttribute.cs`
- `tests/MKS.DeepWater.Web.Tests/PhoneNumberTests.cs`

**Testy jednostkowe**

- `IsValid_przyjmuje_dziewiec_cyfr` — `600100200` → `true`.
- `IsValid_odrzuca_spacje_i_znaki_specjalne` — `600 100 200`, `600-100-200`, `+48600100200`,
  `0048600100200`, `(600)100200` → wszystkie `false`.
- `IsValid_odrzuca_zla_dlugosc` — 8 cyfr, 10 cyfr, pusty, `null` → `false`.
- `IsValid_odrzuca_wiodace_zero` — `0600100200` i `012345678` → `false`.
- `IsValid_odrzuca_litery` — `60010020a` → `false`.
- `ToGatewayFormat_doklada_kierunkowy` — `600100200` → `+48600100200`.
- `ToDisplay_formatuje_w_trzy_grupy` — `600100200` → `600 100 200`; `null` → pusty string.
- `LooksLikeMobile_rozpoznaje_prefiksy` — `600100200` → `true`; `581234567` (stacjonarny Gdańsk)
  → `false`.
- `Atrybut_zwraca_komunikat_o_dziewieciu_cyfrach` — `GetValidationResult` dla `600 100 200`
  zwraca błąd z oczekiwanym tekstem.

**Ukończone, gdy**

- [ ] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` — wszystkie zielone
- [ ] żadne miejsce w kodzie poza `PhoneNumber` nie zawiera literału `"+48"`

---

### Zadanie 3 — Unikalność numeru w bazie

**Status:** ⬜ Oczekuje

**Cel.** Numer telefonu staje się jednoznacznym identyfikatorem członka: unikalnym także wśród
miękko usuniętych rekordów i niemożliwym do wpisania w złym formacie.

**Zakres**

- `Member.PhoneNumber` → `[StringLength(9)]`, `[PolishMobileNumber]`.
- `ApplicationDbContext`: `HasIndex(m => m.PhoneNumber).IsUnique()`.
- Migracja `NormalizePhoneNumbers`: ręczny `Sql()` czyszczący istniejące numery
  (usunięcie separatorów, obcięcie prefiksów `0048` / `48` / `0` po długości, wyzerowanie
  tego, czego nie da się zinterpretować), **potem** założenie indeksu. Kolejność jest istotna.
- `MembersController`: `ValidatePhoneNumberAsync(numer, excludeId)` z `IgnoreQueryFilters()`,
  `IsUniqueConflict(ex, "Members.PhoneNumber")`, komunikat o zajętym numerze.
- `_MemberForm.cshtml`: pole tylko na cyfry (`maxlength="9"`, `inputmode="numeric"`,
  `pattern`), prefiks `+48` jako `input-group-text`, ostrzeżenie przy prefiksie niekomórkowym.
- `Views/Members/Index.cshtml`: wyświetlanie przez `PhoneNumber.ToDisplay`.

**Pliki**

- `src/MKS.DeepWater.Web/Models/Domain/Member.cs` (modyfikacja)
- `src/MKS.DeepWater.Web/Models/ViewModels/MemberFormViewModel.cs` (modyfikacja)
- `src/MKS.DeepWater.Web/Data/ApplicationDbContext.cs` (modyfikacja)
- `src/MKS.DeepWater.Web/Data/Migrations/…_NormalizePhoneNumbers.cs`
- `src/MKS.DeepWater.Web/Controllers/MembersController.cs` (modyfikacja)
- `src/MKS.DeepWater.Web/Views/Members/_MemberForm.cshtml`, `Index.cshtml` (modyfikacja)
- `tests/MKS.DeepWater.Web.Tests/MemberPhoneUniquenessTests.cs`

**Testy jednostkowe**

- `Dwoch_czlonkow_nie_moze_miec_tego_samego_numeru` — drugi zapis → `DbUpdateException` z kodem 19.
- `Numer_usunietego_czlonka_nadal_blokuje` — członek z numerem miękko usunięty, nowy z tym samym
  numerem → kolizja. Test chroni przed pominięciem `IgnoreQueryFilters()`.
- `Walidacja_wykrywa_zajety_numer_przed_zapisem` — `ModelState` ma błąd na `PhoneNumber`,
  bez wyjątku z bazy.
- `Walidacja_pomija_wlasny_rekord_przy_edycji` — zapis członka bez zmiany numeru przechodzi.
- `Pusty_numer_nie_blokuje_innych` — dwóch członków z `null` zapisuje się bez kolizji
  (członkowie archiwalni bez konta).
- `Migracja_czysci_numery` — dane wejściowe `600 100 200`, `+48600100200`, `0048600100200`,
  `0600100200`, `12345` → odpowiednio czterokrotnie `600100200` i `null`.

**Ukończone, gdy**

- [ ] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` — wszystkie zielone
- [ ] `dotnet ef database update` przechodzi na kopii istniejącej bazy `App_Data/deepwater.db`
- [ ] próba wpisania numeru ze spacją w formularzu daje komunikat walidacji, nie wyjątek

---

### Zadanie 4 — Role i powiązanie konta z członkiem

**Status:** ⬜ Oczekuje

**Cel.** Konto wie, którego członka dotyczy, a dostęp do stron wynika z roli. Nadal logowanie
hasłem — ten etap nie dotyka uwierzytelniania.

**Zakres**

- `ApplicationUser.MemberId` (`int?`) + relacja 1:1 z `Member`, unikalny indeks,
  `DeleteBehavior.Restrict`.
- `Data/Roles.cs` — stałe trzech rół.
- `MemberAccountService`: `CreateAccountAsync`, `SyncPhoneAsync`, `SetRoleAsync`,
  `DisableAccountAsync`. Jedyne miejsce dotykające `UserManager`.
- Polityki w `Program.cs`: `PelnaEwidencja`, `WlasneDane`, `ZarzadzanieKontami`.
- `ApplicationUserClaimsPrincipalFactory` — claim `MemberId`.
- `DbInitializer`: trzy role, usunięcie nieużywanej roli `Pracownik`.
- `_Layout.cshtml`: zakładki warunkowane rolą.
- Migracja `PhoneAsLoginIdentity`.

**Pliki**

- `src/MKS.DeepWater.Web/Data/ApplicationUser.cs`, `Roles.cs`,
  `ApplicationUserClaimsPrincipalFactory.cs`, `DbInitializer.cs`, `ApplicationDbContext.cs`
- `src/MKS.DeepWater.Web/Services/MemberAccountService.cs`
- `src/MKS.DeepWater.Web/Data/Migrations/…_PhoneAsLoginIdentity.cs`
- `src/MKS.DeepWater.Web/Program.cs`, `Views/Shared/_Layout.cshtml` (modyfikacja)
- `tests/MKS.DeepWater.Web.Tests/MemberAccountServiceTests.cs`

**Testy jednostkowe**

- `Tworzy_konto_z_numerem_jako_loginem` — `UserName` i `PhoneNumber` konta = numer członka,
  `MemberId` ustawione.
- `Nie_tworzy_konta_bez_numeru` — członek z `null` → `IdentityResult` nieudany, czytelny błąd.
- `Nie_tworzy_drugiego_konta_dla_tego_samego_czlonka` — unikalność `MemberId`.
- `Zmiana_numeru_czlonka_przenosi_login` — po `SyncPhoneAsync` stary numer nie loguje,
  nowy wskazuje to samo konto.
- `Konto_ma_dokladnie_jedna_role` — po `SetRoleAsync` na inną rolę poprzednia jest usunięta.
- `Zablokowane_konto_ma_lockout_w_przyszlosci` — `DisableAccountAsync` ustawia `LockoutEnd`.
- `Claim_MemberId_trafia_do_tozsamosci` — fabryka claimów dodaje `MemberId`; konto admina
  (bez członka) nie dostaje claimu.

**Ukończone, gdy**

- [ ] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` — wszystkie zielone
- [ ] logowanie kontem startowym admina nadal działa
- [ ] rola `Pracownik` nie istnieje, trzy nowe role są zakładane przy starcie

---

### Zadanie 5 — `ISmsSender` i SMS Gate

**Status:** ⬜ Oczekuje

**Cel.** Wysyłka SMS jako szczegół konfiguracyjny: `Log` na czas pracy bez telefonu,
`SmsGate` na development, czwarty provider dopisywany przy wdrożeniu.

**Zakres**

- `ISmsSender`, `SmsResult`, `SmsOptions`.
- `SmsGateSender` — `POST {BaseUrl}/message`, ciało `{ phoneNumbers, message }`, Basic auth,
  nazwany `HttpClient`. Wzorzec przeniesiony z `Moricom.OneHotel.Logic/Sms/SmsService.cs`.
- `LogSmsSender` — kod do logu i `App_Data/sms-outbox.txt`. `DisabledSmsSender` — kontrolowany błąd.
- Guardy: pusty `BaseUrl`, `BaseUrl` nieabsolutny lub nie-http(s), timeout jako odrzucenie wysyłki.
- `+48` doklejane **tylko tutaj**, przez `PhoneNumber.ToGatewayFormat`.
- `Sms:MonthlyLimit` — bezpiecznik przed pętlą.
- **Guard startowy** w `Program.cs`: `Provider` równy `Log` lub `Disabled` w środowisku
  `Production` przerywa start z komunikatem, co ustawić. Wzorzec z istniejącego guardu na
  `SeedAdmin:Password`.
- Sekcja `Sms` w `appsettings.json`, sekrety w user-secrets.
- Bez webhooków i bez rejestracji przy starcie — potrzebny jest tylko ruch wychodzący.

**Pliki**

- `src/MKS.DeepWater.Web/Services/Sms/ISmsSender.cs`, `SmsGateSender.cs`, `LogSmsSender.cs`,
  `DisabledSmsSender.cs`, `SmsOptions.cs`
- `src/MKS.DeepWater.Web/Program.cs`, `appsettings.json` (modyfikacja)
- `tests/MKS.DeepWater.Web.Tests/SmsGateSenderTests.cs`

**Testy jednostkowe** (na podstawionym `HttpMessageHandler`, bez sieci)

- `Wysylka_trafia_pod_sciezke_message` — URL kończy się na `/message`.
- `Numer_jedzie_z_kierunkowym` — ciało żądania zawiera `+48600100200` dla wejścia `600100200`.
- `Uzywa_Basic_auth` — nagłówek `Authorization` = `Basic` z poświadczeniami z konfiguracji.
- `Pusty_BaseUrl_nie_wysyla` — wynik nieudany, **żadnego** żądania HTTP, brak wyjątku.
- `Nieabsolutny_BaseUrl_nie_wysyla` — jak wyżej dla `"localhost:8080"`.
- `Timeout_to_odrzucenie_nie_anulowanie` — `TaskCanceledException` bez anulowania przez wołającego
  → wynik nieudany, nie wyjątek.
- `Odczytuje_identyfikator_wiadomosci` — odpowiedź `{"id":"abc"}` → `SmsResult` z `abc`.
- `Dwa_xx_bez_id_nie_jest_bledem` — 2xx bez rozpoznanego `id` → wynik udany z ostrzeżeniem w logu.
- `Przekroczony_MonthlyLimit_blokuje_wysylke` — wynik nieudany, zero żądań HTTP.
- `LogSmsSender_zapisuje_kod_do_pliku` — plik zawiera numer i kod.
- `Produkcja_z_providerem_Log_przerywa_start` — walidacja konfiguracji rzuca wyjątek
  z komunikatem wskazującym `Sms:Provider`.

**Ukończone, gdy**

- [ ] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` — wszystkie zielone
- [ ] żaden test nie wykonuje realnego żądania sieciowego

---

### Zadanie 6 — `LoginCode` i `LoginCodeService`

**Status:** ⬜ Oczekuje

**Cel.** Serce bezpieczeństwa logowania: kod sześciocyfrowy, krótko ważny, jednorazowy,
z limitem prób i limitem wysyłki. Najważniejsze testy w całym projekcie.

**Zakres**

- Encja `LoginCode`: `UserId`, `CodeHash`, `CreatedAt`, `ExpiresAt`, `ConsumedAt`,
  `FailedAttempts`, `RequestIp`. Migracja `LoginCodes`.
- `LoginCodeService.RequestAsync(user, ip)` — unieważnia poprzednie kody, generuje nowy przez
  `RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6")`, zapisuje **skrót** (nigdy kod
  jawnie), zleca wysyłkę.
- `LoginCodeService.VerifyAsync(user, kod)` — sprawdza skrót, ważność, jednorazowość i licznik prób.
- Parametry z `appsettings.json`: `Digits`, `LifetimeMinutes` (5), `MaxAttempts` (5),
  `MaxCodesPerPhonePer15Min` (3), `ResendCooldownSeconds` (60).
- Treść SMS bez polskich znaków, kod na początku (podpowiedź `one-time-code` w Androidzie).
- Czas wstrzykiwany (`TimeProvider`), żeby testy wygaśnięcia nie musiały czekać.

**Pliki**

- `src/MKS.DeepWater.Web/Models/Domain/LoginCode.cs`
- `src/MKS.DeepWater.Web/Services/LoginCodeService.cs`, `LoginCodeOptions.cs`
- `src/MKS.DeepWater.Web/Data/Migrations/…_LoginCodes.cs`
- `src/MKS.DeepWater.Web/Data/ApplicationDbContext.cs`, `appsettings.json` (modyfikacja)
- `tests/MKS.DeepWater.Web.Tests/LoginCodeServiceTests.cs`

**Testy jednostkowe**

- `Kod_ma_szesc_cyfr` — 1000 wywołań, każdy wynik pasuje do `^[0-9]{6}$` (łapie też `D6`
  gubiące wiodące zera).
- `Kod_nie_jest_zapisywany_jawnie` — w bazie nie ma kolumny z kodem w postaci czytelnej.
- `Poprawny_kod_loguje` — `VerifyAsync` → sukces.
- `Kod_dziala_tylko_raz` — druga weryfikacja tego samego kodu → porażka.
- `Wygasly_kod_nie_dziala` — po przesunięciu czasu o 5 min i 1 s → porażka.
- `Szosta_bledna_proba_spala_kod` — 5 błędnych prób, potem **poprawny** kod → porażka.
- `Nowy_kod_uniewaznia_poprzedni` — stary kod po `RequestAsync` → porażka.
- `Czwarty_kod_w_kwadransie_nie_wychodzi` — czwarte `RequestAsync` nie zleca wysyłki.
- `Ponowna_wysylka_przed_60_s_odrzucona` — cooldown działa.
- `Kod_innego_uzytkownika_nie_dziala` — kod wydany koncie A nie loguje konta B.
- `Tresc_sms_bez_polskich_znakow` — wiadomość w całości w ASCII (ochrona limitu 160 znaków).

**Ukończone, gdy**

- [ ] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` — wszystkie zielone
- [ ] w logach aplikacji nie pojawia się kod ani pełny numer telefonu

---

### Zadanie 7 — Ekrany logowania

**Status:** ⬜ Oczekuje

**Cel.** Członek loguje się numerem i kodem; admin zachowuje ścieżkę hasłową.

**Zakres**

- `AccountController`: `Login` (GET/POST, 9 cyfr), `VerifyCode` (GET/POST), `ResendCode` (POST),
  `LoginAdmin` (GET/POST, dotychczasowa logika hasłowa).
- Numer między krokami w **zaszyfrowanym, krótkożyciowym ciasteczku** (Data Protection),
  nigdy w URL.
- Jednolita odpowiedź niezależnie od istnienia konta (brak możliwości sprawdzenia, kto jest
  członkiem).
- Nieudana weryfikacja → `AccessFailedAsync` (lockout z polityki Identity).
- `AddRateLimiter` na endpointach logowania: limit per IP obok limitu per numer.
- Widoki: `Login.cshtml` (pole 9 cyfr z prefiksem `+48`), `VerifyCode.cshtml`
  (`inputmode="numeric"`, `autocomplete="one-time-code"`, licznik ważności, „wyślij ponownie”
  po 60 s), `LoginAdmin.cshtml`.

**Pliki**

- `src/MKS.DeepWater.Web/Controllers/AccountController.cs` (modyfikacja)
- `src/MKS.DeepWater.Web/Models/ViewModels/PhoneLoginViewModel.cs`, `VerifyCodeViewModel.cs`
- `src/MKS.DeepWater.Web/Views/Account/Login.cshtml` (modyfikacja), `VerifyCode.cshtml`,
  `LoginAdmin.cshtml`
- `src/MKS.DeepWater.Web/Program.cs` (modyfikacja)
- `tests/MKS.DeepWater.Web.Tests/AccountControllerTests.cs`

**Testy jednostkowe**

- `Numer_ze_spacja_nie_przechodzi_walidacji` — widok z błędem, zero wysyłek.
- `Nieistniejacy_numer_daje_ta_sama_odpowiedz_co_istniejacy` — identyczny wynik akcji,
  przy nieistniejącym zero wysyłek.
- `Zablokowane_konto_nie_dostaje_kodu` — zero wysyłek.
- `Poprawny_kod_loguje_i_przekierowuje` — `SignInAsync` wywołane raz.
- `Bledny_kod_zwieksza_licznik_i_zostaje_na_stronie` — `AccessFailedAsync` wywołane.
- `Numer_nie_pojawia_sie_w_adresie_url` — przekierowanie do `VerifyCode` bez numeru w query.
- `ReturnUrl_zewnetrzny_jest_odrzucany` — przekierowanie na listę członków
  (regresja istniejącego `SafeReturnUrl`).
- `Logowanie_admina_haslem_dziala` — ścieżka hasłowa nietknięta.

**Ukończone, gdy**

- [ ] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` — wszystkie zielone
- [ ] pełne logowanie członka przechodzi ręcznie z `Provider = "Log"` (kod odczytany z pliku)

---

### Zadanie 8 — Panel administratora

**Status:** ⬜ Oczekuje

**Cel.** Admin zakłada konta członkom i nadaje im rolę. Plus dwa wyjścia awaryjne: ręczny kod
i podgląd zużycia wysyłki.

**Zakres**

- `UsersController` (`[Authorize(Policy = "ZarzadzanieKontami")]`): `Index` (numer, członek, rola,
  stan, ostatnie logowanie), `Create` (wybór członka bez konta + **select roli**), `Edit`
  (zmiana roli, blokada), `Delete` (blokuje, nie usuwa).
- Checkbox „Konto w systemie” + select roli w `_MemberForm.cshtml`; numer wymagany warunkowo,
  gdy checkbox zaznaczony.
- „Wyślij SMS testowy” — sonda konfiguracji bramki.
- „Wygeneruj kod ręcznie” — kod pokazany adminowi do przekazania telefonicznie.
- Licznik „wysłane w tym miesiącu / limit” z ostrzeżeniem przy zbliżaniu się do limitu.
- Miękkie usunięcie członka blokuje jego konto.

**Pliki**

- `src/MKS.DeepWater.Web/Controllers/UsersController.cs`
- `src/MKS.DeepWater.Web/Models/ViewModels/UserFormViewModel.cs`, `UserListViewModel.cs`
- `src/MKS.DeepWater.Web/Views/Users/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`
- `src/MKS.DeepWater.Web/Controllers/MembersController.cs`,
  `Views/Members/_MemberForm.cshtml` (modyfikacja)
- `tests/MKS.DeepWater.Web.Tests/UsersControllerTests.cs`

**Testy jednostkowe**

- `Tworzenie_konta_nadaje_wybrana_role`.
- `Tworzenie_konta_bez_roli_jest_odrzucane`.
- `Konto_dla_czlonka_bez_numeru_jest_odrzucane` — komunikat o wymaganym numerze.
- `Lista_wyboru_pomija_czlonkow_z_kontem`.
- `Zmiana_roli_zastepuje_poprzednia`.
- `Usuniecie_konta_blokuje_a_nie_usuwa` — rekord istnieje, `LockoutEnd` ustawione.
- `Miekkie_usuniecie_czlonka_blokuje_konto`.
- `Reczny_kod_jest_weryfikowalny` — kod z panelu przechodzi `VerifyAsync`.
- `Rola_nieadministracyjna_nie_wchodzi_do_panelu` — polityka odrzuca.

**Ukończone, gdy**

- [ ] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` — wszystkie zielone
- [ ] przejście ręczne: dodanie członka z kontem i rolą, zalogowanie się tym kontem

---

### Zadanie 9 — „Moje dane”

**Status:** ⬜ Oczekuje

**Cel.** Rola `Dostęp ograniczony` widzi wyłącznie własną kartę i własne wpłaty.

**Zakres**

- `MyProfileController` (`[Authorize(Policy = "WlasneDane")]`) — dane członka z claimu `MemberId`.
- Widok „Moje dane”: dane osobowe, adres, wpłaty składek.
- Zakładka w `_Layout` dla wszystkich zalogowanych.
- `MembersController` niedostępny dla `Dostęp ograniczony` (polityka `PelnaEwidencja`).

**Pliki**

- `src/MKS.DeepWater.Web/Controllers/MyProfileController.cs`
- `src/MKS.DeepWater.Web/Views/MyProfile/Index.cshtml`
- `src/MKS.DeepWater.Web/Views/Shared/_Layout.cshtml` (modyfikacja)
- `tests/MKS.DeepWater.Web.Tests/MyProfileControllerTests.cs`

**Testy jednostkowe**

- `Widzi_tylko_swoje_dane` — konto z `MemberId = 1` dostaje członka 1.
- `Nie_da_sie_podejrzec_innego_czlonka` — brak parametru `id` w akcji; podmiana claimu nie daje
  dostępu do obcego rekordu.
- `Konto_bez_czlonka_dostaje_404` — admin bez `MemberId`.
- `Dostep_ograniczony_nie_wchodzi_na_liste_czlonkow` — polityka `PelnaEwidencja` odrzuca.
- `Usuniety_czlonek_nie_widzi_danych` — globalny filtr zapytań działa także tutaj.

**Ukończone, gdy**

- [ ] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` — wszystkie zielone

---

### Zadanie 10 — Utwardzenie i audyt

**Status:** ⬜ Oczekuje

**Cel.** Ślad po logowaniach, sprzątanie i dokumentacja.

**Zakres**

- Encja `LoginAttempt`: kto, kiedy, skąd, z jakim skutkiem. Migracja.
- Usuwanie wygasłych kodów przy starcie (`DbInitializer`).
- Maskowanie numeru w logach (`600***200`).
- Krótszy `ExpireTimeSpan` ciasteczka dla rół nieadministracyjnych.
- `README.md`: logowanie numerem, role, konfiguracja bramki, uruchomienie testów.

**Pliki**

- `src/MKS.DeepWater.Web/Models/Domain/LoginAttempt.cs`
- `src/MKS.DeepWater.Web/Data/Migrations/…_LoginAttempts.cs`
- `src/MKS.DeepWater.Web/Data/DbInitializer.cs`, `Program.cs` (modyfikacja)
- `README.md` (modyfikacja)
- `tests/MKS.DeepWater.Web.Tests/LoginAuditTests.cs`

**Testy jednostkowe**

- `Udane_logowanie_zapisuje_wpis`.
- `Nieudane_logowanie_zapisuje_wpis_z_przyczyna`.
- `Audyt_nie_zapisuje_kodu`.
- `Maskowanie_numeru_zostawia_szesc_gwiazdek` — `600100200` → `600***200`.
- `Sprzatanie_usuwa_tylko_wygasle` — ważny kod zostaje.

**Ukończone, gdy**

- [ ] `dotnet build` — 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` — wszystkie zielone
- [ ] `README.md` opisuje aktualny sposób logowania

---

### Zadanie 11 — SMS Gate na środowisku dev

**Status:** ⬜ Oczekuje

**Cel.** Realna wysyłka SMS na środowisku developerskim.
**Bez testów jednostkowych** — to konfiguracja urządzenia, nie kod. Weryfikacja ręczna.

**Zakres**

- SMS Gate na telefonie z Androidem, tryb **Local Server**, ustawione login i hasło.
- Wariant zalecany — Wi-Fi: `Sms:BaseUrl` = `http://<IP-telefonu>:8080`, rezerwacja DHCP.
  Wariant zapasowy — USB: `adb forward tcp:8080 tcp:8080`, `BaseUrl` = `http://localhost:8080`.
- Poświadczenia w user-secrets, **kopiowane ze schowka** (znaki `I` / `l` / `1`).
  Po każdej zmianie restart aplikacji — sekrety wczytują się tylko przy starcie.
- Wyłączona optymalizacja baterii dla aplikacji bramki, telefon w zasilaniu.

**Weryfikacja**

- [ ] `curl -s -o /dev/null -w "%{http_code}" http://<host>:8080/health` → `200`
- [ ] `curl -s -o /dev/null -w "%{http_code}" -u sms:HASLO http://<host>:8080/message` → `200`
      (`401` = złe poświadczenia, `404` = zła ścieżka)
- [ ] `Sms:Provider = "SmsGate"`, „wyślij SMS testowy” z panelu kont dociera na telefon
- [ ] pełne logowanie członka realnym SMS-em
- [ ] `dotnet test` nadal zielony (testy nie dotykają sieci)

---

### Zadanie 12 — Wybór płatnej bramki

**Status:** ⬜ Oczekuje

**Cel.** Wdrożenie produkcyjne. **Osobna decyzja biznesowa**, nie część budowy — do podjęcia,
gdy znany będzie realny wolumen logowań i kto ponosi koszt.

**Do rozstrzygnięcia przy tym zadaniu**

- Dostawca. Przy kilkunastu SMS-ach miesięcznie koszt to 1–2,60 zł; odnawialnego darmowego limitu
  na numery polskie nie ma u nikogo (analiza: sekcja 5.3 dokumentu).
- Czy wysyłka na dowolny numer odbiorcy jest dozwolona na wybranym koncie.
- Nazwa nadawcy: narzucona czy firmowa (rejestracja u dostawcy, zwykle płatna).
- `Sms:MonthlyLimit` ustawiony świadomie — chroni już rachunek, nie pulę kredytów.

**Zakres**

- Nowa klasa `ISmsSender` dla wybranego dostawcy (~60 linii) + wpis w rejestracji providerów.
- `Provider` i sekrety przez zmienne środowiskowe na serwerze.

**Testy jednostkowe**

- Ten sam zestaw co w zadaniu 5, dla nowego providera: ścieżka, numer z kierunkowym,
  uwierzytelnianie, guardy, timeout, odczyt identyfikatora wiadomości.

**Ukończone, gdy**

- [ ] `dotnet test` — wszystkie zielone
- [ ] próba uruchomienia produkcji z `Provider = "Log"` **przerywa start** (guard z zadania 5)
- [ ] SMS testowy dociera przez produkcyjnego dostawcę
