# SolidEdge DXF Extractor
Autor: **Mateusz Gołębiewski** ([www.mateuszgolebiewski.pl](http://www.mateuszgolebiewski.pl))

Aplikacja (działająca całkowicie w obrębie środowiska systemu Windows .NET) bezbłędnie automatyzuje proces pozyskiwania czystych plików `.dxf` używanych w środowiskach palenia na maszynach CNC (WYCINARKI PLAZMOWE, LASERY), eksportując rzuty prosto ze złożeń Solid Edge.

## Wymagania

- **Solid Edge** ST10 lub nowszy (zainstalowany na komputerze)
- **.NET 8.0 Runtime** (lub nowszy) dla desktopu
- System operacyjny: Windows 10/11 (64-bit)


## Jak używać (Instrukcja Workflow)

1. Uruchom program **SolidEdge DXF Extractor** (`SolidEdge_FlatExporter.exe`). Nie musisz w tym czasie ręcznie odpalać Solid Edge, nasz program sam nawiąże odpowiednie połączenie maszynowe przez COM.
2. Kliknij **„Przeglądaj..."** i precyzyjnie wskaż plik głównego złożenia (`.asm`). **Alternatywnie:** możesz po prostu przeciągnąć plik `.asm` z eksploratora plików i upuścić go na okno programu, lub bezpośrednio na skrót do pliku `SolidEdge_FlatExporter.exe` - pominie to konieczność używania przycisku i automatycznie rozpocznie skanowanie złożenia!
3. Wciśnij **„Skanuj"** (jeśli nie użyłeś opcji przeciągnij i upuść). Program sprawnie i wnikliwie przeanalizuje listę podzespołów w tle, zliczając wszystkie blachy wygenerowane przez interfejs "Blacha/Sheet Metal". Rozpozna ich materiał oraz wysteruje grubości ze zmiennych.
4. Wybierz części na wygenerowanej interaktywnej liście.
5. Zaznacz, czy chcesz zachować bądź zignorować (usunąć) linnie gięcia dla tworzących się plików.
6. Określ swój autorski schemat pliku - czy życzysz sobie uwzględnić prefix z grubości oraz dopisek z konkretnego materiału.
7. Ostatecznie wybierz **„Eksportuj"**!

> **WAŻNE METODY ZABEZPIECZAJĄCE**: Program posiada maszynę analityczną, która w locie wymusza tworzenie brakujących FlatPatterns i zdejmuje śmieciowe bloki nagłówkowe tworzone przez oprogramowanie SolidEdge, chroniąc system przed wypluwaniem zerowych wektorów pustych `.dxf` (tzw "32 kilobajtowy błąd DXF"). Dodatkowo aplikacja kategoryzuje wybrane warstwy i programowo pozbywa się wektorów `[UP/DOWN]_CENTERLINES` prosto ze składni tekstowych plików, zostawiając nieskazitelnej jakości gładkie obręby dla programów wczytujących DXF. Złożenia naturalnie nie są nadpisywane, ani wizualnie modyfikowane.

## Format generowanych plików (Naming Scheme)

```
[Grubość_][Materiał_]NazwaBazowaCzesci.dxf
```
Przykład: `2.0mm_DC01_SolidnyWspornik_v1.dxf`

## Automatyczne Rozwiązywanie Problemów w logikach silnika
| Zdarzenie | Procedura silnika / Komunikat |
|---------|-------------|
| „Solid Edge nie jest zainstalowany" | Upewnij się, że silnik `Solid Edge` z API poprawnie wpisuje się do Windows COM Registry. |
| Grubość (Thickness) = "nieznana" | Analizator nie odnalazł w zmiennych środowiska odpowiednika (`Grubość materiału` bądź `Gage`). |
| Przypadek 34KB DXF | API SE rzuciło pustym szkieletem. Silnik wykasuje fake-objętość, wysteruje parametry robocze i na nowo wywoła operacyjne "Spłaszczenie i Zrzut". |
| Resztki warstw złoceń | Parser kodów blokowych formatu DXF interweniuje, wycinając z list encji obiekty z nagłówkami na których bazuje SE dla linii `Centerlines`. |

## Licencja i Prawa Autorskie

**Wszelkie autorskie pomysły w obiekcie, implementacja zaawansowanego analizatora błędu autodetekcji Solid Edge'a (COM fallback) oraz silnik parseringu tekstu warstw DXF (odśmiecacz gięć) powstały za sprawą Antigravity Engine i Mateusza Gołębiewskiego. All rights reserved.**

