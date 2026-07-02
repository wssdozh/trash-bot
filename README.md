# Oreshkes

Oreshkes - top-down roguelike на Unity про короткие забеги по опасным комнатам. Игрок подбирает оружие, собирает ресурсы, усиливается через модификаторы и постепенно пробивается к более плотным боям.

Проект сейчас в стадии прототипа. Главная цель - сделать забег быстрым и читаемым: чтобы комната сразу давала понятную ситуацию, оружие ощущалось по-разному, а найденные усиления реально меняли стиль игры.

## Видео

<video src="Media/oreshkes-gameplay.mp4" controls width="100%"></video>

[Открыть видео отдельно](Media/oreshkes-gameplay.mp4)

## Скриншоты

| Gameplay | Progression |
| --- | --- |
| _Место для первого скриншота_ | _Место для второго скриншота_ |
| _Место для третьего скриншота_ | _Место для GIF или boss-сцены_ |

<!--
Когда появятся изображения, заменить строку выше на:

| Gameplay | Progression |
| --- | --- |
| ![Gameplay screenshot](Media/screenshot-gameplay.png) | ![Progression screenshot](Media/screenshot-progression.png) |
| ![Shop screenshot](Media/screenshot-shop.png) | ![Boss GIF](Media/boss.gif) |
-->

## Платформа

- PC / Windows
- Unity build

## Что реализовано

- Процедурная структура комнат и переходов.
- Top-down бой с ближним и дальним оружием.
- Несколько типов оружия и модификаторы для них.
- Pickups, ресурсы и внутриигровой магазин.
- HUD, меню, настройки и визуальная обратная связь.
- Boss encounter с экскаватором.

## Моя роль

- Gameplay programming.
- Системы оружия, модификаторов, pickups и прогрессии забега.
- Генерация комнат и связка игровых сцен.
- UI/HUD, меню, настройки и полировка игрового потока.
- Настройка проекта, GitHub, README и Git LFS для крупных файлов.

## Стек

- Unity `6000.2.8f1`
- C#
- Universal Render Pipeline
- Unity Input System
- AI Navigation
- TextMeshPro / Unity UI
- Git LFS

## Билд

Публичный билд будет добавлен в [GitHub Releases](https://github.com/wssdozh/oreshkes/releases).
