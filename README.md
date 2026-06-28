# Labyrinth Escape

Gra 3D typu first-person corridor exploration / puzzle adventure tworzona w silniku Unity. Gracz porusza sie po mrocznym labiryncie z perspektywy pierwszej osoby, eksploruje korytarze i pomieszczenia, walczy bronia biala z przeciwnikami, rozwiazuje zagadki, zbiera przedmioty i klucze, otwiera drzwi i probuje dotrzec do wyjscia.

## Gameplay

Nagranie z rozgrywki: [youtube.com/watch?v=UuQTD5dbmXY](https://www.youtube.com/watch?v=UuQTD5dbmXY)

## Autorzy

- Ivan Kasyniuk, nr albumu 37696
- Dmytro Zatserkivnyi, nr albumu 37751
- Bohdan Tsybulenko, nr albumu 38049

## Cel gry

Celem rozgrywki jest przejscie przez labirynt, pokonanie lub unikniecie przeciwnikow, rozwiazanie zagadek i dotarcie do strefy wyjscia. Po ukonczeniu poziomu gracz trafia na ekran koncowy z podsumowaniem rozgrywki.

## Glowne funkcjonalnosci

- sterowanie graczem w trybie FPS (ruch, skok, kucanie, sprint, dash), oparte o nowy Input System,
- walka bronia biala z roznymi rodzajami broni (rapier, katana, sztylet, kordelas, miecz), lekki i ciezki atak, blok,
- ekwipunek z pasem szybkiego dostepu (hotbar), przedmioty, mikstury leczace i notatki,
- przeciwnicy z rozbudowana sztuczna inteligencja (patrol, czujnosc, poscig, atak, przeszukiwanie, powrot na trase, smierc) oraz przeciwnik typu boss,
- system zagadek: zamkniete drzwi i klucze, dzwignie, zagadka pamieciowa "Repeat it" (znicze), pokoj fal przeciwnikow,
- interakcja z obiektami (drzwi, skrzynie, dzwignie, znicze, podnoszenie przedmiotow),
- pasek zdrowia i wytrzymalosci, paski zdrowia przeciwnikow i bossa, mapa labiryntu (minimapa),
- menu glowne, menu pauzy, panel ustawien i sterowania, komunikaty i ekran koncowy,
- oprawa audio i wizualna: pochodnie, oswietlenie, mgla, wiatr, efekty czastek i proceduralne dzwieki.

## Struktura projektu

Projekt jest podzielony na logiczne katalogi Unity:

- `Assets/Scenes` - sceny gry,
- `Assets/Scripts` - skrypty C# z logika gry (z podzialem na podsystemy),
- `Assets/Materials` - materialy (z podzialem: Walls, Environment, Props, Torch, Sword, Banners, Effects),
- `Assets/Models` - modele 3D (m.in. dlonie, bronie, elementy labiryntu),
- `Assets/Textures` - tekstury,
- `Assets/Animations` - animacje (m.in. dlonie FPS i bronie),
- `Assets/Audio` - dzwieki,
- `Assets/Settings` - ustawienia renderowania (URP), post-processing, Input System,
- `Assets/_ThirdParty` - zasoby zewnetrzne (paczki z Asset Store).

Katalog `Assets/Scripts` jest podzielony na podsystemy: `Core`, `Player`, `Enemy`, `Combat`, `Inventory`, `Puzzle`, `Environment`, `Audio`, `UI`.

Glowne sceny znajdujace sie w repozytorium:

- `MainMenuScene` - menu glowne gry,
- `GameScene` - glowna scena rozgrywki (labirynt),
- `EndScene` - scena koncowa po ukonczeniu gry.

## Systemy gry

- `GameManager` - zarzadzanie stanem gry, przejsciami scen, pauza, zwyciestwem i porazka.
- `PlayerController`, `PlayerHealth`, `PlayerStamina`, `PlayerStats`, `PlayerStatusEffects` - ruch FPS, kamera, zdrowie, wytrzymalosc i efekty statusowe gracza.
- `SwordCombat`, `WeaponStats`, `WeaponSwitcher`, `CombatVFX` - system walki bronia biala, statystyki broni, przelaczanie i efekty trafien.
- `PlayerInventory`, `InventoryItem`, `DroppedItem` - ekwipunek, hotbar, przedmioty i mikstury.
- `EnemyAI`, `EnemyHealth`, `EnemyAudio` - maszyna stanow przeciwnika, zdrowie i dzwieki.
- `CorridorEnemySpawner`, `AutoPatrolArea`, `EnemyZone` - rozmieszczanie i strefy przeciwnikow w labiryncie.
- `LockedDoor`, `Door`, `Chest`, `PuzzleLever`, `BrazierPuzzle`, `WaveRoomPuzzle`, `ExitZone` - drzwi, skrzynie, dzwignie, zagadki i strefa wyjscia.
- UI: `MainMenu`, `HUD`, `PauseMenu`, `SettingsPanel`, `ControlsPanel`, `InventoryUI`, `HotbarUI`, `ChestUI`, `NoteUI`, `MazeMap`, `EnemyHealthBar`, `BossHealthBar`, `EndGameSequence`.
- Srodowisko i audio: `DungeonWind`, `BannerWave`, `LightFlicker`, kontrolery odleglosciowe swiatel i czastek, `DungeonAmbience`, `ProceduralSfx`.

## Technologie

Wersja projektu w repozytorium:

- Unity Editor `6000.3.12f1` (Unity 6.3 LTS), renderowanie w trybie DX12.

Kluczowe pakiety z `Packages/manifest.json`:

- Universal Render Pipeline `17.3.0`,
- Input System `1.19.0`,
- AI Navigation `2.0.11`,
- Cinemachine `3.1.6`,
- ProBuilder `6.0.9`,
- Timeline `1.8.11`,
- Visual Effect Graph `17.3.0`,
- Unity UI (uGUI) `2.0.0` wraz z TextMeshPro,
- Unity Test Framework `1.6.0`.

Gra jest zrealizowana jako tytul first-person na PC (klawiatura i mysz) w oparciu o nowy Input System. W aktualnym `manifest.json` nie sa uzywane pakiety XR.

## Plan prac wedlug dokumentacji

Dokumentacja dzieli projekt na etapy:

1. Koncepcja i przygotowanie projektu.
2. Implementacja podstawowej mechaniki ruchu FPS.
3. Tworzenie poziomow i labiryntu.
4. Implementacja systemu przeciwnikow AI.
5. System zagadek i interakcji.
6. UI, walka oraz system nagrody.
7. Testowanie i optymalizacja.

Podzial odpowiedzialnosci opisany w dokumentacji:

- Ivan Kasyniuk - mechanika ruchu, UI, konfiguracja projektu Unity i repozytorium Git.
- Dmytro Zatserkivnyi - AI przeciwnikow, level design, glowna scena `GameScene`.
- Bohdan Tsybulenko - system zagadek, interakcje z obiektami, testowanie.

## Jak uruchomic projekt

1. Zainstalowac Unity Editor `6000.3.12f1`.
2. Otworzyc katalog repozytorium jako projekt Unity.
3. Otworzyc scene `Assets/Scenes/MainMenuScene.unity`.
4. Uruchomic projekt przyciskiem Play w edytorze Unity.

## Sterowanie

- ruch - WSAD / strzalki, rozgladanie - mysz,
- skok - spacja, kucanie - C, sprint - Shift, dash - Left Ctrl,
- atak - lewy przycisk myszy (przytrzymanie = ciezki atak), blok / parowanie - prawy przycisk myszy,
- interakcja / podnoszenie - E,
- sloty hotbara - klawisze 1-6 lub kolko myszy, wyrzucenie przedmiotu - Q,
- ekwipunek - I, pauza - Esc.

Pelna i aktualna lista skrotow jest dostepna w grze w panelu "Controls".

## Zrodla informacji

- `Projekt - Wirtualna Rzeczywistosc II.pdf` - koncepcja gry, cel, mechaniki i funkcjonalnosci.
- `Projekt - Wirtualna Rzeczywistosc II Labyrinth Escape.pdf` - autorzy, technologie, architektura, sceny, systemy gry, plan prac i podzial odpowiedzialnosci.
- `ProjectSettings/ProjectVersion.txt` - wersja Unity uzyta w repozytorium.
- `Packages/manifest.json` - lista pakietow Unity w repozytorium.
- `Assets/Scenes` - lista scen w projekcie.
- `Assets/Scripts` - lista systemow i skryptow C# w projekcie.
