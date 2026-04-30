# Labyrinth Escape

Projekt gry 3D typu corridor exploration / puzzle adventure tworzony w silniku Unity. Gracz porusza sie po labiryncie z perspektywy pierwszej osoby, eksploruje korytarze i pomieszczenia, rozwiazuje zagadki, zbiera klucze, otwiera drzwi oraz unika przeciwnikow.

## Autorzy

- Ivan Kasyniuk, nr albumu 37696
- Dmytro Zatserkivnyi, nr albumu 37751
- Bohdan Tsybulenko, nr albumu 38049

## Cel gry

Celem rozgrywki jest przejscie przez labirynt, rozwiazanie zagadek i dotarcie do konca gry. Dokumentacja projektowa opisuje koncowa nagrode jako artefakt lub skarb symbolizujacy ukonczenie gry.

## Glowne funkcjonalnosci

- sterowanie graczem w trybie FPS,
- eksploracja korytarzy i pomieszczen,
- przeciwnicy patrolujacy poziom i reagujacy na gracza,
- system zagadek oparty o klucze, przelaczniki i zamkniete drzwi,
- interakcja z obiektami,
- menu glowne, komunikaty i ekran koncowy,
- system zwyciestwa po ukonczeniu gry.

## Struktura projektu

Projekt jest podzielony na logiczne katalogi Unity:

- `Assets/Scenes` - sceny gry,
- `Assets/Scripts` - skrypty C# odpowiedzialne za logike gry,
- `Assets/Materials` - materialy,
- `Assets/Models` - modele 3D,
- `Assets/Settings` - ustawienia projektu.

Glowne sceny znajdujace sie w repozytorium:

- `MainMenuScene` - menu glowne gry,
- `GameScene` - glowna scena rozgrywki,
- `EndScene` - scena koncowa po ukonczeniu gry.

## Systemy gry

- `GameManager` - zarzadzanie stanem gry, przejsciami scen, zwyciestwem i porazka.
- `PlayerController` - ruch gracza i obsluga widoku FPS.
- `EnemyAI` - podstawowe stany przeciwnika: patrol, poscig i atak.
- Puzzle scripts - obsluga kluczy, drzwi, zamknietych drzwi, skrzyn i przelacznikow.
- UI scripts - menu glowne, HUD oraz ekran koncowy.

## Technologie

Wersja projektu w repozytorium:

- Unity Editor `6000.3.12f1`.

Pakiety widoczne w `Packages/manifest.json`:

- Universal Render Pipeline `17.3.0`,
- Input System `1.19.0`,
- AI Navigation `2.0.11`,
- ProBuilder `6.0.9`,
- Unity UI `2.0.0`,
- Unity Test Framework `1.6.0`.

Dokumentacja projektowa zaklada wykorzystanie Unity 6000.x, URP 17.x, XR Interaction Toolkit 3.0+, TextMeshPro oraz Newtonsoft.Json. W obecnym `Packages/manifest.json` potwierdzone sa Unity 6000.x przez `ProjectVersion.txt` oraz URP 17.x przez manifest. Nie moge potwierdzic obecnosci XR Interaction Toolkit i Newtonsoft.Json w aktualnym manifiescie repozytorium.

## Plan prac wedlug dokumentacji

Dokumentacja dzieli projekt na etapy:

1. Koncepcja i przygotowanie projektu.
2. Implementacja podstawowej mechaniki ruchu FPS / VR.
3. Tworzenie poziomow i labiryntu.
4. Implementacja systemu przeciwnikow AI.
5. System zagadek i interakcji.
6. UI oraz system nagrody.
7. Testowanie i optymalizacja.

Podzial odpowiedzialnosci opisany w dokumentacji:

- Ivan Kasyniuk - mechanika ruchu, UI, konfiguracja projektu Unity i repozytorium Git.
- Dmytro Zatserkivnyi - AI przeciwnikow, level design, glowna scena `GameScene`.
- Bohdan Tsybulenko - system zagadek, interakcje z obiektami, testowanie.

## Jak uruchomic projekt

1. Zainstalowac Unity Editor `6000.3.12f1`.
2. Otworzyc katalog repozytorium jako projekt Unity.
3. Otworzyc scene `Assets/Scenes/MainMenuScene.unity` albo `Assets/Scenes/GameScene.unity`.
4. Uruchomic projekt przyciskiem Play w edytorze Unity.

## Zrodla informacji

- `Projekt - Wirtualna Rzeczywistosc II.pdf` - koncepcja gry, cel, mechaniki i funkcjonalnosci.
- `Projekt - Wirtualna Rzeczywistosc II Labyrinth Escape.pdf` - autorzy, technologie, architektura, sceny, systemy gry, plan prac i podzial odpowiedzialnosci.
- `ProjectSettings/ProjectVersion.txt` - wersja Unity uzyta w repozytorium.
- `Packages/manifest.json` - lista pakietow Unity w repozytorium.
- `Assets/Scenes` - lista scen w projekcie.
- `Assets/Scripts` - lista systemow i skryptow C# w projekcie.
