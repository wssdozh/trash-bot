# Oreshkes

Oreshkes - Unity-проект в активной разработке: top-down action-прототип с процедурными комнатами, боевой системой, оружием, модификаторами, pickups, магазином, HUD, меню и боссом-экскаватором.

## Быстрый старт

1. Установить Unity `6000.2.8f1`.
2. Клонировать репозиторий вместе с Git LFS-объектами:

   ```powershell
   git lfs install
   git clone https://github.com/wssdozh/oreshkes.git
   cd oreshkes
   git lfs pull
   ```

3. Открыть папку проекта через Unity Hub.
4. Дождаться импорта пакетов и ассетов.
5. Запускать проект со сцены `Assets/Scenes/MainMenuScene.unity`.

## Сцены

В Build Settings сейчас включены:

- `Assets/Scenes/MainMenuScene.unity` - главное меню и входная точка.
- `Assets/Scenes/SampleSceneLevelGen.unity` - сцена с генерацией уровня.

Дополнительные сцены:

- `Assets/Scenes/SampleScene.unity` - старая/служебная сцена, сейчас выключена в Build Settings.
- `Assets/Scenes/test.unity` - локальная тестовая сцена.

## Что внутри

- `Assets/Scripts/Player` - движение, бой, инвентарь, оружие, UI игрока.
- `Assets/Scripts/Characters` - враги, турели, снаряды, босс и боевые системы.
- `Assets/Scripts/Level` и `Assets/Scripts/Room` - генерация уровня, комнат, коридоров и runtime-состояния.
- `Assets/Scripts/Shop` - торговые автоматы и покупка модификаторов.
- `Assets/Scripts/UI` - меню, HUD, настройки, пауза и экраны результатов.
- `Assets/SO` - ScriptableObject-настройки предметов, атак, спавнеров, комнат и модификаторов.
- `Docs` - диаграммы и служебная документация.

Подробный список скриптов лежит в `ScriptsDescription.md`.

## Зависимости

Основные пакеты указаны в `Packages/manifest.json`. Самые важные:

- Universal Render Pipeline `17.2.0`
- Unity Input System `1.14.2`
- AI Navigation `2.0.9`
- Unity UI `2.0.0`
- Unity Test Framework `1.6.0`
- Unified Universal Blur из GitHub-зависимости

## Git и GitHub

Актуальная основная линия проекта сейчас находится в ветке `backup/create-fire-executor-post-lfs-rewritten`. План выравнивания GitHub `main` и безопасной уборки веток описан в `Docs/GitHubMaintenance.md`.

Короткие правила:

- Не коммитить `Library`, `Temp`, `Obj`, `Build`, `Builds`, `Logs`, `UserSettings`.
- Для больших бинарных ассетов использовать Git LFS.
- Не удалять удалённые ветки без явного подтверждения и проверки, что нужные коммиты перенесены.
- Для изменений в `main` лучше использовать PR и короткое описание проверки в Unity.

## Проверка перед PR

- Проект открывается в нужной версии Unity.
- Нет случайных изменений в сценах, prefab и `.meta`-файлах.
- Сцены из Build Settings открываются без ошибок.
- Если менялись ассеты, подтянуты Git LFS-файлы.
